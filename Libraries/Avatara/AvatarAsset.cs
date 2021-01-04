using KeplerCMS.Avatara.Figure;

namespace KeplerCMS.Avatara
{
    public class AvatarAsset
    {
        public string Name;
        public int X;
        public int Y;
        public int ImageX;
        public int ImageY;
        public string FileName;
        public FigurePart Part;
        public FigureSet Set;
        public string[] Parts;
        public int RenderOrder;
        public bool IsDrinkCanvas;

        public AvatarAsset(bool isSmall, string action, string name, string fileName, int X, int Y, FigurePart part, FigureSet set, int canvasW, int canvasH, string[] parts)
        {
            this.Name = name;
            this.X = X;
            this.Y = Y;
            this.FileName = fileName;
            this.Part = part;
            this.Set = set;
            this.Parts = parts;
            this.RenderOrder = this.Part.OrderId;
            this.IsDrinkCanvas = false;

            if (action == "lay")
            {
                this.ImageY = Y + (canvasW / 2) + (isSmall ? -5 : -20);
                this.ImageX = X + (canvasH / 2) - (isSmall ? -11 : -10);
            }
            else
            {
                this.ImageX = X + (canvasW / 2) + (isSmall ? 5 : 10);
                this.ImageY = Y + (canvasH / 2) - (isSmall ? 11 : 20);
            }
        }

    }
}