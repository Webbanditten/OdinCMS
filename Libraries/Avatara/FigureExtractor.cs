using KeplerCMS.Avatara.Figure;
using KeplerCMS.Avatara.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace KeplerCMS.Avatara
{
    public class FigureExtractor
    {
        public static Dictionary<string, FigureDocument> Parts;
        private static string assetFolder = "wwwroot/images/habbo-imaging/";
        public static Dictionary<string, FigureDocument> GetParts()
        {
            if (Parts == null)
                Parts = new Dictionary<string, FigureDocument>();
                
            foreach (var path in Directory.GetDirectories(assetFolder))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                string fileName = dirInfo.Name;
                var xmlFile = FileUtil.SolveXmlFile(fileName + "/xml/", "manifest");
                var list = xmlFile.SelectNodes("//manifest/library/assets/asset");

                for (int i = 0; i < list.Count; i++)
                {
                    var asset = list.Item(i);

                    if (asset.Attributes.GetNamedItem("name") == null)
                        continue;

                    var name = asset.Attributes.GetNamedItem("name").InnerText;

                    if (name.Split("_").Length < 6)
                        continue;

                    if (Parts.ContainsKey(name.Split("_")[2] + (fileName.Contains("_50_") ? "_sh" : "_h")))
                        continue;

                    Parts.Add(name.Split("_")[2] + (fileName.Contains("_50_") ? "_sh" : "_h"), new FigureDocument(fileName, xmlFile));
                }
            }

            return Parts;
           
        }
    }
}