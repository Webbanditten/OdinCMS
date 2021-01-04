namespace KeplerCMS.Avatara.Figure
{
    public class FigureSetType
    {
        public string Set;
        public int PaletteId;
        public bool IsMandatory;

        public FigureSetType(string set, int paletteId, bool isMandatory)
        {
            this.Set = set;
            this.PaletteId = paletteId;
            this.IsMandatory = isMandatory;
        }
    }
}