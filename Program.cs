using Elastic.Apm;
using Elastic.Apm.Config;
using ImageProcessor;
using ImageProcessor.Contract;
using ImageProcessor.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using  Serilog.Sinks.OpenTelemetry;
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
using System.Transactions;

class Program
{
    static void Main()
    {
        var serviceName = "ImageProcessingConsole";

        // Load configuration (User Secrets + appsettings.json)
        var configuration = new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json", optional: true)
                      .AddUserSecrets<Program>()
                      .Build();

        // Fetch base URL and token from configuration
        var baseUrl = configuration["OpenTelemetry:BaseUrl"];
        var token = configuration["OpenTelemetry:Token"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(token))
        {
            throw new Exception("OpenTelemetry BaseUrl or Token is not set in User Secrets.");
        }

        var logsEndpoint = $"{baseUrl}/v1/logs";
        var tracesEndpoint = $"{baseUrl}/v1/traces";

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole()
                   .AddOpenTelemetry(options =>
                   {
                       options.IncludeScopes = true;
                       options.ParseStateValues = true;
                       options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                           .AddService(serviceName));

                       options.AddOtlpExporter(otlpOptions =>
                       {
                           otlpOptions.Endpoint = new Uri(logsEndpoint);
                           otlpOptions.Headers = $"Authorization=Bearer {token}";
                           otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                       });
                   });
        });
        var logger = loggerFactory.CreateLogger<Program>();


        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = logsEndpoint;
                options.Protocol = OtlpProtocol.HttpProtobuf;
                options.Headers = new Dictionary<string, string>
                {
                     { "Authorization", $"Bearer {token}" }
                };
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    { "service.name", serviceName }
                };
            })
            .CreateLogger();




        var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService(serviceName: serviceName);


        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(
                resourceBuilder)
            .AddSource(serviceName)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(tracesEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Headers = $"Authorization=Bearer {token}";
            })
            .Build();

        var activitySource = new ActivitySource(serviceName);

        LibrariesEnum[] libraries = new LibrariesEnum[] { LibrariesEnum.ImageSharp , LibrariesEnum.ImageMagick};
        // {LibrariesEnum.ImageMagick, LibrariesEnum.ImageSharp };

        // 1️⃣ Read config from JSON file
        var configJson = File.ReadAllText("config.json");
        var appConfig = JsonSerializer.Deserialize<AppConfig>(configJson);

        foreach (var lib in libraries)
        {
            using var parentActivity = activitySource.StartActivity($"Process Library: {lib}", ActivityKind.Internal);
            //logger.LogInformation("Starting main operation for library..."+lib);
            Log.Information("Starting main operation for library... {LibraryName}", lib);

            parentActivity?.SetTag("library.name", lib.ToString());

            appConfig.Library = lib;

            ProcessorFactory.Initialize(configuration);
            IImageProcessor processor = ProcessorFactory.GetProcessor(appConfig);
            // logger.LogInformation("created processor from processor factory..." + processor);
            Log.Information("created processor from processor factory..." + processor);
            processor.SetSizeConfigs(appConfig.SizeConfig);

            // 2️⃣ Process all files in parallel
            var files = Directory.GetFiles(appConfig.InputFolder);
            parentActivity?.SetTag("files.count", files.Length);
            parentActivity?.SetTag("input.folder", appConfig.InputFolder);
            parentActivity?.SetTag("output.folder", appConfig.OutputFolder);

            // 3️⃣ Wrap image processing in a trace

            var stopwatch = Stopwatch.StartNew();
            //logger.LogInformation("started processing all files parallelly");
            Log.Information("started processing all files parallelly");
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = lib == LibrariesEnum.KrakenSdkProcessor ? 1 : Environment.ProcessorCount }, file =>
            {
                using var fileActivity = activitySource.StartActivity("Process Image",ActivityKind.Internal);
                fileActivity?.SetTag("file.name", Path.GetFileName(file));
                fileActivity?.SetTag("file.full_path", file);
                fileActivity?.SetTag("file.size_bytes", new FileInfo(file).Length);
                fileActivity?.SetTag("library.used", lib.ToString());
                //logger.LogInformation("Working on file..." + Path.GetFileName(file) + "with lib : " + lib);
                Log.Information("Working on file..." + Path.GetFileName(file) + "with lib : " + lib);

                try
                {
                    processor.ProcessImage(file, appConfig.OutputFolder);
                    Console.WriteLine($"Processed: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    fileActivity?.RecordException(ex);
                    fileActivity?.SetStatus(ActivityStatusCode.Error);
                    parentActivity?.RecordException(ex);
                    Console.WriteLine($"Error processing {file}: {ex.Message}");
                }
            });
            stopwatch.Stop();
            parentActivity?.SetTag("total.processing.time.ms", stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"All images processed successfully for library: {lib}");
            tracerProvider.ForceFlush();
        }
    }
}