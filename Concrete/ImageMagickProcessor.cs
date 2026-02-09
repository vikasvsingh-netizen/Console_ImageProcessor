using ImageMagick;
using ImageProcessor.Model;
using System.IO;
using ImageProcessor.Contract;

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

            // 1️⃣ Strip metadata (EXIF, GPS, etc.)
            original.Strip();

            //ProcessSet(original, fileName, libraryRoot, "thumbnails", _config.Thumbnails);
            //ProcessSet(original, fileName, libraryRoot, "grid", _config.Grid);
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

                // 2️⃣ Resize BEFORE compression
                if ((source.Width >= size.Width))
                {
                    clone.Resize(new MagickGeometry((uint)size.Width, 0)
                    {
                        IgnoreAspectRatio = false
                    });
                }

                // 3️⃣ Light sharpening after resize
                clone.UnsharpMask(0, 0.4, 0.6, 0.02);

                // 3.1️⃣ Optional: color adjustments
                clone.Modulate(new Percentage(102), new Percentage(95), new Percentage(100));

                // 4️⃣ WebP output settings
                clone.Format = MagickFormat.WebP;
                clone.Quality = (uint)size.Quality;

                clone.FilterType = FilterType.Lanczos;

                clone.Settings.SetDefine("webp:lossless", "false");
                clone.Settings.SetDefine("webp:alpha-quality", "90");

                // 5️⃣ Best WebP compression
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
