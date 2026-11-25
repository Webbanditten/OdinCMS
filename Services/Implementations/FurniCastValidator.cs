using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Checks a furni cast before it is published to the hotel's hof_furni (docs/furni-editor-housekeeping.md). Every
    /// client downloads these files, and a Director cast can carry Lingo or members named like the client's own classes
    /// and fields, which would replace them for everyone. So a cast is parsed here (afterburned container: Fver, Fcdr,
    /// ABMP, ILS; then CAS*, KEY*, CASt, STXT) and accepted only when it is what a furni cast is: bitmaps, fields and
    /// palettes, no scripts, member names that belong to the furni, a known furni class.
    /// </summary>
    public static class FurniCastValidator
    {
        public static readonly Regex SpritePattern = new("^[a-z0-9_]{1,64}$", RegexOptions.Compiled);

        /// <summary>Chunks a furni cast is made of (all 17 Habbo furni casts checked have exactly these).</summary>
        private static readonly HashSet<string> AllowedChunks = new() { "KEY*", "CAS*", "CASt", "BITD", "ALFA", "CLUT", "STXT", "DRCF", "Fmap", "ccl ", "Cinf", "VERS", "LctX", "Lnam" };

        /// <summary>Member types: bitmap, field, palette.</summary>
        private static readonly HashSet<uint> AllowedTypes = new() { 1, 3, 4 };

        /// <summary>The client's furni classes (hh_furni_classes and the furni of hh_roomdimmer, hh_soundmachine,
        /// hh_window_class) that asset.index may name.</summary>
        public static readonly HashSet<string> FurniClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Active Object Class", "Active Object Extension Class", "Birdie Class", "Credit Furni Class", "Crossfade Furni Class",
            "E-Dice Class", "FadeAnimation Furni Class", "Fortune Class", "Furniture Barrier Class", "Furniture Bottle Class",
            "Furniture Club TV Class", "Furniture Divider Class", "Furniture Edicehc Class", "Furniture Food Class",
            "Furniture Fridge Class", "Furniture Hockeylight Class", "Furniture Holo Class", "Furniture IcecreamMachine Class",
            "Furniture Luxus TV Class", "Furniture OneWayDoor Class", "Furniture PartyBeamer Class", "Furniture Present Class",
            "Furniture Red TV Class", "Furniture Samovar Class", "Furniture Scifidoor Class", "Furniture Scifiport Class",
            "Furniture Scifirocket Class", "Furniture Score Class", "Furniture Sink Class", "Furniture Solarium Class",
            "Furniture StudyDesk Class", "Furniture Teleport Class", "Furniture Toilet Class", "Furniture TP Class",
            "Furniture Traffic Light Class", "Furniture Waterbowl Class", "Habbo Wheel Class", "Item Object Class",
            "Item Object Extension Class", "Move Blocker Class", "Package Card Class", "Passive Object Class", "Pet Toy Class",
            "Plate Class", "PostIt Item Class", "Queue Class", "Queue Public Class",
            "State Change Animated Active Object Extension Class", "Trophy Class", "Valentine Randomizer Class",
            "Watermatic Class", "Furniture Roomdimmer Class", "Furniture Sound Machine Class", "Furniture Song Disk Class",
            "Furniture Jukebox Class", "Window Class", "Window ZShift Class",
        };

        private static readonly HashSet<string> SharedFields = new(StringComparer.OrdinalIgnoreCase) { "memberalias.index", "asset.index", "copyright" };

        public class Report
        {
            public bool Ok => Errors.Count == 0;
            public List<string> Errors { get; } = new();
            public List<string> Warnings { get; } = new();
            public int Bytes { get; set; }
            public string Sha256 { get; set; }
            public int Bitmaps { get; set; }
            public int Fields { get; set; }
            public int Palettes { get; set; }
            public List<string> Classes { get; } = new();
            /// <summary>The cast's size from its member names (length, width), when it has floor layers.</summary>
            public int? Length { get; set; }
            public int? Width { get; set; }
            public List<string> Members { get; } = new();
        }

        /// <summary>Whether a member name belongs to furni <paramref name="sprite"/>.</summary>
        public static bool OwnName(string name, string sprite)
        {
            if (string.IsNullOrEmpty(name) || SharedFields.Contains(name)) return true;
            var n = name.ToLowerInvariant();
            foreach (var prefix in new[] { "", "s_" })
            {
                var cls = prefix + sprite;
                if (n == cls || n.StartsWith(cls + "_") || n.StartsWith(cls + ".")) return true;
                foreach (var wall in new[] { "leftwall ", "rightwall " })
                {
                    var w = prefix + wall + sprite;
                    if (n == w || n.StartsWith(w + "_")) return true;
                }
            }
            return false;
        }

        public static Report Validate(byte[] data, string sprite, int maxBytes, bool small = false)
        {
            var r = new Report { Bytes = data.Length, Sha256 = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant() };
            if (!SpritePattern.IsMatch(sprite ?? "")) r.Errors.Add("The sprite must be lower-case letters, digits and _ (at most 64)");
            if (data.Length > maxBytes) r.Errors.Add($"The cast is {data.Length:N0} bytes; at most {maxBytes:N0} are allowed");
            if (!r.Ok) return r;

            Dictionary<uint, (string Tag, byte[] Data)> chunks;
            bool littleEndian;
            try
            {
                chunks = ReadAfterburned(data, out littleEndian);
            }
            catch (Exception e)
            {
                r.Errors.Add("Not a readable afterburned cast: " + e.Message);
                return r;
            }

            foreach (var tag in chunks.Values.Select(c => c.Tag).Distinct())
            {
                if (tag == "Lscr") r.Errors.Add("The cast contains Lingo scripts (Lscr)");
                else if (!AllowedChunks.Contains(tag)) r.Errors.Add($"Chunk type '{tag}' doesn't belong in a furni cast");
            }
            if (!r.Ok) return r;

            var cas = chunks.Values.FirstOrDefault(c => c.Tag == "CAS*").Data;
            var key = chunks.Values.FirstOrDefault(c => c.Tag == "KEY*").Data;
            if (cas == null || key == null)
            {
                r.Errors.Add("No cast member table (CAS*/KEY*)");
                return r;
            }
            var media = new List<(uint Section, uint Cast, string Tag)>();
            var used = U32(key, 8, littleEndian);
            for (var i = 0; i < used && 12 + i * 12 + 12 <= key.Length; i++)
            {
                var at = 12 + i * 12;
                media.Add((U32(key, at, littleEndian), U32(key, at + 4, littleEndian), Tag(key, at + 8, littleEndian)));
            }

            var fieldText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dims = new HashSet<(int, int)>();
            var floorName = new Regex("^(s_)?" + Regex.Escape(sprite) + "_[a-z]_0_(\\d+)_(\\d+)_\\d+_\\d+$");
            for (var i = 0; i + 4 <= cas.Length; i += 4)
            {
                var id = U32(cas, i, false);
                if (id == 0 || !chunks.TryGetValue(id, out var member) || member.Tag != "CASt") continue;
                var m = member.Data;
                if (m.Length < 12) { r.Errors.Add($"Member {i / 4 + 1} is damaged"); continue; }
                var type = U32(m, 0, false);
                var infoLen = (int)U32(m, 4, false);
                var name = MemberName(m, 12, infoLen);
                if (!AllowedTypes.Contains(type)) { r.Errors.Add($"Member {i / 4 + 1} '{name}' is of type {type}: only bitmaps, fields and palettes"); continue; }
                if (type == 1) r.Bitmaps++;
                else if (type == 3) r.Fields++;
                else r.Palettes++;
                r.Members.Add(name);
                if (type != 4 && !OwnName(name, sprite)) r.Errors.Add($"Member '{name}' isn't named after the furni ({sprite}...)");
                var f = floorName.Match(name.ToLowerInvariant());
                if (f.Success) dims.Add((int.Parse(f.Groups[2].Value), int.Parse(f.Groups[3].Value)));
                if (type == 3)
                {
                    var stxt = media.FirstOrDefault(k => k.Cast == id && k.Tag == "STXT");
                    if (stxt.Section != 0 && chunks.TryGetValue(stxt.Section, out var t) && t.Data.Length >= 12)
                    {
                        var len = (int)U32(t.Data, 4, false);
                        var hl = (int)U32(t.Data, 0, false);
                        if (hl + len <= t.Data.Length) fieldText[name] = Encoding.Latin1.GetString(t.Data, hl, len);
                    }
                }
            }
            if (dims.Count == 1) (r.Length, r.Width) = (dims.First().Item1, dims.First().Item2);
            else if (dims.Count > 1) r.Warnings.Add("Layers have different sizes in their names: " + string.Join(", ", dims.Select(d => $"{d.Item1}×{d.Item2}")));

            if (fieldText.TryGetValue("asset.index", out var asset))
            {
                var idMatch = Regex.Match(asset, "#id\\s*:\\s*\"([^\"]*)\"");
                var id = idMatch.Success ? idMatch.Groups[1].Value : "";
                if (id != sprite && id != "s_" + sprite) r.Errors.Add($"asset.index names '{id}', not {sprite}");
                var classes = Regex.Match(asset, "#classes\\s*:\\s*\\[([^\\]]*)\\]");
                foreach (Match c in Regex.Matches(classes.Success ? classes.Groups[1].Value : "", "\"([^\"]*)\""))
                {
                    r.Classes.Add(c.Groups[1].Value);
                    if (!FurniClasses.Contains(c.Groups[1].Value)) r.Errors.Add($"asset.index names the class '{c.Groups[1].Value}', which isn't a furni class of the client");
                }
            }
            else
            {
                if (!small) r.Warnings.Add("No asset.index: the client will treat it as plain furni (Active Object Class)");
            }

            if (fieldText.TryGetValue("memberalias.index", out var aliases))
            {
                foreach (var line in aliases.Split('\r', '\n'))
                {
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var from = line[..eq].Trim();
                    var to = line[(eq + 1)..].Trim().TrimEnd('*');
                    if (!OwnName(from, sprite) || !OwnName(to, sprite)) r.Errors.Add($"memberalias.index line '{line.Trim()}' names another furni's members");
                }
            }
            if (r.Bitmaps == 0) r.Errors.Add("The cast has no pictures");
            return r;
        }

        // --- afterburned container ---------------------------------------------------------------------------------

        private static uint U32(byte[] b, int at, bool le) =>
            le ? (uint)(b[at] | b[at + 1] << 8 | b[at + 2] << 16 | b[at + 3] << 24) : (uint)(b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3]);

        private static string Tag(byte[] b, int at, bool le) =>
            le ? Encoding.ASCII.GetString(new[] { b[at + 3], b[at + 2], b[at + 1], b[at] }) : Encoding.ASCII.GetString(b, at, 4);

        private static uint VarInt(byte[] b, ref int p)
        {
            uint v = 0;
            for (var i = 0; i < 5 && p < b.Length; i++)
            {
                var c = b[p++];
                v = (v << 7) | (uint)(c & 0x7f);
                if ((c & 0x80) == 0) return v;
            }
            return v;
        }

        private static byte[] Inflate(byte[] b, int start, int length)
        {
            using var input = new MemoryStream(b, start, length);
            using var z = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[65536];
            int n;
            while ((n = z.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, n);
                if (output.Length > 64 * 1024 * 1024) throw new InvalidDataException("too large when unpacked");
            }
            return output.ToArray();
        }

        /// <summary>Resource id -> (tag, decoded data), as the cast editor's container reader does.</summary>
        private static Dictionary<uint, (string, byte[])> ReadAfterburned(byte[] d, out bool littleEndian)
        {
            if (d.Length < 16) throw new InvalidDataException("too short");
            var sig = Encoding.ASCII.GetString(d, 0, 4);
            if (sig != "XFIR" && sig != "RIFX") throw new InvalidDataException("no RIFX/XFIR header");
            var le = sig == "XFIR";
            littleEndian = le;
            if (Tag(d, 8, le) != "FGDC") throw new InvalidDataException("not an afterburned cast (FGDC)");
            var p = 12;
            void Expect(string want)
            {
                var t = Tag(d, p, le);
                p += 4;
                if (t != want) throw new InvalidDataException($"expected {want}, found {t}");
            }
            Expect("Fver");
            var fverLen = (int)VarInt(d, ref p); // (not p += VarInt(d, ref p): that adds to p's value from before the varint)
            p += fverLen;
            Expect("Fcdr");
            var fcdrLen = (int)VarInt(d, ref p);
            p += fcdrLen;
            Expect("ABMP");
            var abmpLen = (int)VarInt(d, ref p);
            var abmpEnd = p + abmpLen;
            VarInt(d, ref p);
            VarInt(d, ref p);
            var abmp = Inflate(d, p, abmpEnd - p);
            p = abmpEnd;
            var ap = 0;
            VarInt(abmp, ref ap);
            VarInt(abmp, ref ap);
            var count = VarInt(abmp, ref ap);
            if (count > 100000) throw new InvalidDataException("too many resources");
            var entries = new List<(uint Id, uint Offset, uint Comp, string Tag)>();
            for (var i = 0; i < count; i++)
            {
                var id = VarInt(abmp, ref ap);
                var offset = VarInt(abmp, ref ap);
                var comp = VarInt(abmp, ref ap);
                VarInt(abmp, ref ap);
                VarInt(abmp, ref ap);
                if (ap + 4 > abmp.Length) throw new InvalidDataException("resource map cut off");
                var tag = Tag(abmp, ap, le);
                ap += 4;
                entries.Add((id, offset, comp, tag));
            }
            Expect("FGEI");
            VarInt(d, ref p);
            var ilsBody = p;
            var ils = entries.FirstOrDefault(e => e.Id == 2);
            if (ils.Tag == null) throw new InvalidDataException("no ILS");
            if (ilsBody + ils.Comp > d.Length) throw new InvalidDataException("ILS past the end");
            var ilsData = Inflate(d, ilsBody, (int)ils.Comp);
            var byId = entries.ToDictionary(e => e.Id, e => e);
            var result = new Dictionary<uint, (string, byte[])>();
            var ip = 0;
            while (ip < ilsData.Length)
            {
                var id = VarInt(ilsData, ref ip);
                if (!byId.TryGetValue(id, out var e)) break;
                var len = (int)Math.Min(e.Comp, (uint)(ilsData.Length - ip));
                result[id] = (e.Tag, ilsData.Skip(ip).Take(len).ToArray());
                ip += len;
            }
            foreach (var e in entries)
            {
                if (e.Id == 2 || result.ContainsKey(e.Id) || e.Tag[0] == '\0') continue;
                var start = ilsBody + (long)e.Offset;
                if (start + e.Comp > d.Length) throw new InvalidDataException($"resource {e.Id} past the end");
                byte[] payload;
                try { payload = e.Comp == 0 ? Array.Empty<byte>() : Inflate(d, (int)start, (int)e.Comp); }
                catch (InvalidDataException) { payload = d.Skip((int)start).Take((int)e.Comp).ToArray(); }
                result[e.Id] = (e.Tag, payload);
            }
            return result;
        }

        /// <summary>A CASt's name: info item 1, a Pascal string.</summary>
        private static string MemberName(byte[] m, int infoStart, int infoLen)
        {
            if (infoLen < 20 || infoStart + infoLen > m.Length) return "";
            var dataOffset = (int)U32(m, infoStart, false);
            var table = infoStart + dataOffset;
            if (table + 2 > m.Length) return "";
            var n = m[table] << 8 | m[table + 1];
            if (n < 2) return "";
            var itemsStart = table + 2 + (n + 1) * 4;
            var a = itemsStart + (int)U32(m, table + 2 + 4, false);
            var z = itemsStart + (int)U32(m, table + 2 + 8, false);
            if (a >= z || z > m.Length) return "";
            var len = Math.Min(m[a], z - a - 1);
            return Encoding.Latin1.GetString(m, a + 1, len);
        }
    }
}
