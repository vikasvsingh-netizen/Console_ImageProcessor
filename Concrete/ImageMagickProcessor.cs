using ImageMagick;
using ImageProcessor.Contract;
using ImageProcessor.Model;
using Kraken.Model;
using System.IO;

namespace ImageProcessor.Concrete
{


    public class ImageMagickProcessor : IImageProcessor
    {
        private ImageSizeConfig _config;

        public void SetSizeConfigs(ImageSizeConfig config)
        {
            _config = config;
        }

        public void ProcessImage(string inputFilePath, string outputRoot)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string libraryRoot = Path.Combine(outputRoot, "ImageMagick");

            using var original = new MagickImage(inputFilePath);

            original.AutoOrient();
            original.Strip();

            if (_config != null && _config.Thumbnails != null && _config.Thumbnails.Count > 0)
                ProcessSet(original, fileName, libraryRoot, "thumbnails", _config.Thumbnails);
            if (_config != null && _config.Grid != null && _config.Grid.Count > 0)
                ProcessSet(original, fileName, libraryRoot, "grid", _config.Grid);
            if (_config != null && _config.FullPage != null && _config.FullPage.Count > 0)
                ProcessSet(original, fileName, libraryRoot, "fullpage", _config.FullPage);
        }

        private void ProcessSet(
            MagickImage source,
            string fileName,
            string libraryRoot,
            string type,
            System.Collections.Generic.List<ImageSize> sizes)
        {
            foreach (var size in sizes)
            {
                string outputDir = Path.Combine(libraryRoot, type, size.Width.ToString());
                Directory.CreateDirectory(outputDir);

                using var clone = source.Clone();

                if ((source.Width >= size.Width * 1.05))
                {
                    clone.Resize(new MagickGeometry((uint)size.Width, 0)
                    {
                        IgnoreAspectRatio = false
                    });
                    clone.FilterType = size.Width <= 400 ? FilterType.Mitchell : FilterType.Lanczos;
                }

                double sharpenAmount =
                    size.Width <= 400 ? 0.35 :
                    size.Width <= 800 ? 0.25 :
                    size.Width <= 1600 ? 0.15 : 0.10;

                clone.UnsharpMask(0, 0.4, sharpenAmount, 0.02); 
                clone.Modulate(new Percentage(102), new Percentage(95), new Percentage(100));
                clone.Contrast();

                clone.Format = MagickFormat.WebP;
                clone.Quality = (uint)size.Quality;

                if (size.Quality >= 85)
                { clone.Settings.SetDefine("webp:lossless", "true"); }
                else clone.Settings.SetDefine("webp:lossless", "false");

                clone.Settings.SetDefine("webp:alpha-quality", "90");
                clone.Settings.SetDefine("webp:method", "6");
                clone.Strip();

                string outputPath = Path.Combine(
                    outputDir,
                    $"{fileName}_{type}_{size.Width}.webp"
                );

                clone.Write(outputPath);
            }
        }
    }

}
