using System.ComponentModel.DataAnnotations;

namespace Azure_AI_Search_API.Models
{
    public class GolfBallImageRequest
    {
        [Required]
        public List<IFormFile> Images { get; set; }
    }
}
