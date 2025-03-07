using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
//
using Azure_AI_Search_API.Interfaces;
using Azure_AI_Search_API.Models;
using Azure.Search.Documents.Models;
using Azure;
using Swashbuckle.AspNetCore.Annotations;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Search.Documents.Indexes;
//

namespace Azure_AI_Search_API.Controllers
{
    /// <summary>
    /// Controller for managing vector search indexes and performing search operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Search Operations")]
    [Produces("application/json")]
    public class SearchController : ControllerBase
    {
        private readonly IIndexService _indexService;
        private readonly ILogger<IndexController> _logger;
        private readonly IConfiguration _configuration;
        private readonly TokenCredential _credential;
        private readonly SearchIndexClient _indexClient;
        private readonly AzureOpenAIClient _azureOpenAIClient;

        public SearchController(IConfiguration configuration, ILogger<IndexController> logger, TokenCredential credential, IIndexService indexService)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _logger = logger;
            _configuration = configuration;
            _indexClient = _indexService.InitializeSearchIndexClient(_credential);
            _azureOpenAIClient = _indexService.InitializeOpenAIClient(_credential);
        }

        #region Search Operations

        /// <summary>
        /// Performs a search against a text embeddings index
        /// </summary>
        /// <remarks>
        /// Searches the specified text embeddings index using the provided search string
        /// </remarks>
        /// <param name="indexName">The name of the index to search</param>
        /// <param name="request">The search request containing the search string and other parameters</param>
        /// <returns>Search results matching the query</returns>
        [HttpPost("text/{indexName}/search")]
        [ProducesResponseType(typeof(List<GolfBallDataV1>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SearchTextIndex(
            [Required] string indexName,
            [FromBody] SearchTextRequest request)
        {  // TBD 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var searchClient = _indexClient.GetSearchClient(indexName);
                var searchResults = await _indexService.SearchTextOnly(
                    searchClient,
                    request.Query,
                        request.K,
                        request.Top,
                        request.Filter,
                        request.TextOnly,
                        request.Hybrid,
                        request.Semantic);
                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs a search against a text and image embeddings index
        /// </summary>
        /// <remarks>
        /// Searches the specified text and image embeddings index using the provided search criteria, which may include text and/or images
        /// </remarks>
        /// <param name="indexName">The name of the index to search</param>
        /// <param name="request">The search request containing the search criteria and other parameters</param>
        /// <returns>Search results matching the query</returns>
        [HttpPost("textimage/{indexName}/search")]
        [ProducesResponseType(typeof(List<GolfBallDataV1>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> SearchTextImageIndex(
            [Required] string indexName,
            [FromForm] GolfBallImageRequest request)
        { // TBD
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // var searchResults = await _indexService.SearchTextImageIndexAsync(indexName, request);
                // Process the first image if available
                List<GolfBallDataV2> allResults = new List<GolfBallDataV2>();

                foreach (var image in request.Images)
                {
                    float[]? imageEmbedding = null;
                    _logger.LogInformation("Processing first image: {FileName}, Size: {Size} bytes", image.FileName, image.Length);
                    // Convert IFormFile to byte array
                    using var memoryStream = new MemoryStream();
                    await image.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();

                    using var imageStream = image.OpenReadStream();
                    imageEmbedding = await _indexService.GenerateImageEmbeddingsAsyncV3(imageBytes);

                    // Now that we have the embeddings for the image we need to
                    // perform a Semantic Simalarity Service againts the image embeddings stored in the Vector Store
                    var searchClient = _indexClient.GetSearchClient(indexName);

                    // Perform the search based on available inputs
                    List<GolfBallDataV2> imageResults;
                    imageResults = await _indexService.SearchTextImageOnly(
                              searchClient,
                              imageEmbedding
                         );

                    // Add all results from this iumage to the accumulated list
                    allResults.AddRange(imageResults);
                }
                return Ok(allResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion
    }

  
}
