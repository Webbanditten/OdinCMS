using System.Collections.Generic;

namespace KeplerCMS.Avatara.Figure
{
    public class FigureSet
    {
        public List<FigurePart> FigureParts;
        public List<string> HiddenLayers;
        public string SetType;
        public string Id;
        public string Gender;
        public bool Club;
        public bool Colourable;
        public bool Selectable;

        public FigureSet(string setType, string id, string gender, bool club, bool colourable, bool selectable)
        {
            this.SetType = setType;
            this.Id = id;
            this.Gender = gender;
            this.Club = club;
            this.Colourable = colourable;
            this.Selectable = selectable;
            this.FigureParts = new List<FigurePart>();
            this.HiddenLayers = new List<string>();
        }
    }
}