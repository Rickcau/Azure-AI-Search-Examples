using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using OpenAI.Embeddings;
using api_azure_ai_search.Helper;
using api_azure_ai_search.Models;
using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;

namespace api_azure_ai_search.Helper
{
    public class GolfBallHelper
    {
        private readonly ILogger<GolfBallHelper> _logger;
        private static IConfiguration _configuration;
        private readonly string _aoaiEndpoint = String.Empty;
        private readonly string _aoaiApiKey = String.Empty;
        private readonly string _searchAdminKey = String.Empty;
        private readonly string _searchServiceEndpoint = String.Empty;
        private readonly string _aoaiEmbeddingModel = String.Empty;
        private readonly string _aoaiEmbeddingDeplopyment = String.Empty;
        private readonly string _aoaiEmbeddingDimensions = String.Empty;
        private readonly string _testImagesFolder = String.Empty;
        private readonly string _csvFolder = String.Empty;
        private readonly string _csvFilePath = String.Empty;
        private readonly string _csvFileName = String.Empty;
        private readonly string _azureVisionEndpoint = String.Empty;
        private readonly string _azureVisionKey = String.Empty;

        public string TestImagesFolder {  get { return _testImagesFolder; } }

        //string endpoint = ConfigHelper.GetEnvironmentVariable("AZURE_VISION_ENDPOINT")!.TrimEnd('/');
        //string apiKey = ConfigHelper.GetEnvironmentVariable("AZURE_VISION_KEY");

        public GolfBallHelper(IConfiguration configuration, ILogger<GolfBallHelper>? logger = null)
        {
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<GolfBallHelper>();

            _aoaiEndpoint = configuration["AZURE_OPENAI_ENDPOINT"] ??
                throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT configuration value is missing or empty");

            _aoaiApiKey = configuration["AZURE_OPENAI_API_KEY"] ??
                throw new InvalidOperationException("AZURE_OPENAI_API_KEY configuration value is missing or empty");

            _searchAdminKey = configuration["AZURE_SEARCH_ADMIN_KEY"] ??
                throw new InvalidOperationException("AZURE_SEARCH_ADMIN_KEY configuration value is missing or empty");

            _searchServiceEndpoint = configuration["AZURE_SEARCH_SERVICE_ENDPOINT"] ??
                throw new InvalidOperationException("AZURE_SEARCH_SERVICE_ENDPOINT configuration value is missing or empty");

            _aoaiEmbeddingModel = configuration["AZURE_OPENAI_EMBEDDING_MODEL"] ??
                throw new InvalidOperationException("AZURE_OPENAI_EMBEDDING_MODEL configuration value is missing or empty");

            _aoaiEmbeddingDeplopyment = configuration["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"] ??
                throw new InvalidOperationException("AZURE_OPENAI_EMBEDDING_DEPLOYMENT configuration value is missing or empty");

            _aoaiEmbeddingDimensions = configuration["AZURE_OPENAI_EMBEDDING_DIMENSIONS"] ??
                throw new InvalidOperationException("AZURE_OPENAI_EMBEDDING_DIMENSIONS configuration value is missing or empty");
            
            _azureVisionEndpoint = configuration["AZURE_VISION_ENDPOINT"]!.TrimEnd('/') ??
                throw new InvalidOperationException("AZURE_VISION_ENDPOINT configuration value is missing or empty");

            _azureVisionKey = configuration["AZURE_VISION_KEY"] ??
                throw new InvalidOperationException("AZURE_VISION_KEY configuration value is missing or empty");

            // Get the current directory where the application is running
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _testImagesFolder = Path.Combine(currentDirectory, "Test-Images");
            _csvFolder = Path.Combine(currentDirectory, "Data");
            _csvFilePath = Path.Combine(_csvFolder, configuration["CSV_FILE_NAME"]!);
        }


        // RDC - this is the old version of the code that does not use the image vector field.    
        public async Task UploadGolfBallDataTextOnlyAsync(AzureOpenAIClient azureOpenAIClient,
            SearchClient searchClient)
        {
            _logger.LogInformation("UploadGolfBall Data Text Only, CSV File Path: {CsvFilePath}", _csvFileName);
            // The next line of code reads the data from the CSV into a list
            var golfBalls = await LoadGolfBallsFromCsvAsyncV1(_csvFilePath);

            if (golfBalls == null || !golfBalls.Any())
            {
                throw new ArgumentException("No golf ball data found in CSV.");
            }

            var embeddingClient = azureOpenAIClient.GetEmbeddingClient(_aoaiEmbeddingDeplopyment);

            // Iterate over the list of golfballs, creating the embeddings
            foreach (var golfBall in golfBalls)
            {
                string textForEmbedding = $"Manufacturer: {golfBall.Manufacturer}, " +
                                        $"Pole Marking: {golfBall.Pole_Marking}, " +
                                        $"Color: {golfBall.Colour}, " +
                                        $"Seam Marking: {golfBall.Seam_Marking}";

                OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(textForEmbedding);
                golfBall.VectorContent = embedding.ToFloats().ToArray().ToList();
            }

            // Now, upload the data to our index!
            var batch = IndexDocumentsBatch.Upload(golfBalls);
            var result = await searchClient.IndexDocumentsAsync(batch);
            _logger.LogInformation("Upload GolfBall Data for Text only index completed!");
        }

        // RDC Load the GolfBall Data from CSV for a Text based embeddings index
        // this approach does not have image embeddings
        // this can be refactored to populate the index with data from SQL 
        private async Task<List<GolfBallDataV1>> LoadGolfBallsFromCsvAsyncV1(string csvFilePath)
        {
            var golfBalls = new List<GolfBallDataV1>();
            var lines = await File.ReadAllLinesAsync(csvFilePath);
            var headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var golfBall = new GolfBallDataV1
                {
                    Manufacturer = values[1],
                    USGA_Lot_Num = values[2],
                    Pole_Marking = values[3],
                    Colour = values[4],
                    ConstCode = values[5],
                    BallSpecs = values[6],
                    Dimples = int.Parse(values[7]),
                    Spin = values[8],
                    Pole_2 = values[9],
                    Seam_Marking = values[10],
                    ImageUrl = values[11]
                };
                golfBalls.Add(golfBall);
            }

            return golfBalls;
        }

        // RDC Load the GolfBall Data from CSV for a Text and Image based embeddings index
        // this can be refactored to populate the index with data from SQL 
        public async Task UploadGolfBallDataTextImageAsyncV2(AzureOpenAIClient azureOpenAIClient,
        SearchClient searchClient)
        {
            _logger.LogInformation("UploadGolfBall Data Text and Image, CSV File Path: {CsvFilePath}", _csvFileName);
            var processedRows = new List<GolfBallDataV2>();
            var failedRows = new List<(GolfBallDataV2 golfBall, string error)>();

            try
            {
                var golfBalls = await LoadGolfBallsFromCsvAsyncV2(_csvFilePath);
                if (golfBalls == null || !golfBalls.Any())
                {
                    throw new ArgumentException("No golf ball data found in CSV.");
                }

                // Create the text embedding client from your AzureOpenAIClient.
                var embeddingClient = azureOpenAIClient.GetEmbeddingClient(_aoaiEmbeddingDeplopyment!);

                foreach (var golfBall in golfBalls)
                {
                    try
                    {
                        // Build the text string used for text embeddings.
                        string textForEmbedding = $"Manufacturer: {golfBall.Manufacturer}, " +
                                                  $"Pole Marking: {golfBall.Pole_Marking}, " +
                                                  $"Colour: {golfBall.Colour}, " +
                                                  $"Seam Marking: {golfBall.Seam_Marking}";

                        // Generate text embedding (e.g., 1536 dimensions)
                        OpenAIEmbedding textEmbedding = await embeddingClient.GenerateEmbeddingAsync(textForEmbedding);
                        golfBall.TextVector = textEmbedding.ToFloats().ToArray().ToList();

                        // Generate an image embedding (e.g., 1024 dimensions) if available.
                        if (!string.IsNullOrEmpty(golfBall.ImageUrl))
                        {
                            float[] imageEmbedding = await GenerateImageEmbeddingAsyncV2(golfBall.ImageUrl);
                            golfBall.ImageVector = imageEmbedding.ToList();
                        }
                        else
                        {
                            golfBall.ImageVector = new List<float>();
                        }
                        processedRows.Add(golfBall);
                    }
                    catch (Exception ex)
                    {
                        failedRows.Add((golfBall, ex.Message));
                        Console.WriteLine($"Error processing golf ball (Manufacturer: {golfBall.Manufacturer}): {ex.Message}");
                    }
                }

                if (processedRows.Any())
                {
                    var batch = IndexDocumentsBatch.Upload(processedRows);
                    var result = await searchClient.IndexDocumentsAsync(batch);
                    Console.WriteLine($"Successfully indexed {processedRows.Count} golf ball records.");
                }

                if (failedRows.Any())
                {
                    string logFilePath = Path.Combine(Path.GetDirectoryName(_csvFileName)!, "failed_rows.log");
                    await WriteFailedRowsToLogAsync(failedRows, logFilePath);
                    Console.WriteLine($"Failed to process {failedRows.Count} golf balls. See {logFilePath} for details.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error during processing: {ex.Message}");
                throw;
            }
        }

        private async Task WriteFailedRowsToLogAsync(List<(GolfBallDataV2 golfBall, string error)> failedRows, string logFilePath)
        {
            var logEntries = failedRows.Select(fr =>
                $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n" +
                $"Golf Ball Details:\n" +
                $"  Manufacturer: {fr.golfBall.Manufacturer}\n" +
                $"  Pole Marking: {fr.golfBall.Pole_Marking}\n" +
                $"  Color: {fr.golfBall.Colour}\n" +
                $"  Seam Marking: {fr.golfBall.Seam_Marking}\n" +
                $"Error: {fr.error}\n" +
                $"----------------------------------------\n");

            await File.WriteAllLinesAsync(logFilePath, logEntries);
        }

        /// <summary>
        /// Load GolfBall data from CSV which uses both text and image data
        /// </summary>
        /// <param name="csvFilePath"></param>
        /// <returns></returns>
        private async Task<List<GolfBallDataV2>> LoadGolfBallsFromCsvAsyncV2(string csvFilePath)
        {
            var golfBalls = new List<GolfBallDataV2>();
            var lines = await File.ReadAllLinesAsync(csvFilePath);
            var headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var golfBall = new GolfBallDataV2
                {
                    Manufacturer = values[1],
                    USGA_Lot_Num = values[2],
                    Pole_Marking = values[3],
                    Colour = values[4],
                    ConstCode = values[5],
                    BallSpecs = values[6],
                    Dimples = int.Parse(values[7]),
                    Spin = values[8],
                    Pole_2 = values[9],
                    Seam_Marking = values[10],
                    ImageUrl = values[11]
                };
                golfBalls.Add(golfBall);
            }

            return golfBalls;
        }

        // RDC this version downloads the image into a byte array then we send that to the AI Vision to create embeddings
        public async Task<float[]> GenerateImageEmbeddingAsyncV2(string imageUrl)
        {
            try
            {
                //string endpoint = ConfigHelper.GetEnvironmentVariable("AZURE_VISION_ENDPOINT")!.TrimEnd('/');
                //string apiKey = ConfigHelper.GetEnvironmentVariable("AZURE_VISION_KEY");

                // Step 2: Construct the full API URL.
                string apiVersion = "2024-02-01";
                string modelVersion = "2023-04-15";
                string requestUri = $"{_azureVisionEndpoint!}/computervision/retrieval:vectorizeImage?api-version={apiVersion}&model-version={modelVersion}";

                // Step 3: Download the image binary data.
                byte[] imageBytes;
                //using (var imageClient = new HttpClient())
                //{
                //    imageBytes = await imageClient.GetByteArrayAsync(imageUrl);
                //}

                imageBytes = await DownloadImageAsync(imageUrl);

                // Step 4: Prepare the HTTP client for the Azure Vision API call.
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _azureVisionKey.Trim());

                // Step 5: Create a ByteArrayContent using the downloaded binary data.
                using var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");


                // For debugging - print the request details
                _logger.LogInformation($"Request URI: {requestUri}");
                _logger.LogInformation($"Image bytes length: {imageBytes.Length}");
                _logger.LogInformation($"API Key (first 4 chars): {_azureVisionKey.Substring(0, 4)}..."); // Only show first 4 chars for security

                // Step 6: Send the request
                var response = await httpClient.PostAsync(requestUri, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                    _logger.LogInformation($"Response Content: {responseContent}");
                    throw new Exception($"API call failed (status: {response.StatusCode}): {responseContent}");
                }

                // Step 7: Parse the response
                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<VectorizeImageResponse>(responseContent, deserializeOptions);

                if (result?.Vector == null || result.Vector.Length == 0)
                {
                    throw new Exception("No vector embedding generated.");
                }

                return result.Vector;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error generating image embedding: {ex.Message}");
                throw;
            }
        }

        public async Task<float[]> GenerateImageEmbeddingAsyncV3(byte[] imageBytes)
        {
            try
            {
                // Step 1: Retrieve endpoint and key from configuration.

                // Step 2: Construct the full API URL.
                string apiVersion = "2024-02-01";
                string modelVersion = "2023-04-15";
                string requestUri = $"{_azureVisionEndpoint!}/computervision/retrieval:vectorizeImage?api-version={apiVersion}&model-version={modelVersion}";

                // Step 3: Prepare the HTTP client for the Azure Vision API call.
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _azureVisionKey.Trim());

                // Step 4: Create a ByteArrayContent using the provided binary data.
                using var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // For debugging - print the request details
                _logger.LogInformation($"Request URI: {requestUri}");
                _logger.LogInformation($"Image bytes length: {imageBytes.Length}");
                _logger.LogInformation($"API Key (first 4 chars): {_azureVisionKey.Substring(0, 4)}...");

                // Step 5: Send the request
                var response = await httpClient.PostAsync(requestUri, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                    _logger.LogInformation($"Response Content: {responseContent}");
                    throw new Exception($"API call failed (status: {response.StatusCode}): {responseContent}");
                }

                // Step 6: Parse the response
                var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<VectorizeImageResponse>(responseContent, deserializeOptions);

                if (result?.Vector == null || result.Vector.Length == 0)
                {
                    throw new Exception("No vector embedding generated.");
                }

                return result.Vector;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error generating image embedding: {ex.Message}");
                throw;
            }
        }

        private async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            try
            {
                using var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.All
                };

                using var client = new HttpClient(handler);
                // Add common browser headers to avoid being blocked
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

                // Set a reasonable timeout
                client.Timeout = TimeSpan.FromSeconds(30);

                using var response = await client.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to download image. Status code: {response.StatusCode}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error downloading image: {ex.Message}");
                throw new Exception($"Failed to download image using both methods. Original error: {ex.Message}");

                //// Try alternative approach using WebClient if HttpClient fails
                //try
                //{
                //    using var webClient = new System.Net.WebClient();
                //    webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                //    return await webClient.DownloadDataTaskAsync(imageUrl);
                //}
                //catch (Exception webEx)
                //{
                //    throw new Exception($"Failed to download image using both methods. Original error: {ex.Message}, WebClient error: {webEx.Message}");
                //}
            }
        }

        public async Task ProcessImagesAndSearch(SearchClient searchClient)
        {
            try
            {
                // Get all JPG files from the test images folder
                var imageFiles = Directory.GetFiles(_testImagesFolder, "*.jpg", SearchOption.TopDirectoryOnly);

                foreach (var imagePath in imageFiles)
                {
                    try
                    {
                        Console.WriteLine($"\nProcessing image: {Path.GetFileName(imagePath)}");

                        // Load the image file
                        byte[] imageBytes = File.ReadAllBytes(imagePath);

                        // Generate embedding for the image using Azure Vision API
                        float[] imageEmbedding = await GolfBallSearchHelper.GenerateImageEmbeddingAsyncV3(configuration, imageBytes);

                        // Search using the embedding
                        //var searchResults = await searchHelper.SearchV2(
                        //    configuration,
                        //    searchClient,
                        //    imageEmbedding,
                        //    k: 3,
                        //    top: 10
                        //);

                        // RDC testing the V3 code with scores
                        var searchResults = await searchHelper.SearchV4(
                           configuration,
                           searchClient,
                           imageEmbedding,
                           k: 3,
                           top: 10
                       );

                        // Display results
                        Console.WriteLine($"Top matches for {Path.GetFileName(imagePath)}:");
                        foreach (var result in searchResults)
                        {
                            Console.WriteLine($"Match: {result.Manufacturer} - {result.USGA_Lot_Num} Seam_Marking: {result.Seam_Marking})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing image {imagePath}: {ex.Message}");
                        // Continue with next image
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessImagesAndSearch: {ex.Message}");
                throw;
            }
        }

    }
}
