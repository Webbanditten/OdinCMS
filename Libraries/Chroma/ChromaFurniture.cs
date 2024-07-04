﻿using KeplerCMS.Chroma.Extractor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using KeplerCMS.Chroma.Extensions;
using System.Drawing;
using Color = SixLabors.ImageSharp.Color;
using System;
using Newtonsoft.Json;

namespace KeplerCMS.Chroma
{
    public class ChromaFurniture
    {
        private string fileName;
        private string outputFileName;
        public bool IsSmallFurni;

        public int RenderState;
        public int RenderDirection;

        public int ColourId;
        public string Sprite;
        public List<ChromaAsset> Assets;
        public Image<Rgba32> DrawingCanvas;

        public int CANVAS_WIDTH = 1200;
        public int CANVAS_HEIGHT = 1200;
        public string CANVAS_PICTURE = "bg.png";

        public string FurniData;

        private bool RenderShadows;
        private bool RenderBackground;
        private string RenderCanvasColour;
        private bool CropImage;
        public bool IsIcon;
        private int AnimationCount;
        public SortedDictionary<int, ChromaAnimation> Animations;
        public int HighestAnimationLayer;

        //public List<ChromaAsset> BuildQueue;

        //private Dictionary<int, ChromaFrame> FrameSettings;
        //public int OverrideRenderState;
        //private bool FoundAnimations;

        public string OutputDirectory
        {
            get { return Path.Combine("furni_export", Sprite); }
        }

        public string XmlDirectory
        {
            get { return Path.Combine("furni_export", Sprite, "xml"); }
        }

        public int MaxStates { get; private set; }

        public ChromaFurniture(string inputFileName, bool isSmallFurni, int renderState, int renderDirection, int colourId = -1, bool renderShadows = false, bool renderBackground = false, string renderCanvasColour = "FEFEFE",
            bool cropImage = true, bool renderIcon = false)
        {
            this.fileName = inputFileName;
            this.IsSmallFurni = isSmallFurni;
            this.Assets = new List<ChromaAsset>();
            this.RenderState = renderState;
            this.RenderDirection = renderDirection;
            this.ColourId = colourId;
            this.Sprite = Path.GetFileNameWithoutExtension(inputFileName);
            this.outputFileName = this.GetFileName();
            this.FurniData = Path.Combine("furni_export/" + Path.GetFileNameWithoutExtension(inputFileName) + "/furni.json");
            this.RenderShadows = renderShadows;
            this.RenderBackground = renderBackground;
            this.RenderCanvasColour = renderCanvasColour;
            this.CropImage = cropImage;
            this.IsIcon = renderIcon;
        }

        public string Run()
        {
            FurniExtractor.Parse(this.fileName);

            if (this.RenderBackground)
            {
                DrawingCanvas = SixLabors.ImageSharp.Image.Load<Rgba32>(CANVAS_PICTURE);//new Image<Rgba32>(CANVAS_HEIGHT, CANVAS_WIDTH, colour);

                CANVAS_HEIGHT = DrawingCanvas.Height;
                CANVAS_WIDTH = DrawingCanvas.Width;
            }
            else
            {
                DrawingCanvas = new Image<Rgba32>(CANVAS_HEIGHT, CANVAS_WIDTH, HexToColor(this.RenderCanvasColour));
            }
;
            GenerateAnimations();
            GenerateAssets();

            var json = JsonConvert.SerializeObject(this.Animations);

            CreateBuildQueue();

            this.outputFileName = this.GetFileName();
            return this.outputFileName;
        }

        private int GetMaxFrames()
        {
            return Assets.Max(x => x.Frame);
        }

        private void GenerateAssets(bool createFiles = true)
        {
            var xmlData = FileUtil.SolveXmlFile(XmlDirectory, "assets");

            if (xmlData == null)
            {
                return;
            }

            XmlNodeList assets = xmlData.SelectNodes("//assets/asset");

            for (int i = 0; i < assets.Count; i++)
            {
                var asset = assets.Item(i);

                var X = int.Parse(asset.Attributes.GetNamedItem("x").InnerText);
                var Y = int.Parse(asset.Attributes.GetNamedItem("y").InnerText);

                string imageName = asset.Attributes.GetNamedItem("name").InnerText;

                if (imageName.Contains(".props") || imageName.StartsWith("s_" + this.Sprite))
                    continue;

                if (!IsIcon)
                {
                    if (imageName.Contains("_icon_"))
                        continue;
                }
                else
                {
                    if (!imageName.Contains("_icon_"))
                        continue;
                }

                if (asset.Attributes.GetNamedItem("source") != null)
                {
                    var newChromaAsset = new ChromaAsset(this, X, Y, asset.Attributes.GetNamedItem("source").InnerText, imageName);
                    CreateAsset(newChromaAsset, asset, createFiles);
                }
                else
                {
                    var chromaAsset = new ChromaAsset(this, X, Y, null, imageName);
                    CreateAsset(chromaAsset, asset, createFiles);
                }
            }

            this.MaxStates = 0;

            var visualization = FileUtil.SolveXmlFile(XmlDirectory, "visualization");

            if (visualization == null)
            {
                return;
            }

            XmlNodeList animations = visualization.SelectNodes("//visualizationData/visualization[@size='" + (IsSmallFurni ? "32" : "64") + "']/animations/animation");

            if (animations == null || animations.Count == 0)
                animations = visualization.SelectNodes("//visualizationData/visualization[@size='" + (IsSmallFurni ? "32" : "64") + "']/directions/direction[@id='" + RenderDirection + "']/animations/animation");

            for (int i = 0; i < animations.Count; i++)
            {
                var animation = animations.Item(i);

                if (animation.Name != "animation" && animation.Attributes.GetNamedItem("id") == null)
                    continue;

                int state = int.Parse(animation.Attributes.GetNamedItem("id").InnerText);

                if (state > MaxStates)
                    MaxStates = state;
            }

            this.MaxStates = MaxStates;
            this.HighestAnimationLayer = this.Assets.Where(x => !x.Shadow).ToList().Max(x => x.Layer) + 1;

            for (int i = 0; i < HighestAnimationLayer; i++)
            {
                //var letter = alphabet[i];
                //string letter = Convert.ToString(alphabet[i]);

                if (!this.Animations.ContainsKey(i))
                {
                    var animation = new ChromaAnimation();
                    this.Animations.Add(i, animation);

                    for (int j = 0; j < this.AnimationCount; j++)
                    {
                        if (!animation.States.ContainsKey(j))
                        {
                            var frame = new ChromaFrame();
                            frame.Frames.Add("0");

                            animation.States.Add(j, frame);
                        }
                    }
                }
            }
        }

        private void CreateAsset(ChromaAsset chromaAsset, XmlNode node, bool createFiles)
        {
            if (!chromaAsset.Parse())
                return;

            if (Assets.Count(x => x.imageName == chromaAsset.imageName) == 0)
            {
                chromaAsset.flipH = (node.Attributes.GetNamedItem("flipH") != null && node.Attributes.GetNamedItem("flipH").InnerText == "1");
                Assets.Add(chromaAsset);

                if (chromaAsset.sourceImage != null && createFiles)
                {
                    chromaAsset.GenerateImage();
                }

                chromaAsset.ImageX = chromaAsset.ImageX + (CANVAS_WIDTH / 2);// 32;
                chromaAsset.ImageY = chromaAsset.ImageY + (CANVAS_HEIGHT / 2);// 25;
            }

            if (chromaAsset.imageName.Contains("_sd_"))
            {
                chromaAsset.Shadow = true;
                chromaAsset.Z = int.MinValue;
            }
            else
            {
                chromaAsset.Z += chromaAsset.Layer;
            }
        }

        private List<ChromaAsset> CreateBuildQueue()
        {
            if (RenderState > MaxStates)
                RenderState = 0;

            var candidates = new List<ChromaAsset>(Assets).Where(x => x.IsSmall == IsSmallFurni).ToList();
            var validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();

            if (validDirections.Count == 0)
            {
                RenderDirection = 0;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            if (validDirections.Count == 0)
            {
                RenderDirection = 2;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            if (validDirections.Count == 0)
            {
                RenderDirection = 4;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            if (validDirections.Count == 0)
            {
                RenderDirection = 6;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            candidates = validDirections;

            var renderFrames = new List<ChromaAsset>();

            for (int Layer = 0; Layer < this.HighestAnimationLayer; Layer++)
            {
                //var frames = candidates.Where(x => x.IsSmall == IsSmallFurni).ToList();
                //candidates = candidates.Where(x => x.IsSmall == IsSmallFurni).ToList();
                var Frame = 0;

                if (Animations.ContainsKey(Layer) &&
                    Animations[Layer].States.Count > 0 &&
                    Animations[Layer].States.ContainsKey(RenderState))
                {
                    var json = JsonConvert.SerializeObject(Animations[Layer].States[RenderState]);
                    Frame = int.Parse(Animations[Layer].States[RenderState].Frames[0]);
                }

                renderFrames.AddRange(candidates.Where(x => x.Layer == Layer && x.Frame == Frame).ToList());
            }

            if (!this.RenderShadows)
            {
                renderFrames = renderFrames.Where(x => !x.Shadow).ToList();
            }

            /*var compulsoryFrames = new List<ChromaAsset>(Assets);

            compulsoryFrames = compulsoryFrames.Where(x => x.IsSmall == IsSmallFurni).ToList();
            compulsoryFrames = compulsoryFrames.Where(x => x.Frame == 0).ToList();

            var candidates = new List<ChromaAsset>(Assets);
            candidates = candidates.Where(x => x.IsSmall == IsSmallFurni).ToList();

            var validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();

            if (validDirections.Count == 0)
            {
                RenderDirection = 0;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            if (validDirections.Count == 0)
            {
                RenderDirection = 2;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            if (validDirections.Count == 0)
            {
                RenderDirection = 4;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            if (validDirections.Count == 0)
            {
                RenderDirection = 6;
                validDirections = candidates.Where(x => x.Direction == RenderDirection).ToList();
            }

            // If for some reason the frame/state doesn't exist
            candidates = validDirections.Where(x => x.Frame == RenderState).ToList();

            if (candidates.ToList().Count == 0)
            {
                RenderState = 0; // Reset state
                candidates = new List<ChromaAsset>(Assets);
                candidates = candidates.Where(x => x.IsSmall == IsSmallFurni).ToList();
                candidates = candidates.Where(x => x.Direction == RenderDirection).ToList();
                candidates = candidates.Where(x => x.Frame == 0).ToList();
            }

       
            // Select the missing frames ordered by direction
            if (compulsoryFrames.Count > 0)
            {
                compulsoryFrames = compulsoryFrames.Where(x => x.Direction == RenderDirection).ToList();
            }

            // Add missing frames if they don't exist
            foreach (var frame in compulsoryFrames)
            {
                if (candidates.Count(x => x.Layer == frame.Layer) > 0)
                {
                    continue;
                }

                var nextFrame = compulsoryFrames.Where(x => x.Layer == frame.Layer && x.Direction == frame.Direction && x.Frame == 0).FirstOrDefault();

                if (nextFrame != null)
                {
                    candidates.Add(nextFrame);
                }
            }

            if (!this.RenderShadows)
            {
                candidates = candidates.Where(x => !x.Shadow).ToList();
            }

            foreach (var asset in Assets)
            {
                if (candidates.Count(x => x.Layer == asset.Layer) == 0)
                {
                    var potential = Assets.Where(x => x.Layer == asset.Layer && x.Direction == RenderDirection && x.IsSmall == IsSmallFurni && !x.Shadow).ToList();

                    if (potential.Count(x => x.Frame == RenderState) > 0)
                    {
                        candidates.AddRange(potential.Where(x => x.Frame == RenderState).ToList());
                    }
                    else
                    {
                        candidates.AddRange(potential);
                    }
                }
            }

            candidates = candidates.OrderBy(x => x.Z).ToList();

            return candidates;
            */


            renderFrames = renderFrames.OrderBy(x => x.Z).ToList();
            return renderFrames;
        }

        public byte[] CreateImage()
        {
            var buildQueue = CreateBuildQueue();

            if (buildQueue == null)
                return null;

            var cropColours = new List<Rgba32>();

            if (this.CropImage)
            {
                if (this.RenderBackground)
                {
                    cropColours.Add(Color.FromRgb(142, 142, 94));
                    cropColours.Add(Color.FromRgb(152, 152, 101));
                }
                else
                {
                    cropColours.Add(HexToColor(this.RenderCanvasColour));
                }
            }

            using (var canvas = this.DrawingCanvas)
            {
                foreach (var asset in buildQueue)
                {
                    var image = SixLabors.ImageSharp.Image.Load<Rgba32>(asset.GetImagePath());

                    if (asset.Alpha != -1)
                    {
                        TintImage(image, "FFFFFF", (byte)asset.Alpha);
                    }

                    if (asset.ColourCode != null)
                    {
                        TintImage(image, asset.ColourCode, 255);
                    }

                    if (asset.Shadow)
                    {
                        image.Mutate(ctx =>
                        {
                            ctx.Opacity(0.2f);
                        });
                    }

                    var graphicsOptions = new GraphicsOptions();

                    if ((asset.Ink == "ADD" || asset.Ink == "33"))
                    {
                        graphicsOptions.ColorBlendingMode = PixelColorBlendingMode.Add;
                    }
                    else
                    {
                        graphicsOptions.ColorBlendingMode = PixelColorBlendingMode.Normal;
                    }

                    canvas.Mutate(ctx =>
                    {
                        ctx.DrawImage(image, new SixLabors.ImageSharp.Point(canvas.Width - asset.ImageX, canvas.Height - asset.ImageY), graphicsOptions);
                    });
                }


                using (Bitmap tempBitmap = canvas.ToBitmap())
                {
                    if (CropImage && cropColours.Count > 0)
                    {
                        var temp = canvas.ToBitmap();

                        // Crop the image
                        using (Bitmap croppedBitmap = ImageUtil.TrimBitmap(tempBitmap, cropColours.ToArray()))
                        {
                            return RenderImage(croppedBitmap);
                        }

                    }
                    else
                    {
                        return RenderImage(tempBitmap);
                    }
                }
            }
        }

        private byte[] RenderImage(Bitmap croppedBitmap)
        {
            return croppedBitmap.ToByteArray();
        }

        private void TintImage(Image<Rgba32> image, string colourCode, byte alpha)
        {
            var rgb = HexToColor(colourCode);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var current = image[x, y];

                    if (current.A > 0)
                    {
                        current.R = (byte)(rgb.R * current.R / 255);
                        current.G = (byte)(rgb.G * current.G / 255);
                        current.B = (byte)(rgb.B * current.B / 255);
                        current.A = alpha;
                    }

                    image[x, y] = current;
                }
            }
        }

        public static Rgba32 HexToColor(string hexString)
        {
            if (hexString.ToLower() == "transparent")
            {
                return Color.Transparent;
            }

            try
            {
                var drawingColor = System.Drawing.ColorTranslator.FromHtml("#" + hexString);
                return Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
            }
            catch (Exception)
            {
            }

            return Color.FromRgb(254, 254, 254);
        }

        public string GetFileName()
        {
            string name = (IsSmallFurni ? "s_" : "") + Sprite + "_" + RenderDirection + "_" + RenderState;

            if (this.ColourId > -1 && this.Assets.Count(x => x.ColourCode != null) > 0)
            {
                name += "_colour" + this.ColourId;
            }

            return name + ".png";
        }

        private void GenerateAnimations()
        {
            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToLower().ToCharArray();
            var xmlData = FileUtil.SolveXmlFile(XmlDirectory, "visualization");

            this.AnimationCount = 0;
            this.Animations = new SortedDictionary<int, ChromaAnimation>();

            if (xmlData == null)
            {
                return;
            }

            XmlNodeList frames = xmlData.SelectNodes("//visualizationData/visualization[@size='" + (IsSmallFurni ? "32" : "64") + "']/animations/animation/animationLayer/frameSequence/frame");
            
            if (frames == null)
            {
                frames = xmlData.SelectNodes("//visualizationData/visualization[@size='64']/animations/animation/animationLayer/frameSequence/frame");
            }

            this.HighestAnimationLayer = 0;

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames.Item(i);

                var animationLayer = frame.ParentNode.ParentNode;
                int letterPosition = int.Parse(animationLayer.Attributes.GetNamedItem("id").InnerText);

                if (letterPosition < 0 || letterPosition > alphabet.Length)
                {
                    continue;
                }

                var animationLetter = int.Parse(animationLayer.Attributes.GetNamedItem("id").InnerText);//Convert.ToString(alphabet[int.Parse(animationLayer.Attributes.GetNamedItem("id").InnerText)]);

                HighestAnimationLayer = int.Parse(animationLayer.Attributes.GetNamedItem("id").InnerText) + 1;

                var animation = frame.ParentNode.ParentNode.ParentNode;
                var animationId = int.Parse(animation.Attributes.GetNamedItem("id").InnerText);

                var castAnimationId = animationId + 1;

                if (castAnimationId > this.AnimationCount)
                {
                    this.AnimationCount = castAnimationId;
                }

                if (!this.Animations.ContainsKey(animationLetter))
                {
                    this.Animations.Add(animationLetter, new ChromaAnimation());//new Dictionary<int, List<string>>());
                }

                if (!this.Animations[animationLetter].States.ContainsKey(animationId))
                {
                    var frameClass = new ChromaFrame();
                    this.Animations[animationLetter].States.Add(animationId, frameClass);

                    if (animationLayer.Attributes.GetNamedItem("loopCount") != null)
                        frameClass.Loop = int.Parse(animationLayer.Attributes.GetNamedItem("loopCount").InnerText);

                    if (animationLayer.Attributes.GetNamedItem("frameRepeat") != null)
                        frameClass.FramesPerSecond = int.Parse(animationLayer.Attributes.GetNamedItem("frameRepeat").InnerText);
                }

                this.Animations[animationLetter].States[animationId].Frames.Add(frame.Attributes.GetNamedItem("id").InnerText);
            }
        }
    }
}
 