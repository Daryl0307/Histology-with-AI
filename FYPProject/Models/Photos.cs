using System.ComponentModel.DataAnnotations.Schema;

namespace FYPProject.Models
{
    public class Photos
    {
        public int Photo_ID { get; set; }
        public string Photo_Description { get; set; }
        public string Photo_URL { get; set; }
        [NotMapped]
        public IFormFile? PhotoFile { get; set; }
        public int? Tissue_ID { get; set; }
        public int? UserId { get; set; }
        public int? QuizId { get; set; }


    }

}