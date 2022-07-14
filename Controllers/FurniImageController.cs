using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using KeplerCMS.Chroma;
using System.Security.Cryptography;
using System.Text;
using KeplerCMS.Helpers;

namespace KeplerCMS.Controllers
{
    public class FurniImageController : Controller
    {
        public FurniImageController()
        {
        }

        [Route("habbo-imaging/furni")]
        public async Task<IActionResult> Index(int traxId)
        {
            bool isSmallFurni = false;
            int renderState = 0;
            int renderDirection = 0;
            int color = 0;
            string sprite = null;
            bool renderBackground = false;
            bool renderShadows = false;
            bool cropImage = true;
            string renderCanvasColour = "transparent";
            bool renderIcon = false;


            if (Request.Query.ContainsKey("sprite"))
            {
                Request.Query.TryGetValue("sprite", out var value);
                sprite = value.ToString();
            }

            if (Request.Query.ContainsKey("s"))
            {
                Request.Query.TryGetValue("s", out var value);

                if (value == "1" || value == "true")
                {
                    isSmallFurni = true;
                }
            }

            if (Request.Query.ContainsKey("small"))
            {
                Request.Query.TryGetValue("small", out var value);

                if (value == "1" || value == "true")
                {
                    isSmallFurni = true;
                }
            }

            if (Request.Query.ContainsKey("state"))
            {
                Request.Query.TryGetValue("state", out var value);

                if (value.ToString().IsNumeric())
                {
                    renderState = int.Parse(value.ToString());

                    if (renderState >= 101)
                    {
                        renderState = 0;
                    }
                }
            }

            if (Request.Query.ContainsKey("direction"))
            {
                Request.Query.TryGetValue("direction", out var value);

                if (value.ToString().IsNumeric())
                {
                    renderDirection = int.Parse(value.ToString());
                }
            }

            if (Request.Query.ContainsKey("rotation"))
            {
                Request.Query.TryGetValue("rotation", out var value);

                if (value.ToString().IsNumeric())
                {
                    renderDirection = int.Parse(value.ToString());
                }
            }

            if (Request.Query.ContainsKey("color"))
            {
                Request.Query.TryGetValue("color", out var value);

                if (value.ToString().IsNumeric())
                {
                    color = int.Parse(value.ToString());

                    if (color >= 16)
                    {
                        color = 0;
                    }
                }
            }

            if (Request.Query.ContainsKey("colour"))
            {
                Request.Query.TryGetValue("colour", out var value);

                if (StringHelper.IsNumeric(value.ToString()))
                {
                    color = int.Parse(value.ToString());

                    if (color >= 16)
                    {
                        color = 0;
                    }
                }
            }

            if (Request.Query.ContainsKey("bg"))
            {
                Request.Query.TryGetValue("bg", out var value);
                renderBackground = !(value.ToString().Equals("0") || value.ToString().Equals("false"));
            }

            if (Request.Query.ContainsKey("crop"))
            {
                Request.Query.TryGetValue("crop", out var value);
                cropImage = (value.ToString().Equals("1") || value.ToString().Equals("true"));
            }

            if (Request.Query.ContainsKey("shadow"))
            {
                Request.Query.TryGetValue("shadow", out var value);
                renderShadows = (value.ToString().Equals("1") || value.ToString().Equals("true"));
            }

            if (Request.Query.ContainsKey("canvas"))
            {
                Request.Query.TryGetValue("canvas", out var value);
                renderCanvasColour = value.ToString();
            }


            if (Request.Query.ContainsKey("icon"))
            {
                Request.Query.TryGetValue("icon", out var value);
                renderIcon = (value.ToString().Equals("1") || value.ToString().Equals("true"));
            }

            if (sprite != null && sprite.Length > 0)
            {
                string fileNameUnique = string.Concat(sprite, isSmallFurni, renderState, renderDirection, color, renderShadows, renderBackground, renderCanvasColour, cropImage, renderIcon);
                string hashedUniqueName = Hash(fileNameUnique);

                if (!System.IO.Directory.Exists("furni_export/" + sprite + "/export"))
                {
                    Directory.CreateDirectory("furni_export/" + sprite + "/export");
                }

                if (!System.IO.File.Exists("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png"))
                {
                    if (sprite != null && sprite.Length > 0)
                    {
                        if(!System.IO.File.Exists("swfs/hof_furni/" + sprite + ".swf")) return NotFound();


                        var furni = new ChromaFurniture("swfs/hof_furni/" + sprite + ".swf",
                                        isSmallFurni: isSmallFurni, renderState: renderState,
                                        renderDirection: renderDirection, colourId: color,
                                        renderShadows: renderShadows, renderBackground: renderBackground,
                                        renderCanvasColour: renderCanvasColour, cropImage: cropImage, renderIcon: renderIcon);
                        furni.Run();
                        var bytes = furni.CreateImage();

                        if (bytes != null)
                        {
                            System.IO.File.WriteAllBytes("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png", bytes);
                        }
                        else
                        {
                            System.IO.File.WriteAllBytes("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png", new byte[0]);

                        }

                    }
                }

                if (System.IO.File.Exists("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png"))
                {
                    Response.Headers.Add("Cache-Control", "public, max-age=31536000");
                    return File(System.IO.File.ReadAllBytes("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png"), "image/png");
                }
            }

            return NotFound();
        }
        static string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

    }
}
