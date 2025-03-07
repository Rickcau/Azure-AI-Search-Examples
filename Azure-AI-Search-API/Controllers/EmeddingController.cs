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
    [SwaggerTag("Embedding Generation")]
    [Produces("application/json")]
    public class EmeddingController : ControllerBase
    {
        private readonly IIndexService _indexService;
        private readonly ILogger<IndexController> _logger;
        private readonly IConfiguration _configuration;
        private readonly TokenCredential _credential;
        private readonly SearchIndexClient _indexClient;
        private readonly AzureOpenAIClient _azureOpenAIClient;

        public EmeddingController(IConfiguration configuration, ILogger<IndexController> logger, TokenCredential credential, IIndexService indexService)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _logger = logger;
            _configuration = configuration;
            _indexClient = _indexService.InitializeSearchIndexClient(_credential);
            _azureOpenAIClient = _indexService.InitializeOpenAIClient(_credential);
        }

        #region Embedding Generation

        /// <summary>
        /// Generates embeddings for a text-only index
        /// </summary>
        /// <remarks>
        /// Processes and generates embeddings for the specified text-only index
        /// </remarks>
        /// <param name="indexName">The request containing the index name and data to process</param>
        /// <returns>A summary of the embedding generation process</returns>
        [HttpPost("text/{indexName}/embeddings")]
        [SwaggerOperation(
           Summary = "Generate embeddings for a text only index",
           Description = "Generate embeddings for a text only index"
        )]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GenerateTextEmbeddings(
            [Required] string indexName)
        { // TBD
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var searchClient = _indexClient.GetSearchClient(indexName);
                await _indexService.GenerateTextEmbeddingsAsync(_azureOpenAIClient, searchClient);
                return Ok("Successfully generated and stored embeddings");
            }
            catch (RequestFailedException ex) when (ex.Status == 400)
            {
                return BadRequest($"Indexname must be provided");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates embeddings for a text and image index
        /// </summary>
        /// <remarks>
        /// Processes and generates embeddings for the specified text and image index
        /// </remarks>
        /// <param name="request">The request containing the index name and data to process</param>
        /// <returns>A summary of the embedding generation process</returns>
        [HttpPost("textimage/{indexName}/embeddings")]
        [SwaggerOperation(
           Summary = "Generate embeddings for a multi-modal index",
           Description = "Generate embeddings for a multi-modal index"
        )]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GenerateTextImageEmbeddings(
            [Required] string indexName)
        { // TBD
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // var result = await _indexService.GenerateTextImageEmbeddingsAsync(indexName, request);
                var searchClient = _indexClient.GetSearchClient(indexName);
                await _indexService.GenerateTextImageEmbeddingsAsync(_azureOpenAIClient, searchClient);
                return Ok("Successfully generated and stored embeddings");
            }
            catch (RequestFailedException ex) when (ex.Status == 400)
            {
                return BadRequest($"Indexname must be provided");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

    }

}
