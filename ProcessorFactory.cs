using ImageProcessor.Concrete;
using ImageProcessor.Contract;
using ImageProcessor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor
{
    public static class ProcessorFactory
    {
        public static IImageProcessor GetProcessor(AppConfig config)
        {
            return config.Library switch
            {
                LibrariesEnum.ImageSharp => new ImageSharpProcessor(),
                LibrariesEnum.ImageMagick => new ImageMagickProcessor(),
                LibrariesEnum.KrakenProcessor => new KrakenProcessor(config.KrakenApiKey,config.KrakenApiSecret),
                //"SkiaSharp" => new SkiaSharpProcessor(), // future
                _ => throw new NotSupportedException($"Library not supported")
            };
        }
    }

}
