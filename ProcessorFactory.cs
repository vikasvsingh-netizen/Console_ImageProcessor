using ImageProcessor.Concrete;
using ImageProcessor.Contract;
using ImageProcessor.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessor
{
    public static class ProcessorFactory
    {
        private static IConfiguration _configuration;

        // Initialize the factory with a configuration instance
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public static IImageProcessor GetProcessor(AppConfig config)
        {
            return config.Library switch
            {
                LibrariesEnum.ImageSharp => new ImageSharpProcessor(),
                LibrariesEnum.ImageMagick => new ImageMagickProcessor(),
                LibrariesEnum.KrakenProcessor => new KrakenProcessor(
                    _configuration?["KrakenApi:ApiKey"] ?? config.KrakenApiKey,
                    _configuration?["KrakenApi:ApiSecret"] ?? config.KrakenApiSecret
                ),
                LibrariesEnum.KrakenSdkProcessor => new KrakenSdkProcessor(
                    _configuration?["KrakenApi:ApiKey"] ?? config.KrakenApiKey,
                    _configuration?["KrakenApi:ApiSecret"] ?? config.KrakenApiSecret
                ),
                //"SkiaSharp" => new SkiaSharpProcessor(), // future
                _ => throw new NotSupportedException($"Library not supported")
            };
        }
    }

}
