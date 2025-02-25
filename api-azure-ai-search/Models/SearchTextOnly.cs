using System.ComponentModel;

namespace api_azure_ai_search.Models
{
    public class SearchTextRequest
    {
        [DefaultValue("Pinetree, white, TQX with S in gold")]
        public string Query { get; set; } = "Pinetree, white, TQX with S in gold";

        [DefaultValue(3)]
        public int K { get; set; } = 3;

        [DefaultValue(10)]
        public int Top { get; set; } = 10;

        [DefaultValue(null)]
        public string? Filter { get; set; } = null;

        [DefaultValue(false)]
        public bool TextOnly { get; set; } = false;

        [DefaultValue(true)]
        public bool Hybrid { get; set; } = true;

        [DefaultValue(false)]
        public bool Semantic { get; set; } = false;

        [DefaultValue(2.0)]
        public double MinRerankerScore { get; set; } = 2.0;
    }
}
