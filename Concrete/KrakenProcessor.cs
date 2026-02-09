using ImageProcessor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ImageProcessor.Contract;

namespace ImageProcessor.Concrete
{
    public class KrakenProcessor : IImageProcessor
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private ImageSizeConfig _config;
        private static readonly HttpClient _http = new HttpClient();

        public KrakenProcessor(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }

        public void SetSizeConfigs(ImageSizeConfig config)
        {
            _config = config;
        }

        public void ProcessImage(string inputFilePath, string outputRoot)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string libraryRoot = Path.Combine(outputRoot, "Kraken");


            if (_config != null && _config.Thumbnails != null && _config.Thumbnails.Count > 0)
                ProcessSet(inputFilePath, fileName, libraryRoot, "thumbnails", _config.Thumbnails);
            if (_config != null && _config.Grid != null && _config.Grid.Count > 0)
                ProcessSet(inputFilePath, fileName, libraryRoot, "grid", _config.Grid);
            if (_config != null && _config.FullPage != null && _config.FullPage.Count > 0)
                ProcessSet(inputFilePath, fileName, libraryRoot, "fullpage", _config.FullPage);
        }

        private void ProcessSet(
            string inputFilePath,
            string fileName,
            string libraryRoot,
            string type,
            System.Collections.Generic.List<ImageSize> sizes)
        {
            foreach (var size in sizes)
            {
                string outputDir = Path.Combine(libraryRoot, type, size.Width.ToString());
                Directory.CreateDirectory(outputDir);

                string outputPath = Path.Combine(
                    outputDir,
                    $"{fileName}_{type}_{size.Width}.webp"
                );

                OptimizeWithKraken(inputFilePath, outputPath, size.Width, size.Quality);
            }
        }

        private void OptimizeWithKraken(string inputPath, string outputPath, int width, int quality)
        {
            byte[] imageBytes = File.ReadAllBytes(inputPath);

            var payload = new
            {
                auth = new { api_key = _apiKey, api_secret = _apiSecret },
                wait = true,
                lossy = true,
                webp = true,
                resize = new
                {
                    width = width,
                    strategy = "none"
                },
                quality = quality
            };


            var request = new MultipartFormDataContent();
            request.Add(new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), "data");
            request.Add(new ByteArrayContent(imageBytes), "file", Path.GetFileName(inputPath));

            var response = _http.PostAsync("https://api.kraken.io/v1/upload", request).Result;
            var responseJson = response.Content.ReadAsStringAsync().Result;

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (!root.GetProperty("success").GetBoolean())
            {
                throw new Exception("Kraken optimization failed");
            }

            string optimizedUrl = root.GetProperty("kraked_url").GetString();

            byte[] optimizedBytes = _http.GetByteArrayAsync(optimizedUrl).Result;
            File.WriteAllBytes(outputPath, optimizedBytes);
        }
    }
}





