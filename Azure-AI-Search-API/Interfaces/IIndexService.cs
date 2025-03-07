using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure_AI_Search_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure_AI_Search_API.Interfaces
{
    /// <summary>
    /// Service interface for managing vector search indexes and operations
    /// </summary>
    public interface IIndexService
    {

        /// <summary>
        /// Initializes and returns an AzureOpenAIClient, mostly used for embeddings
        /// </summary>
        AzureOpenAIClient InitializeOpenAIClient(TokenCredential credential);

        /// <summary>
        /// Initializes and returns an SearchIndexClient, used for perform operations against the index
        /// </summary>
        SearchIndexClient InitializeSearchIndexClient(TokenCredential credential);

        /// <summary>
        /// Gets a list of all available index names
        /// </summary>
        Task<IReadOnlyList<string>> GetIndexesAsync(SearchIndexClient indexClient);

        /// <summary>
        /// Creates a new text embeddings index
        /// </summary>
        /// <param name="indexName">The name for the new index</param>
        /// <param name="indexClient">The actual indexClient to use</param>
        Task CreateTextIndexAsync(string indexName, SearchIndexClient indexClient);

        /// <summary>
        /// Creates a new text and image embeddings index
        /// </summary>
        /// <param name="indexName">The name for the new index</param>
        /// <param name="indexClient">The name for the new index</param>
        Task CreateTextImageIndexAsync(string indexName, SearchIndexClient indexClient);

        /// <summary>
        /// Gets detailed information about a specific index
        /// </summary>
        /// <param name="indexName">The name for the new index</param>
        /// <param name="indexClient">The actual indexClient to use</param>
        Task<SearchIndexDetails> GetIndexDetailsAsync(string indexName, SearchIndexClient indexClient);

        /// <summary>
        /// Gets statistics for a specific index
        /// </summary>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexClient">The actual indexClient to use</param>
        Task<IndexStatistics> GetIndexStatisticsAsync(string indexName, SearchIndexClient indexClient);

        /// <summary>
        /// Deletes a specific index
        /// </summary>
        /// <param name="indexName">The name of the index to delete</param>
        /// <param name="indexClient">The actual indexClient to use</param>
        /// <returns>True if the index was deleted, false if it doesn't exist</returns>
        Task DeleteIndexAsync(string indexName, SearchIndexClient indexClient);

        /// <summary>
        /// Lists documents in a text embeddings index or text and image embeddings index
        /// </summary>
        /// <param name="indexName">The name of the index</param>
        /// <param name="searchClient">The actual searchClient to use</param>
        /// <param name="indexClient">The actual indexClient to use</param>
        /// <param name="suppressVectorFields">Whether to suppress vector fields in the response</param>
        /// <param name="maxResults">The maximum number of results to return</param>
        Task<List<SearchDocument>> ListDocumentsAsync(
                string indexName,
                SearchIndexClient indexClient,
                bool suppressVectorFields = true,
                int maxResults = 1000);



        /// <summary>
        /// Generates embeddings for a text-only index
        /// </summary>
        /// <param name="azureOpenAIClient">The actual azureOpenAIClient to use to generate the embeddings</param>
        /// <param name="searchClient">The actual searchClient to use to add the documents to the index</param>
        Task GenerateTextEmbeddingsAsync(AzureOpenAIClient azureOpenAIClient, SearchClient searchClient);

        /// <summary>
        /// Generates embeddings for a text-only index
        /// </summary>
        /// <param name="azureOpenAIClient">The actual azureOpenAIClient to use to generate the embeddings</param>
        /// <param name="searchClient">The actual searchClient to use to add the documents to the index</param>
        Task GenerateTextImageEmbeddingsAsync(AzureOpenAIClient azureOpenAIClient, SearchClient searchClient);

        /// <summary>
        /// Performs a search against a text embeddings index
        /// </summary>
        /// <param name="searchClient">The actual searchClient to use to add the documents to the index</param>
        /// <param name="query">The query to use when searching the index</param>
        Task<List<GolfBallDataV1>> SearchTextOnly(
            SearchClient searchClient,
            string query,
            int k = 3,
            int top = 10,
            string? filter = null,
            bool textOnly = false,
            bool hybrid = true,
            bool semantic = false,
            double minRerankerScore = 2.0);

        /// <summary>
        /// Performs a search against a text and image embeddings index
        /// </summary>
        /// <param name="searchClient">The actual searchClient to use to add the documents to the index</param>
        /// <param name="imageEmbedding">An array of float which represents the vectors for the provided image</param>
        Task<List<GolfBallDataV2>> SearchTextImageOnly(
           SearchClient searchClient,
           float[] imageEmbedding,
           int k = 3,
           int top = 10,
           string? filter = null,
           bool enableSemanticRanking = true);

        /// <summary>
        /// Generate Image Embeddings for a provided byte array
        /// </summary>
        /// <param name="imageBytes">The provide IFormFile needs to be converted to an array of bytes then passed</param>
        /// <returns>
        /// Array of Float which are the vectors for the image
        /// </returns>
        Task<float[]> GenerateImageEmbeddingsAsyncV3(byte[] imageBytes);

        Task UploadGolfBallDataTextOnlyAsync(AzureOpenAIClient azureOpenAIClient, SearchClient searchClient);
        Task UploadGolfBallDataTextImageAsyncV2(AzureOpenAIClient azureOpenAIClient, SearchClient searchClient);
    }
    
}
