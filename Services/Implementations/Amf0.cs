using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Flash Remoting (AMF0) as the 2006 Christmas e-card (wwwroot/flash/xmas06/ecard.swf, Macromedia's mx.remoting) speaks it
    /// to /api/ecard/gateway: a request is a version, headers and bodies; each body names a service method ("Ecard.saveCard")
    /// and a response id ("/1") and carries the arguments, usually as a strict array. The reply is one body to
    /// "&lt;response id&gt;/onResult" holding the result. Values: numbers (double), booleans, strings, null/undefined, objects
    /// and ECMA arrays (Dictionary), strict arrays (List), dates (DateTime), XML documents (string).
    /// </summary>
    public static class Amf0
    {
        public record Body(string Target, string Response, object Value);

        public record Request(int Version, List<Body> Bodies);

        public static Request Read(byte[] data)
        {
            var reader = new Reader(data);
            var version = reader.U16();
            var headers = reader.U16();
            for (var i = 0; i < headers; i++)
            {
                reader.Utf8();       // name
                reader.Byte();       // must understand
                reader.U32();        // length
                reader.Value();
            }
            var count = reader.U16();
            var bodies = new List<Body>();
            for (var i = 0; i < count; i++)
            {
                var target = reader.Utf8();
                var response = reader.Utf8();
                reader.U32();        // length (often 0xFFFFFFFF)
                bodies.Add(new Body(target, response, reader.Value()));
            }
            return new Request(version, bodies);
        }

        /// <summary>A reply to one call: the result for "&lt;responseId&gt;/onResult".</summary>
        public static byte[] Result(string responseId, object value)
        {
            using var stream = new MemoryStream();
            var writer = new Writer(stream);
            writer.U16(0);           // version
            writer.U16(0);           // headers
            writer.U16(1);           // bodies
            writer.Utf8((responseId ?? "/1") + "/onResult");
            writer.Utf8("null");
            var body = new MemoryStream();
            new Writer(body).Value(value);
            writer.U32((uint)body.Length);
            body.Position = 0;
            body.CopyTo(stream);
            return stream.ToArray();
        }

        /// <summary>The arguments of a call: the strict array's items, or the single value.</summary>
        public static List<object> Arguments(object value) => value is List<object> list ? list : new List<object> { value };

        private sealed class Reader
        {
            private readonly byte[] _data;
            private int _pos;
            private readonly List<object> _references = new();

            public Reader(byte[] data) => _data = data;

            private void Need(int n)
            {
                if (_pos + n > _data.Length) throw new FormatException("AMF0: unexpected end of data");
            }

            public byte Byte() { Need(1); return _data[_pos++]; }
            public ushort U16() { Need(2); var v = BinaryPrimitives.ReadUInt16BigEndian(_data.AsSpan(_pos)); _pos += 2; return v; }
            public uint U32() { Need(4); var v = BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(_pos)); _pos += 4; return v; }
            private double Double() { Need(8); var v = BinaryPrimitives.ReadDoubleBigEndian(_data.AsSpan(_pos)); _pos += 8; return v; }
            private string Text(int length) { Need(length); var s = Encoding.UTF8.GetString(_data, _pos, length); _pos += length; return s; }
            public string Utf8() => Text(U16());

            public object Value(int depth = 0)
            {
                if (depth > 32) throw new FormatException("AMF0: nested too deep");
                var marker = Byte();
                switch (marker)
                {
                    case 0x00: return Double();
                    case 0x01: return Byte() != 0;
                    case 0x02: return Utf8();
                    case 0x03: return Properties(new Dictionary<string, object>(), depth);
                    case 0x05: return null;
                    case 0x06: return null;  // undefined
                    case 0x07:
                    {
                        var index = U16();
                        return index < _references.Count ? _references[index] : null;
                    }
                    case 0x08:
                        U32();               // ECMA array count (a hint)
                        return Properties(new Dictionary<string, object>(), depth);
                    case 0x0A:
                    {
                        var count = U32();
                        if (count > 10000) throw new FormatException("AMF0: array too long");
                        var list = new List<object>();
                        _references.Add(list);
                        for (var i = 0; i < count; i++) list.Add(Value(depth + 1));
                        return list;
                    }
                    case 0x0B:
                    {
                        var ms = Double();
                        U16();               // time zone (unused)
                        return DateTime.UnixEpoch.AddMilliseconds(ms);
                    }
                    case 0x0C: return Text((int)Math.Min(U32(), int.MaxValue));
                    case 0x0F: return Text((int)Math.Min(U32(), int.MaxValue)); // XML document
                    case 0x10:
                        Utf8();              // class name
                        return Properties(new Dictionary<string, object>(), depth);
                    default:
                        throw new FormatException("AMF0: unsupported type 0x" + marker.ToString("x2"));
                }
            }

            private Dictionary<string, object> Properties(Dictionary<string, object> target, int depth)
            {
                _references.Add(target);
                while (true)
                {
                    var key = Utf8();
                    if (key.Length == 0)
                    {
                        if (Byte() != 0x09) throw new FormatException("AMF0: object without end marker");
                        return target;
                    }
                    target[key] = Value(depth + 1);
                }
            }
        }

        private sealed class Writer
        {
            private readonly Stream _stream;
            public Writer(Stream stream) => _stream = stream;

            public void U16(ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, v); _stream.Write(b); }
            public void U32(uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); _stream.Write(b); }

            public void Utf8(string s)
            {
                var bytes = Encoding.UTF8.GetBytes(s ?? "");
                U16((ushort)Math.Min(bytes.Length, ushort.MaxValue));
                _stream.Write(bytes, 0, Math.Min(bytes.Length, ushort.MaxValue));
            }

            public void Value(object value)
            {
                switch (value)
                {
                    case null:
                        _stream.WriteByte(0x05);
                        break;
                    case bool b:
                        _stream.WriteByte(0x01);
                        _stream.WriteByte(b ? (byte)1 : (byte)0);
                        break;
                    case int or long or double or float:
                    {
                        _stream.WriteByte(0x00);
                        Span<byte> d = stackalloc byte[8];
                        BinaryPrimitives.WriteDoubleBigEndian(d, Convert.ToDouble(value));
                        _stream.Write(d);
                        break;
                    }
                    case string s:
                    {
                        var bytes = Encoding.UTF8.GetBytes(s);
                        if (bytes.Length <= ushort.MaxValue)
                        {
                            _stream.WriteByte(0x02);
                            U16((ushort)bytes.Length);
                        }
                        else
                        {
                            _stream.WriteByte(0x0C);
                            U32((uint)bytes.Length);
                        }
                        _stream.Write(bytes);
                        break;
                    }
                    case IDictionary<string, object> map:
                        _stream.WriteByte(0x03);
                        foreach (var (k, v) in map)
                        {
                            Utf8(k);
                            Value(v);
                        }
                        U16(0);
                        _stream.WriteByte(0x09);
                        break;
                    case IList<object> list:
                        _stream.WriteByte(0x0A);
                        U32((uint)list.Count);
                        foreach (var item in list) Value(item);
                        break;
                    default:
                        Value(value.ToString());
                        break;
                }
            }
        }
    }
}
