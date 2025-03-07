namespace Azure_AI_Search_API.Models
{
    public class IndexStatistics
    {
        public long VectorIndexSize { get; set; }
        public long DocumentCount { get; set; }
        public long StorageSizeInBytes { get; set; }
    }
}
