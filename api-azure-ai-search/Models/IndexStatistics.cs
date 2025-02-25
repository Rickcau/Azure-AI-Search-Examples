namespace api_azure_ai_search.Models
{
    public class IndexStatistics
    {
        public long VectorIndexSize { get; set; }
        public long DocumentCount { get; set; }
        public long StorageSizeInBytes { get; set; }
    }
}
