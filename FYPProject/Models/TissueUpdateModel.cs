using System.ComponentModel.DataAnnotations.Schema;

namespace FYPProject.Models
{
    public class TissueUpdateModel
    {
        public int TissueId { get; set; }
        public string TissueName { get; set; }
        public string TissueDescription { get; set; }
        public int PhotoId { get; set; }
        public string? Photo_URL { get; set; }
        public IEnumerable<Photos>?Photos { get; set; } = new List<Photos>();
        public IEnumerable<IFormFile>? PhotoFiles { get; set; }
        


    }
}
