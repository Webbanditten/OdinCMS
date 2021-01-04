using System.Xml;

namespace KeplerCMS.Avatara
{
    public class FigureDocument
    {
        public string FileName;
        public XmlDocument XmlFile;

        public FigureDocument(string fileName, XmlDocument xmlFile)
        {
            this.FileName = fileName;
            this.XmlFile = xmlFile;
        }
    }
}