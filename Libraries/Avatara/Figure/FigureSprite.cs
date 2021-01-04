using System.Collections.Generic;

namespace KeplerCMS.Avatara.Figure
{
    public class FigureSprite
    {
        public OldFigurePart[] FigureParts;
        public string SetType;
        public string Id;
        public string Gender;

        public FigureSprite(string setType, string id, string gender, OldFigurePart[] parts)
        {
            this.SetType = setType;
            this.Id = id;
            this.Gender = gender;
            this.FigureParts = parts;
        }
    }
}