namespace FYPProject.Model
{
    public class Photo
    {
        public int PhotoId { get; set; }
        public string? PhotoDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public IFormFile? PhotoFile { get; set; }
        public int? TissueId { get; set; }
        public int? UserId { get; set; }
        public int? QuizId { get; set; }


    }

}