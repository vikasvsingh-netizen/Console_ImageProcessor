using ImageProcessor.Contract;
using ImageProcessor.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace ImageProcessor.Concrete
{
    public class ImageSharpProcessor : IImageProcessor
    {
        private ImageSizeConfig _config;

        public void SetSizeConfigs(ImageSizeConfig config)
        {
            _config = config;
        }

        public void ProcessImage(string inputFilePath, string outputRoot)
        {
            using var image = Image.Load(inputFilePath);
            image.Metadata.ExifProfile = null; // remove metadata

            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string libraryFolder = Path.Combine(outputRoot, "ImageSharp");
            image.Mutate(x => x.Saturate(0.95f).Brightness(1.02f).Contrast(1.05f));
            //ProcessSet(image, fileName, libraryFolder, "thumbnails", _config.Thumbnails);
            //ProcessSet(image, fileName, libraryFolder, "grid", _config.Grid);
            ProcessSet(image, fileName, libraryFolder, "fullpage", _config.FullPage);
        }

        private void ProcessSet(Image source, string fileName, string libraryFolder, string type, List<ImageSize> sizes)
        {
            foreach (var size in sizes)
            {
                string outputFolder = Path.Combine(libraryFolder, type, size.Width.ToString());
                Directory.CreateDirectory(outputFolder);

                
                    using var clone = source.Clone(ctx =>
                    {
                        if (source.Width >= size.Width)
                        {
                            ctx.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Max,
                                Size = new Size(size.Width, 0),
                                Sampler = KnownResamplers.Lanczos3
                            });
                        }
                        ctx.GaussianSharpen(0.25f);
                    });

                var encoder = new WebpEncoder { Quality = size.Quality, Method = WebpEncodingMethod.BestQuality , NearLossless = false };
                string outputPath = Path.Combine(outputFolder, $"{fileName}_{type}_{size.Width}.webp");
                clone.Metadata.ExifProfile = null;
                clone.Metadata.IccProfile = null;
                clone.Save(outputPath, encoder);
            }
        }
    }

}
