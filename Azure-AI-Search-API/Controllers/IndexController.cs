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
using Azure.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Azure.Search.Documents.Indexes;
using Azure.AI.OpenAI;
using Helper.AzureOpenAISearchHelper;
//

namespace Azure_AI_Search_API.Controllers
{
    /// <summary>
    /// Controller for managing vector search indexes and performing search operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Index Operations")]
    [Produces("application/json")]
    public class IndexController : ControllerBase
    {
        private readonly IIndexService _indexService;
        private readonly ILogger<IndexController> _logger;
        private readonly IConfiguration _configuration;
        private readonly TokenCredential _credential;
        private readonly SearchIndexClient _indexClient;
        private readonly AzureOpenAIClient _azureOpenAIClient;
       // private readonly Azure.Search.Documents.SearchClient _searchClient;

        public IndexController(IConfiguration configuration, ILogger<IndexController> logger, TokenCredential credential, IIndexService indexService)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _logger = logger;
            _configuration = configuration; 
            _indexClient = _indexService.InitializeSearchIndexClient(_credential);
            _azureOpenAIClient = _indexService.InitializeOpenAIClient(_credential);
        }

        #region Index Management

        /// <summary>
        /// Retrieves all available index names
        /// </summary>
        /// <remarks>
        /// Returns a list of all index names that are currently available in the system
        /// </remarks>
        /// <returns>A list of index names</returns>
        [HttpGet]
        [SwaggerOperation(
           Summary = "Get all Indexes",
           Description = "Returns a list of Indexes"
        )]
        [ProducesResponseType(typeof(IEnumerable<string>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<string>>> GetAllIndexNames()
        { 
            var indexNames = await _indexService.GetIndexesAsync(_indexClient);
            return Ok(indexNames);
        }

        /// <summary>
        /// Creates a new text embeddings index
        /// </summary>
        /// <remarks>
        /// Creates a new index that will use text embeddings with the specified name
        /// </remarks>
        /// <param name="request">The request containing the index name</param>
        /// <returns>Details of the created index</returns>
        [HttpPost("text")]
        [SwaggerOperation(
           Summary = "Create a new index with text embeddings",
           Description = "Create a new index with text embeddings only"
        )]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateTextIndex([Required] string indexName)
        { 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _indexService.CreateTextIndexAsync(indexName, _indexClient);
                return CreatedAtAction(nameof(CreateTextIndex), new { indexName }, indexName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }


        }

        /// <summary>
        /// Creates a new text and image embeddings index
        /// </summary>
        /// <remarks>
        /// Creates a new index that will use both text and image embeddings with the specified name
        /// </remarks>
        /// <param name="request">The request containing the index name</param>
        /// <returns>Details of the created index</returns>
        [HttpPost("textimage")]
        [SwaggerOperation(
           Summary = "Create a new index with text and image embeddings",
           Description = "Create a new index with text and image embeddings"
        )]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateTextImageIndex([Required] string indexName)
        { 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _indexService.CreateTextImageIndexAsync(indexName, _indexClient);
            return CreatedAtAction(nameof(CreateTextImageIndex), new { indexName }, indexName);
        }

        /// <summary>
        /// Gets detailed information about a specific index
        /// </summary>
        /// <remarks>
        /// Returns detailed information about the index with the specified name
        /// </remarks>
        /// <param name="indexName">The name of the index</param>
        /// <returns>Details of the specified index</returns>
        [HttpGet("{indexName}/details")]
        [SwaggerOperation(
           Summary = "Retreive index details",
           Description = "Retreive index details"
        )]
        [ProducesResponseType(typeof(SearchIndexDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetIndexDetails([Required] string indexName)
        { 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var indexDetails = await _indexService.GetIndexDetailsAsync(indexName, _indexClient);
            if (indexDetails == null)
            {
                return NotFound($"Index '{indexName}' not found");
            }

            return Ok(indexDetails);
        }

        /// <summary>
        /// Gets statistics for a specific index
        /// </summary>
        /// <remarks>
        /// Returns statistical information about the index with the specified name
        /// </remarks>
        /// <param name="indexName">The name of the index</param>
        /// <returns>Statistics for the specified index</returns>
        [HttpGet("{indexName}/statistics")]
        [SwaggerOperation(
           Summary = "Retreive index statistics",
           Description = "Retreive index statistics"
        )]
        [ProducesResponseType(typeof(IndexStatistics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetIndexStatistics([Required] string indexName)
        {  
            var indexStats = await _indexService.GetIndexStatisticsAsync(indexName, _indexClient);
            if (indexStats == null)
            {
                return NotFound($"Index '{indexName}' not found");
            }

            return Ok(indexStats);
        }

        /// <summary>
        /// Deletes a specific index
        /// </summary>
        /// <remarks>
        /// Permanently deletes the index with the specified name and all its data
        /// </remarks>
        /// <param name="indexName">The name of the index to delete</param>
        /// <returns>No content</returns>
        [HttpDelete("{indexName}")]
        [SwaggerOperation(
           Summary = "Delete an Index",
           Description = "Delete an Index"
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteIndex([Required] string indexName)
        { // TBD
          // var success = await _indexService.DeleteIndexAsync(indexName);
            try
            {
                await _indexService.DeleteIndexAsync(indexName, _indexClient);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

       
    }
}
