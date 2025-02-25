namespace api_azure_ai_search.Models
{
    public class GolfBallDataV2
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Manufacturer { get; set; } = string.Empty;
        public string USGA_Lot_Num { get; set; } = string.Empty;
        public string Pole_Marking { get; set; } = string.Empty;
        public string Colour { get; set; } = string.Empty;
        public string ConstCode { get; set; } = string.Empty;
        public string BallSpecs { get; set; } = string.Empty;
        public int Dimples { get; set; }
        public string Spin { get; set; } = string.Empty;
        public string Pole_2 { get; set; } = string.Empty;
        public string Seam_Marking { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public IList<float> TextVector { get; set; } = new List<float>();
        // Embedding for the image (1024 dimensions)
        public List<float> ImageVector { get; set; } = new List<float>();
    }
}
