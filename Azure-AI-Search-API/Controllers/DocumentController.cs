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
    [Route("api/Document")]
    [SwaggerTag("Document Management")]
    [Produces("application/json")]
    public class DocumentController : ControllerBase
    {
        private readonly IIndexService _indexService;
        private readonly ILogger<IndexController> _logger;
        private readonly IConfiguration _configuration;
        private readonly TokenCredential _credential;
        private readonly SearchIndexClient _indexClient;
        private readonly AzureOpenAIClient _azureOpenAIClient;

        public DocumentController(IConfiguration configuration, ILogger<IndexController> logger, TokenCredential credential, IIndexService indexService)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _logger = logger;
            _configuration = configuration;
            _indexClient = _indexService.InitializeSearchIndexClient(_credential);
            _azureOpenAIClient = _indexService.InitializeOpenAIClient(_credential);
        }


        #region Document Management

        /// <summary>
        /// Lists documents in a text embeddings index
        /// </summary>
        /// <remarks>
        /// Returns a list of documents from the specified text embeddings index
        /// </remarks>
        /// <param name="indexName">The name of the index</param>
        /// <param name="suppressVectorFields">Whether to suppress vector fields in the response</param>
        /// <param name="maxResults">The maximum number of results to return (default: 100)</param>
        /// <returns>A list of documents</returns>
        [HttpGet("text/{indexName}/documents")]
        [ProducesResponseType(typeof(List<SearchDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ListTextDocuments(
            [Required] string indexName,
            [FromQuery] bool suppressVectorFields = true,
            [FromQuery] int maxResults = 100)
        {  // TBD
            if (maxResults <= 0 || maxResults > 1000)
            {
                return BadRequest("MaxResults must be between 1 and 1000");
            }

            try
            {
                var documents = await _indexService.ListDocumentsAsync(indexName, _indexClient ,suppressVectorFields, maxResults);
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

        /// <summary>
        /// Lists documents in a text and image embeddings index
        /// </summary>
        /// <remarks>
        /// Returns a list of documents from the specified text and image embeddings index
        /// </remarks>
        /// <param name="indexName">The name of the index</param>
        /// <param name="suppressVectorFields">Whether to suppress vector fields in the response</param>
        /// <param name="maxResults">The maximum number of results to return (default: 100)</param>
        /// <returns>A list of documents</returns>
        [HttpGet("textimage/{indexName}/documents")]
        [ProducesResponseType(typeof(List<SearchDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ListTextImageDocuments(
            [Required] string indexName,
            [FromQuery] bool suppressVectorFields = true,
            [FromQuery] int maxResults = 100)
        { // TBD
            if (maxResults <= 0 || maxResults > 1000)
            {
                return BadRequest("MaxResults must be between 1 and 1000");
            }

            try
            {
                var documents = await _indexService.ListDocumentsAsync(indexName, _indexClient, suppressVectorFields, maxResults);
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

        #endregion

    }

}
