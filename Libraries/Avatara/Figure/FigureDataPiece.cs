using System.Collections.Generic;

namespace KeplerCMS.Avatara.Figure
{
    public class FigureDataPiece
    {
        public FigureSprite Sprite;
        public OldFigureColor[] Colors;
        public string Gender;

        public FigureDataPiece(FigureSprite sprite, OldFigureColor[] colors, string gender)
        {
            this.Sprite = sprite;
            this.Colors = colors;
            this.Gender = gender;
        }
    }
}