using KeplerCMS.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace KeplerCMS.Helpers
{
    public class FigurePart
    {
        public int Sprite { get; set; }
        public int Color { get; set; }

        public FigurePart(int sprite, int color)
        {
            Sprite = sprite;
            Color = color;
        }

        public FigurePart(string sprite, string color)
        {
            Sprite = int.Parse(sprite);
            Color = int.Parse(color);
        }

        public override string ToString()
        {
            return $"{Sprite}{Color}";
        }
    }
    public class Figure
    {
        public FigurePart HR { get; set; }
        public FigurePart HD { get; set; }
        public FigurePart CH { get; set; }
        public FigurePart LG { get; set; }
        public FigurePart SH { get; set; }
    }

    public class FigureHelper
    {
        public static Figure GetFigureFromString(string input)
        {
            var figure = new Figure();


            var hr = input.Substring(0, 5);
            figure.HR = new FigurePart(hr.Substring(0,3), hr.Substring(3,2));


            var hd = input.Substring(5, 5);
            figure.HD = new FigurePart(hd.Substring(0, 3), hd.Substring(3, 2));


            var lg = input.Substring(10, 5);
            figure.LG = new FigurePart(lg.Substring(0, 3), lg.Substring(3, 2));



            var sh = input.Substring(15, 5);
            figure.SH = new FigurePart(sh.Substring(0, 3), sh.Substring(3, 2));

            var ch = input.Substring(20, 5);
            figure.CH = new FigurePart(ch.Substring(0, 3), ch.Substring(3, 2));


            



            return figure;
        }

        public static string FixFigure(string input)
        {
            var hr = input.Substring(0, 5);
            var hd = input.Substring(5, 5);
            var ch = input.Substring(10, 5);
            var lg = input.Substring(15, 5);
            var sh = input.Substring(20, 5);

            return hr + hd + lg + sh + ch; 
        }

        public static string GetOldColorFromFigureList(string iPart, int iSprite, int iColorIndex)
        {
            //iColorIndex = iColorIndex.Trim('0');
            dynamic figureData = JObject.Parse(Statics.figureData);

            foreach (var gender in figureData["colors"])
            {
                foreach (var parts in gender) {
                   
                    var partsWithKey = parts.First.ToObject<Dictionary<string, dynamic>>();

                    foreach (var part in partsWithKey)
                    {
                        if(part.Key == iPart)
                        {
                            foreach (var spriteDef in part.Value)
                            {
                                var sprite = spriteDef.First;
                                string spriteId = sprite["s"];
                                var spriteColors = sprite["c"];

                                if (spriteId == iSprite.ToString()) {
                                    return spriteColors[iColorIndex - 1];
                                }

                            }
                        }

                    }
                }
            }
            return "ERR";
        }

        public static string ConvertOldColorToNew(string iPart, int iSprite, int iColorIndex)
        {
            //iColorIndex = iColorIndex.Trim('0');
            dynamic newFigureData = JObject.Parse(Statics.newFigureData);
            var color = GetOldColorFromFigureList(iPart, iSprite, iColorIndex);

            /*
             foreach ($json_a["palette"] as $paletteIndex => $paletteValue) {
                foreach ($paletteValue as $colorIndex => $colorValue) {
                    if($color == $colorValue["color"]) {

                        return $colorIndex;
                    }
                }
            }
             */
            var paletteWithKey = newFigureData["palette"].ToObject<Dictionary<string, dynamic>>();

            foreach (var palette in paletteWithKey)
            {
                var colorsWithKey = palette.Value.ToObject<Dictionary<string, dynamic>>();

                foreach (var colorWithKey in colorsWithKey)
                {
                    if(colorWithKey.Value["color"] == color)
                    {
                        return colorWithKey.Key;
                    }
                }
            }
            return null;
        }

        public static string TakeCareOfHats(int spriteId, string colorId)
        {
            switch (spriteId) {
            // REggae
            case 120:
                return ".hr-676-61.ha-1001-0.fa-1201-62";
            // Cap
            case 525:
            case 140:
                return ".ha-1002-" + colorId;
            // Comfy beanie
            case 150:
            case 535:
                return ".ha-1003-" + colorId;
            //Fishing hat
            case 160:
            case 565:
                return ".ha-1004-" + colorId;
            // Bandana
            case 570:
                return ".ha-1005-" + colorId;
            // Xmas beanie
            case 585:
            case 175:
                return ".ha-1006-0";
            // Xmas rodolph
            case 580:
            case 176:
                return ".ha-1007-0.fa-1202-1412";
            // Bunny
            case 590:
            case 177:
                return ".ha-1008-0.fa-1202-1327";
            // Hard Hat
            case 178:
                    return ".ha-1009-1321";
            // Boring beanie
            case 595:
                return ".ha-1010-" + colorId;
            // HC Beard hat
            case 801:
                return ".hr-829-" + colorId + ".fa-1201-62.ha-1011-" + colorId;

            // HC Beanie
            case 800:
            case 810:
                return ".ha-1012-" + colorId;
            // HC Cowboy Hat
            case 802:
            case 811:
            return ".ha-1013-" + colorId;
                default:
            return ".ha-0-" + colorId;
            }
        }

        public static int FixLegs(int part, int direction)
        {
            if (part == 275 && direction != 2 && direction != 4) {
                return 3116;
            }
            return part;
        }

        public static string ConvertFigure(string figure, int direction)
        {
            var fig = GetFigureFromString(figure);
            string buildFigure = "";
            buildFigure += $"hr-{fig.HR.Sprite}-{ConvertOldColorToNew("hr", fig.HR.Sprite, fig.HR.Color)}";
            buildFigure += $".hd-{fig.HD.Sprite}-{ConvertOldColorToNew("hd", fig.HD.Sprite, fig.HD.Color)}";
            buildFigure += $".ch-{fig.CH.Sprite}-{ConvertOldColorToNew("ch", fig.CH.Sprite, fig.CH.Color)}";
            buildFigure += $".lg-{FixLegs(fig.LG.Sprite, direction)}-{ConvertOldColorToNew("lg", fig.LG.Sprite, fig.LG.Color)}";
            buildFigure += $".sh-{fig.SH.Sprite}-{ConvertOldColorToNew("sh", fig.SH.Sprite, fig.SH.Color)}";
            buildFigure += TakeCareOfHats(fig.HR.Sprite, ConvertOldColorToNew("hr", fig.HR.Sprite, fig.HR.Color));
            return buildFigure;
        }
    }
}
