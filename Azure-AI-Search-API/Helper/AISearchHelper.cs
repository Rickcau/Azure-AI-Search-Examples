using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure;
using Microsoft.Extensions.Configuration;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents;
using System.Text.Json;
using OpenAI.Embeddings;
using Azure.Search.Documents.Models;
using Azure.Core;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using System;
using System.Linq;
//
using Azure_AI_Search_API.Models;
using project.Helper;
//
using System.Reflection.Metadata;

namespace Helper.AzureOpenAISearchHelper
{
    /// <summary>
    /// AzureOpenAISearchHelper : This class is a helper that allows the creation of a single modality index(text) using 
    /// embeddings for hybrid or semantic searches or a multi-modality index (image and text) allowing for image similarity searches.
    /// RDC: 2/23/2025 - Working well.
    /// </summary>
    public class AISearchHelper
    {
        private readonly ILogger<AISearchHelper> _logger;
        private static IConfiguration? _configuration;
        private readonly string _aoaiEndpoint = String.Empty;
        private readonly string _aoaiApiKey = String.Empty;
        private readonly string _searchAdminKey = String.Empty;
        private readonly string _searchServiceEndpoint = String.Empty;
        private readonly string _aoaiEmbeddingModel = String.Empty;
        private readonly string _aoaiEmbeddingDeplopyment = String.Empty;
        private readonly string _aoaiEmbeddingDimensions = String.Empty;
        public GolfBallHelper _ballHelper;

        public AISearchHelper(IConfiguration configuration, ILogger<AISearchHelper>? logger = null)
        {
            _logger = logger ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AISearchHelper>();
            _configuration = configuration;

            _aoaiEndpoint = _configuration["AZURE_OPENAI_ENDPOINT"] ??
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

            _ballHelper = new GolfBallHelper(_configuration);

        }

        public AzureOpenAIClient InitializeOpenAIClient(TokenCredential credential)
        {
            if (!string.IsNullOrEmpty(_aoaiApiKey))
            {
                return new AzureOpenAIClient(new Uri(_aoaiEndpoint!), new AzureKeyCredential(_aoaiApiKey));
            }
            _logger.LogInformation($"Initialze OpenAI Client for the Index");

            return new AzureOpenAIClient(new Uri(_aoaiEndpoint!), credential);
        }

        public SearchIndexClient InitializeSearchIndexClient(TokenCredential credential)
        {
            if (!string.IsNullOrEmpty(_searchAdminKey))
            {
                return new SearchIndexClient(new Uri(_searchServiceEndpoint!), new AzureKeyCredential(_searchAdminKey));
            }

            return new SearchIndexClient(new Uri(_searchServiceEndpoint!), credential);
        }

        public async Task DeleteIndexAsync(string indexName, SearchIndexClient indexClient)
        {
            try
            {
                _logger.LogInformation("Attempting to delete index: {indexName}", indexName);
                await indexClient.DeleteIndexAsync(indexName);
                _logger.LogInformation("Successfully deleted index: {indexName}", indexName);
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    _logger.LogInformation("Index {IndexName} does not exist.", indexName);
                }
                else
                {
                    _logger.LogError(ex, "Error deleting index: {Message}", ex.Message);
                    throw;
                }
            }
        }

        public async Task<IReadOnlyList<string>> GetIndexesAsync(SearchIndexClient indexClient)
        {
            try
            {
                _logger.LogInformation("Retrieving list of indexes");
                // In order to use .ToListAsync() you must have the System.Linq.Async package installed
                var indexes = await indexClient.GetIndexNamesAsync().ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} indexes", indexes.Count);
                return indexes;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error retrieving indexes: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<SearchIndexDetails> GetIndexDetailsAsync(string indexName, SearchIndexClient indexClient)
        {
            try
            {
                _logger.LogInformation("Retrieving details for index: {IndexName}", indexName);
                SearchIndex index = await indexClient.GetIndexAsync(indexName);

                var details = new SearchIndexDetails
                {
                    Name = index.Name,
                    Fields = index.Fields.Select(f => new FieldInfo
                    {
                        Name = f.Name,
                        Type = f.Type.ToString(),
                        IsSearchable = f.IsSearchable ?? false,
                        IsFilterable = f.IsFilterable ?? false,
                        IsSortable = f.IsSortable ?? false,
                        IsFacetable = f.IsFacetable ?? false,
                        IsKey = f.IsKey ?? false
                    }).ToList(),
                    HasVectorSearch = index.VectorSearch != null,
                    HasSemanticSearch = index.SemanticSearch != null,
                    Vectorizers = index.VectorSearch?.Vectorizers?.Select(v => v.GetType().Name)?.ToList() ?? new List<string>(),
                    SemanticConfigurations = index.SemanticSearch?.Configurations?.Select(c => c.Name)?.ToList() ?? new List<string>()
                };

                _logger.LogInformation("Successfully retrieved index details for {IndexName}", indexName);
                return details;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error retrieving index details: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IndexStatistics> GetIndexStatisticsAsync(string indexName, SearchIndexClient indexClient)
        {
            try
            {
                _logger.LogInformation("Retrieving statistics for index: {IndexName}", indexName);
                var stats = await indexClient.GetIndexStatisticsAsync(indexName);

                return new IndexStatistics
                {
                    DocumentCount = stats.Value.DocumentCount,
                    VectorIndexSize = stats.Value.VectorIndexSize,
                    StorageSizeInBytes = stats.Value.StorageSize
                };
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error retrieving index statistics: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<List<SearchDocument>> ListDocumentsAsync(
                string indexName,
                SearchClient searchClient,
                SearchIndexClient indexClient,
                bool suppressVectorFields = true,
                int maxResults = 1000)
        {
            try
            {
                _logger.LogInformation("Retrieving documents from index: {IndexName}", indexName);

                var searchOptions = new SearchOptions
                {
                    Size = maxResults,
                    IncludeTotalCount = true
                };

                // If suppressVectorFields is true, get the index schema and exclude vector fields
                if (suppressVectorFields)
                {
                    SearchIndex index = await indexClient.GetIndexAsync(indexName);
                    var nonVectorFields = index.Fields
                        .Where(f => f.Type != SearchFieldDataType.Collection(SearchFieldDataType.Single))
                        .Select(f => f.Name);

                    // Add each field individually to the Select list
                    foreach (var field in nonVectorFields)
                    {
                        searchOptions.Select.Add(field);
                    }
                }

                var response = await searchClient.SearchAsync<SearchDocument>("*", searchOptions);
                var documents = new List<SearchDocument>();

                foreach (var result in response.Value.GetResults())
                {
                    documents.Add(result.Document);
                }

                _logger.LogInformation("Retrieved {Count} documents from index {IndexName}", documents.Count, indexName);
                return documents;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Error retrieving documents: {Message}", ex.Message);
                throw;
            }
        }


        // Creates the Index for use with the data found in the CSV 
        public async Task SetupIndexTextAsync(string indexName, SearchIndexClient indexClient)
        {
            const string vectorSearchHnswProfile = "golf-vector-profile";
            const string vectorSearchHnswConfig = "golfHnsw";
            const string vectorSearchVectorizer = "golfOpenAIVectorizer";
            const string semanticSearchConfig = "golf-semantic-config";

            SearchIndex searchIndex = new(indexName)
            {
                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                        {
                            VectorizerName = vectorSearchVectorizer
                        }
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration(vectorSearchHnswConfig)
                        {
                            Parameters = new HnswParameters
                            {
                                M = 4,
                                EfConstruction = 400,
                                EfSearch = 500,
                                Metric = "cosine"
                            }
                        }
                    },
                    Vectorizers =
                    {
                        new AzureOpenAIVectorizer(vectorSearchVectorizer)
                        {
                            Parameters = new AzureOpenAIVectorizerParameters
                            {
                                ResourceUri = new Uri(_aoaiEndpoint!),
                                ModelName = _aoaiEmbeddingModel,
                                DeploymentName = _aoaiEmbeddingDeplopyment,
                                ApiKey = _aoaiApiKey
                            }
                        }
                    }
                },
                SemanticSearch = new()
                {
                    Configurations =
                    {
                        new SemanticConfiguration(semanticSearchConfig, new()
                        {
                            TitleField = new SemanticField("manufacturer"),
                            ContentFields =
                            {
                                new SemanticField("pole_marking"),
                                new SemanticField("seam_marking")
                            }
                        })
                    }
                },
                Fields =
                    {
                        new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                        new SearchableField("manufacturer") { IsFilterable = true, IsSortable = true },
                        new SearchableField("usga_lot_num") { IsFilterable = true },
                        new SearchableField("pole_marking") { IsFilterable = true },
                        new SearchableField("colour") { IsFilterable = true },
                        new SearchableField("constCode") { IsFilterable = true },
                        new SearchableField("ballSpecs") { IsFilterable = true },
                        new SimpleField("dimples", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                        new SearchableField("spin") { IsFilterable = true },
                        new SearchableField("pole_2") { IsFilterable = true },
                        new SearchableField("seam_marking") { IsFilterable = true },
                        new SimpleField("imageUrl", SearchFieldDataType.String) { IsFilterable = false },
                        new SearchField("vectorContent", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                        {
                            IsSearchable = true,
                            VectorSearchDimensions = int.Parse(_aoaiEmbeddingDimensions!),
                            VectorSearchProfileName = vectorSearchHnswProfile
                        }
                    }
            };

            await indexClient.CreateOrUpdateIndexAsync(searchIndex);
        }

        // RDC - this is the old version of the code that does not use the image vector field.    
        public async Task UploadGolfBallDataTextOnlyAsync(AzureOpenAIClient azureOpenAIClient,
            SearchClient searchClient)
        {
            // Generates embeddings and adds the document by ingesting the data in the CSV file.
            await _ballHelper.UploadGolfBallDataTextOnlyAsync(azureOpenAIClient, searchClient);
        }

        // RDC - this is the old version of the code that does not use the image vector field.    
        public async Task UploadGolfBallDataTextImageAsync(AzureOpenAIClient azureOpenAIClient,
            SearchClient searchClient)
        {
            // Generates embeddings and adds the document by ingesting the data in the CSV file.
            await _ballHelper.UploadGolfBallDataTextImageAsyncV2(azureOpenAIClient, searchClient);
        }

        // RDC Sets up a multi-modal index with text vector field as well as an image vector field
        // The idea here is that you can perform semantic simalarity searches using a provided image
        // Or, you can perform hybrid, semantic and full-text searches using the text fields
        // in this case, we are using public golf ball data as our data set as an example
        public async Task SetupIndexTextImageAsync(string indexName, SearchIndexClient indexClient)
        {
            const string vectorSearchHnswConfig = "golfHnsw";
            const string textVectorProfile = "text-vector-profile";
            const string textVectorizer = "golfOpenAITextVectorizer";
            const string semanticSearchConfig = "golf-semantic-config";

            SearchIndex searchIndex = new(indexName)
            {
                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(textVectorProfile, vectorSearchHnswConfig)
                        {
                            VectorizerName = textVectorizer
                        },
                        // For the imageVector field, you can define a profile even if you generate vectors externally.
                        // You might provide a unique profile name for clarity.
                        new VectorSearchProfile("image-vector-profile", vectorSearchHnswConfig)
                        {
                            // Leave the VectorizerName blank or set to a custom value if you plan on using a custom transformer.
                            // In this case, you're uploading vectors, so no built-in vectorizer is needed.
                        }
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration(vectorSearchHnswConfig)
                        {
                            Parameters = new HnswParameters
                            {
                                M = 4,
                                EfConstruction = 400,
                                EfSearch = 500,
                                Metric = "cosine"
                            }
                        }
                    },
                    // Register only the text vectorizer since image embeddings are handled externally.
                    Vectorizers =
                    {
                        new AzureOpenAIVectorizer(textVectorizer)
                        {
                            Parameters = new AzureOpenAIVectorizerParameters
                            {
                                ResourceUri = new Uri(_aoaiEndpoint!),
                                ModelName = _aoaiEmbeddingModel,
                                DeploymentName = _aoaiEmbeddingDeplopyment,
                                ApiKey = _aoaiApiKey
                            }
                        }
                        // No image vectorizer registration is required here.
                    }
                },

                SemanticSearch = new()
                {
                    Configurations =
                    {
                        new SemanticConfiguration(semanticSearchConfig, new()
                        {
                            TitleField = new SemanticField("manufacturer"),
                            ContentFields =
                            {
                                new SemanticField("pole_marking"),
                                new SemanticField("pole_2"),
                                new SemanticField("colour"),
                                new SemanticField("seam_marking")
                            }
                        })
                    }
                },
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SearchableField("manufacturer") { IsFilterable = true, IsSortable = true },
                    new SearchableField("usga_lot_num") { IsFilterable = true },
                    new SearchableField("pole_marking") { IsFilterable = true },
                    new SearchableField("colour") { IsFilterable = true },
                    new SearchableField("constCode") { IsFilterable = true },
                    new SearchableField("ballSpecs") { IsFilterable = true },
                    new SimpleField("dimples", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                    new SearchableField("spin") { IsFilterable = true },
                    new SearchableField("pole_2") { IsFilterable = true },
                    new SearchableField("seam_marking") { IsFilterable = true },
                    new SimpleField("imageUrl", SearchFieldDataType.String) { IsFilterable = false },
                    new SearchField("textVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 1536,
                        VectorSearchProfileName = textVectorProfile
                    },
                    new SearchField("imageVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 1024,
                        VectorSearchProfileName = "image-vector-profile"
                    }
                }
            };

            await indexClient.CreateOrUpdateIndexAsync(searchIndex);
        }

        /// <summary>
        /// This function is used to search the V2 index for Capabilities it returns a List of Capabilities
        /// </summary>
        /// <param name="searchClient"></param>
        /// <param name="query"></param>
        /// <param name="k"></param>
        /// <param name="top"></param>
        /// <param name="filter"></param>
        /// <param name="textOnly"></param>
        /// <param name="hybrid"></param>
        /// <param name="semantic"></param>
        /// <param name="minRerankerScore"></param>
        /// <returns>List&lt;Capability&gt;</returns>
        public async Task<List<GolfBallDataV1>> SearchTextOnly(
            SearchClient searchClient,
            string query,
            int k = 3,
            int top = 10,
            string? filter = null,
            bool textOnly = false,
            bool hybrid = true,
            bool semantic = false,
            double minRerankerScore = 2.0)
        {
            var searchOptions = new SearchOptions
            {
                Filter = filter,
                Size = top,
                Select = { "id", "manufacturer", "pole_marking", "colour", "seam_marking" },
                IncludeTotalCount = true
            };

            if (!textOnly)
            {
                searchOptions.VectorSearch = new()
                {
                    Queries = {
                new VectorizableTextQuery(text: query)
                {
                    KNearestNeighborsCount = k,
                    Fields = { "vectorContent" }
                }
            }
                };
            }

            if (hybrid || semantic)
            {
                searchOptions.QueryType = SearchQueryType.Semantic;
                searchOptions.SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "golf-semantic-config",
                    QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                    QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive),
                };
            }

            string? queryText = (textOnly || hybrid || semantic) ? query : null;
            SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(queryText, searchOptions);

            var documents = new List<GolfBallDataV1>();
            await foreach (var result in response.GetResultsAsync())
            {
                // Use RerankerScore if available; otherwise, fallback to Score for filtering
                double? relevanceScore = result.SemanticSearch?.RerankerScore ?? result.Score;

                double adjustedMinRerankerScore = textOnly ? 0.03 : minRerankerScore;

                //if (result.SemanticSearch?.RerankerScore >= minRerankerScore)
                //{
                if (relevanceScore >= adjustedMinRerankerScore)
                {
                    //Capability capability = new Capability
                    //{
                    //    Id = (string)result.Document["id"],
                    //    Name = (string)result.Document["name"],
                    //    Description = (string)result.Document["description"],
                    //    CapabilityType = (string)result.Document["capabilityType"],
                    //    Tags = ((string[])result.Document["tags"]).ToList(),
                    //    Parameters = result.Document["parameters"]?.ToString() is string paramStr ?
                    //        JsonSerializer.Deserialize<List<Parameter>>(paramStr) ?? new List<Parameter>() : new List<Parameter>(),
                    //    ExecutionMethod = result.Document["executionMethod"]?.ToString() is string execStr ?
                    //        JsonSerializer.Deserialize<ExecutionMethod>(execStr) ?? new ExecutionMethod() : new ExecutionMethod()
                    //};

                    documents.Add(new GolfBallDataV1
                    {
                        Id = result.Document?["id"].ToString() ?? String.Empty,
                        Manufacturer = result.Document?["manufacturer"].ToString() ?? String.Empty,
                        Pole_Marking = result.Document?["pole_marking"].ToString() ?? String.Empty,
                        Colour = result.Document?["colour"].ToString() ?? String.Empty,
                        Seam_Marking = result.Document?["seam_marking"].ToString() ?? String.Empty
                    });

                    _logger.LogDebug("Search Result - Manufacturer: {Manufacturer}, Score: {Score}, RerankerScore: {RerankerScore}",
                        result.Document?["manufacturer"].ToString(),
                        result.Score,
                        result.SemanticSearch?.RerankerScore);
                }
            }

            _logger.LogDebug("Total Results: {Count}", documents.Count);
            return documents;
        }

        public async Task<List<GolfBallDataV2>> SearchImageOnly(
           SearchClient searchClient,
           float[] imageEmbedding,
           int k = 3,
           int top = 10,
           string? filter = null,
           bool enableSemanticRanking = true)
        {
            try
            {
                var results = new List<GolfBallDataV2>();
                var tempScores = new Dictionary<string, (double? Score, double? RerankerScore)>();

                // Set up search options with vector query
                var searchOptions = new SearchOptions
                {
                    Filter = filter,
                    Size = top,
                    Select = { "id", "manufacturer", "usga_lot_num", "pole_marking", "colour",
                      "constCode", "ballSpecs", "dimples", "spin", "pole_2",
                      "seam_marking", "imageUrl" },
                    VectorSearch = new()
                    {
                        Queries = {
                    new VectorizedQuery(imageEmbedding)
                    {
                        KNearestNeighborsCount = k,
                        Fields = { "imageVector" }
                    }
                }
                    },
                    // Enable scoring
                    IncludeTotalCount = true
                };

                // Add semantic ranking if requested
                if (enableSemanticRanking)
                {
                    searchOptions.QueryType = SearchQueryType.Semantic;
                    searchOptions.SemanticSearch = new()
                    {
                        SemanticConfigurationName = "golf-semantic-config",
                        QueryCaption = new(QueryCaptionType.Extractive),
                        QueryAnswer = new(QueryAnswerType.Extractive)
                    };
                }

                // Perform the search
                SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);

                // First pass: Process document data and store scores separately
                await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
                {
                    var id = result.Document["id"].ToString();

                    var golfBall = new GolfBallDataV2
                    {
                        Id = id!,
                        Manufacturer = result.Document["manufacturer"].ToString(),
                        USGA_Lot_Num = result.Document["usga_lot_num"].ToString(),
                        Pole_Marking = result.Document["pole_marking"].ToString(),
                        Colour = result.Document["colour"].ToString(),
                        ConstCode = result.Document["constCode"].ToString(),
                        BallSpecs = result.Document["ballSpecs"].ToString(),
                        Dimples = Convert.ToInt32(result.Document["dimples"]),
                        Spin = result.Document["spin"].ToString(),
                        Pole_2 = result.Document["pole_2"].ToString(),
                        Seam_Marking = result.Document["seam_marking"].ToString(),
                        ImageUrl = result.Document["imageUrl"].ToString()
                    };

                    results.Add(golfBall);

                    // Store the scores separately
                    tempScores[id!] = (result.Score, result.SemanticSearch?.RerankerScore);
                }
                foreach (var entry in tempScores)
                {
                    string id = entry.Key;
                    var scoreData = entry.Value;
                    Console.WriteLine($"{id}\t{scoreData.Score}\t{scoreData.RerankerScore}");
                }

                //// Second pass: Attach scores to results after they've been created
                //foreach (var golfBall in results)
                //{
                //    if (tempScores.TryGetValue(golfBall.Id, out var scoreData))
                //    {
                //        Console.WriteLine
                //        golfBall.Score = scoreData.Score;
                //        golfBall.RerankerScore = scoreData.RerankerScore;
                //    }
                //}

                // Order results appropriately
                //return enableSemanticRanking
                //    ? results.OrderByDescending(r => r.RerankerScore ?? r.Score).ToList()
                //    : results.OrderByDescending(r => r.Score).ToList();
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchV3: {ex.Message}");
                throw;
            }
        }

        //public async Task<List<GolfBallDataV2>> ProcessTestImagesAndSearch(
        //  SearchClient searchClient,
        //  float[] imageEmbedding,
        //  int k = 3,
        //  int top = 10,
        //  string? filter = null,
        //  bool enableSemanticRanking = true)
        //{

        //    var results = new List<GolfBallDataV2>();
        //    var tempScores = new Dictionary<string, (double? Score, double? RerankerScore)>();
        //    await Task.Delay(1000);
        //    return results;
        //}

        //}
    }
}
