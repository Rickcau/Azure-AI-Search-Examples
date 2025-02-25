using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Mvc;
using Helper.AzureOpenAISearchHelper;
using System.Net.Mime;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.OpenApi.Models;
using Azure.Core;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Search.Documents.Models;
using System.Runtime.InteropServices;
using api_azure_ai_search.Models;
using Microsoft.Extensions.Configuration;

namespace api_gen_ai_itops.Controllers
{
    [ApiController]
    [Route("indexes")]
    public class IndexesController : ControllerBase
    {
        private readonly ILogger<IndexesController> _logger;
        private readonly IConfiguration _configuration;
        private readonly AISearchHelper _aiSearchHelper;
        private readonly SearchIndexClient _indexClient;
        private readonly TokenCredential _credential;
        private readonly AzureOpenAIClient _azureOpenAIClient;
        private readonly Azure.Search.Documents.SearchClient _searchClient;

        public IndexesController(IConfiguration configuration, ILogger<IndexesController> logger, TokenCredential credential)
        {
            _logger = logger;
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _configuration = configuration;
            _aiSearchHelper = new AISearchHelper(_configuration);
            // Initialize clients
            _azureOpenAIClient = _aiSearchHelper.InitializeOpenAIClient(_credential);
            _indexClient = _aiSearchHelper.InitializeSearchIndexClient(_credential);
            // _searchClient = _indexClient.GetSearchClient(configuration.IndexName);
        }

        // GET: api/indexes
        [SwaggerOperation(
           Summary = "Gets all Indexes",
           Description = "Returns a list of AI Search Indexes"
        )]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetIndexes()
        {
            try
            {
                // TODO: Implement logic to list all indexes
                // This could use SearchIndexClient to list available indexes
                var list = await _aiSearchHelper.GetIndexesAsync(_indexClient);
                await Task.Delay(100);
                return Ok(list); // Placeholder
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [SwaggerOperation(
            Summary = "Create a new text only Golfball index",
            Description = "Creates a new Azure AI Search index with text only embeddings")]
        [HttpPost("textOnly/{indexName}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTextIndex(string indexName)
        {
            try
            {
                await _aiSearchHelper.SetupIndexTextAsync(indexName, _indexClient);
                return CreatedAtAction(nameof(CreateTextIndex), new { indexName }, indexName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [SwaggerOperation(
           Summary = "Create a new text and image Golfball index",
           Description = "Creates a new Azure AI Search index with text and image embeddings")]
        [HttpPost("textImage/{indexName}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTextImageIndex(string indexName)
        {
            try
            {
                await _aiSearchHelper.SetupIndexTextImageAsync(indexName, _indexClient);
                return CreatedAtAction(nameof(CreateTextImageIndex), new { indexName }, indexName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("generate/textOnly/{indexName}")]
        [SwaggerOperation(
                Summary = "Generate embeddings for for textOnly Index",
                Description = "Generates and stores embeddings for all golfballs in the textOnly search index")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateTextEmbeddings(string indexName)
        {
            try
            {
                var searchClient = _indexClient.GetSearchClient(indexName);
                await _aiSearchHelper.UploadGolfBallDataTextOnlyAsync(_azureOpenAIClient, searchClient);
                return Ok("Successfully generated and stored embeddings for capabilities");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("generate/textImage/{indexName}")]
        [SwaggerOperation(
               Summary = "Generate embeddings for a text and image embeddings Index",
               Description = "Generates and stores embeddings for all golfballs allowing for text and image search")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateTextImageEmbeddings(string indexName)
        {
            try
            {
                var searchClient = _indexClient.GetSearchClient(indexName);
                await _aiSearchHelper.UploadGolfBallDataTextImageAsync(_azureOpenAIClient, searchClient);
                return Ok("Successfully generated and stored embeddings for capabilities");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        /// <summary>
        /// This is the function / operation that is used to search for capabilities
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="request"></param>
        /// <returns>List&lt;Capability&gt;</returns>
        [HttpPost("textonly/search")]
        [SwaggerOperation(
            Summary = "Search index using text only",
            Description = "Performs hybrid search against capabilities index including vector, text, and semantic search")]
        [ProducesResponseType(typeof(List<GolfBallDataV1>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchTextOny(
            [FromQuery][Required] string indexName,
            [FromBody] SearchTextRequest request)
        {
            try
            {
                    _logger.LogInformation("Text only Index is being searched.  Query: {Query}", request.Query);
                    var searchClient = _indexClient.GetSearchClient(indexName);
                    var results = await _aiSearchHelper.SearchTextOnly(
                        searchClient,
                        request.Query,
                        request.K,
                        request.Top,
                        request.Filter,
                        request.TextOnly,
                        request.Hybrid,
                        request.Semantic);
                    return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching capabilities: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("search/with-images")]
        [SwaggerOperation(
            Summary = "Search index using uploaded images",
            Description = "Performs semantic search using uploaded image")]
        [ProducesResponseType(typeof(List<GolfBallDataV2>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SearchWithImages(
            [FromQuery][Required] string indexName,
            [FromForm] IFormFile? image = null)
        {
            try
            {
                _logger.LogInformation("Search with images against index: {IndexName}", indexName);

                // Validate inputs
                if ( image == null)
                {
                    return BadRequest("At least one image must be provided");
                }

                // Process the first image if available


                float[]? imageEmbedding = null;
                if (image != null && image.Length > 0)
                {
                    _logger.LogInformation("Processing first image: {FileName}, Size: {Size} bytes",
                        image.FileName, image.Length);

                    // Convert IFormFile to byte array
                    using var memoryStream = new MemoryStream();
                    await image.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();

                    using var imageStream = image.OpenReadStream();
                    imageEmbedding = await _aiSearchHelper._ballHelper.GenerateImageEmbeddingAsyncV3(imageBytes);

                }

                // Get the search client for the specified index
                var searchClient = _indexClient.GetSearchClient(indexName);

                // Perform the search based on available inputs
                List<GolfBallDataV2> results;
                results = await _aiSearchHelper.SearchImageOnly(
                          searchClient,
                          imageEmbedding
                     );

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching with images: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        /// <summary>
        /// Gets detailed information about a search index
        /// </summary>
        /// <param name="indexName">Name of the index to retrieve details for</param>
        /// <returns>Details of the specified search index</returns>
        /// <response code="200">Returns the index details</response>
        /// <response code="400">If indexName is null</response>
        /// <response code="404">If the index is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [SwaggerOperation(
            Summary = "Get index details",
            Description = "Retrieves detailed information about a specific Azure AI Search index")]
        [HttpGet("details")]
        [ProducesResponseType(typeof(SearchIndexDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetIndexDetails([FromQuery][Required] string indexName)
        {
            try
            {
                // TODO: Implement logic to get specific index details
                var indexname = indexName ?? throw new ArgumentNullException(nameof(indexName));
                var details = await _aiSearchHelper.GetIndexDetailsAsync(indexname, _indexClient);
                return Ok(details); // Placeholder
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [SwaggerOperation(
            Summary = "Get index statistics",
            Description = "Returns statistics for an indxe given an indexName")]
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(IndexStatistics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetIndexStatistics([FromQuery][Required] string indexName)
        {
            try
            {
                var stats = await _aiSearchHelper.GetIndexStatisticsAsync(indexName, _indexClient);
                return Ok(stats);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound($"Index '{indexName}' not found");
            }
        }

        // DELETE: a/indexes/{indexName}
        [SwaggerOperation(
            Summary = "Delete an index using the Index Name",
            Description = "Returns a 204 if the Index was deleted."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete]
        public async Task<IActionResult> DeleteIndex([FromQuery] string? indexName = null)
        {
            try
            {
                var indexname = indexName ?? throw new ArgumentNullException(nameof(indexName));
                await _aiSearchHelper.DeleteIndexAsync(indexname, _indexClient);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lists documents in a search index
        /// </summary>
        [SwaggerOperation(
            Summary = "Lists documents for a textOnly index",
            Description = "Returns a list of documents for a textOnly index")]
        [HttpGet("textOnly/documents")]
        [ProducesResponseType(typeof(List<SearchDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListTextOnlyDocuments(
           [FromQuery][Required] string indexName,
           [FromQuery] bool surpressVectorFields = true,
           [FromQuery] int maxResults = 1000)
        {
            try
            {
                var searchClient = _indexClient.GetSearchClient(indexName);
                var documents = await _aiSearchHelper.ListDocumentsAsync(indexName, searchClient, _indexClient, surpressVectorFields, maxResults);
                return Ok(documents);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound($"Index '{indexName}' not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [SwaggerOperation(
           Summary = "Lists documents for a textImage index",
           Description = "Returns a list of textImage documents for a textImage index")]
        [HttpGet("textImage/documents")]
        [ProducesResponseType(typeof(List<SearchDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListTextImageDocuments(
          [FromQuery][Required] string indexName,
          [FromQuery] bool surpressVectorFields = true,
          [FromQuery] int maxResults = 1000)
        {
            // RDC: tBD need to fix this code so it calls a function for textImage Documents only
            try
            {
                var searchClient = _indexClient.GetSearchClient(indexName);
                var documents = await _aiSearchHelper.ListDocumentsAsync(indexName, searchClient, _indexClient, surpressVectorFields, maxResults);
                return Ok(documents);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound($"Index '{indexName}' not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    // DTOs for request/response models
    public class IndexCreateModel
    {
        // Add properties needed for index creation
        public string? Name { get; set; }
        // Add other configuration properties
    }

    public class IndexUpdateModel
    {
        // Add properties needed for index updates
        public string? Name { get; set; }
        // Add other configuration properties
    }
}
