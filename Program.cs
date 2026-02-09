using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ImageProcessor.Model;
using ImageProcessor;
using ImageProcessor.Contract;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        LibrariesEnum [] libraries = new LibrariesEnum[] { LibrariesEnum.ImageMagick, LibrariesEnum.ImageSharp, LibrariesEnum.KrakenProcessor };
        
        // 1️⃣ Read config from JSON file
        var configJson = File.ReadAllText("config.json");
        var appConfig = JsonSerializer.Deserialize<AppConfig>(configJson);

        foreach (var lib in libraries)
        {
            appConfig.Library = lib;
            IImageProcessor processor = ProcessorFactory.GetProcessor(appConfig);
            processor.SetSizeConfigs(appConfig.SizeConfig);

            var stopwatch = Stopwatch.StartNew();
            // 2️⃣ Process all files in parallel
            var files = Directory.GetFiles(appConfig.InputFolder);

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
            {
                try
                {
                    processor.ProcessImage(file, appConfig.OutputFolder);
                    Console.WriteLine($"Processed: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {file}: {ex.Message}");
                }
            });
            stopwatch.Stop();
            Console.WriteLine("Time taken to complete all image processing :" + stopwatch.ElapsedMilliseconds + " MilliSeconds");
            Console.WriteLine("All images processed successfully for library : ! "+lib);
        }
    }
}