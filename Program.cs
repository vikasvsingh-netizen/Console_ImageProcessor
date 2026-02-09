using ImageProcessor;
using ImageProcessor.Contract;
using ImageProcessor.Model;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System;
using System.Diagnostics;
using System.IO;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        LibrariesEnum[] libraries = new LibrariesEnum[] { LibrariesEnum.KrakenSdkProcessor };
        // {LibrariesEnum.ImageMagick, LibrariesEnum.ImageSharp };

        // 1️⃣ Read config from JSON file
        var configJson = File.ReadAllText("config.json");
        var appConfig = JsonSerializer.Deserialize<AppConfig>(configJson);

        foreach (var lib in libraries)
        {
            appConfig.Library = lib;
            var configuration = new ConfigurationBuilder()
                      .AddJsonFile("appsettings.json", optional: true)
                      .AddUserSecrets<Program>() 
                      .Build();

            ProcessorFactory.Initialize(configuration);
            IImageProcessor processor = ProcessorFactory.GetProcessor(appConfig);
            processor.SetSizeConfigs(appConfig.SizeConfig);

            var stopwatch = Stopwatch.StartNew();
            // 2️⃣ Process all files in parallel
            var files = Directory.GetFiles(appConfig.InputFolder);

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = lib == LibrariesEnum.KrakenSdkProcessor ? 1 : Environment.ProcessorCount }, file =>
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