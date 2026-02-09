using ImageProcessor.Contract;
using ImageProcessor.Model;
using Kraken;
using Kraken.Http;
using Kraken.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ImageProcessor.Concrete
{
    internal class KrakenSdkProcessor : IImageProcessor
    {
        private readonly Client _client;
        private ImageSizeConfig _config;
        private static readonly HttpClient _http = new HttpClient();
        public KrakenSdkProcessor(string apikey , string apiSecret) {
            var connection = Connection.Create(apikey,apiSecret);
            _client = new Client(connection);
        } 
        public void SetSizeConfigs(ImageSizeConfig config)
        {
            _config = config;
        }
        public void ProcessImage(string inputFilePath, string outputRoot)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string libraryRoot = Path.Combine(outputRoot, "KrakenSdk");

            var request = new OptimizeSetUploadWaitRequest
            {
                Lossy = true,
                WebP = true,
                AutoOrient = true,
                SamplingScheme = SamplingScheme.Default
            };

            if(_config != null && _config.Thumbnails!=null && _config.Thumbnails.Count>0)
                AddSizes(request, "thumbnails", _config.Thumbnails);
            if (_config != null && _config.Grid != null && _config.Grid.Count > 0)
                AddSizes(request, "grid", _config.Grid);
            if (_config != null && _config.FullPage != null && _config.FullPage.Count > 0)
                AddSizes(request, "fullpage", _config.FullPage);

            var response = _client.OptimizeWait(inputFilePath, request).Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Kraken optimization failed");
            }

            foreach (var result in response.Body.Results)
            {
                SaveResult(result, libraryRoot, fileName);
            }
        }

        private void AddSizes(
            OptimizeSetUploadWaitRequest request,
            string type,
            List<ImageSize> sizes)
        {
            foreach (var size in sizes)
            {
                int? height = null;

                request.AddSet(new ResizeImageSet
                {
                    Name = $"{type}_{size.Width}",
                    Width = size.Width,
                    Lossy = true,
                    Strategy = Strategy.None,
                    SamplingScheme = SamplingScheme.Default
                });
            }
        }

        private void SaveResult(OptimizeSetWaitResult result,string libraryRoot,string fileName)
        {
            var parts = result.Name.Split('_');
            var type = parts[0];
            var width = parts[1];

            string outputDir = Path.Combine(libraryRoot, type, width);
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(
                outputDir,
                $"{fileName}_{type}_{width}.webp"
            );

            var bytes = _http.GetByteArrayAsync(result.KrakedUrl).Result;
            System.IO.File.WriteAllBytes(outputPath, bytes);
            Console.WriteLine($"Downloaded {new FileInfo(outputPath).Length / 1024} KB");
        }

    }
}
