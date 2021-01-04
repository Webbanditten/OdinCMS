namespace KeplerCMS.Avatara.Figure
{
    public class FigureColor
    {
        public string ColourId;
        public string Index;
        public bool IsClubRequired;
        public bool IsSelectable;
        public string HexColor;

        public FigureColor(string colourId, string index, bool isClubRequired, bool isSelectable, string HexColor)
        {
            this.ColourId = colourId;
            this.Index = index;
            this.IsClubRequired = isClubRequired;
            this.IsSelectable = isSelectable;
            this.HexColor = HexColor;
        }
    }
}