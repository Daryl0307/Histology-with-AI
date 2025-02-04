

namespace FYPProject.Models
{
    public class QuizViewModel
    {
        public Quiz Quiz { get; set; }
        public Question Question { get; set; }
        public List<Answer> Answer { get; set; } = new List<Answer>();
        public string Photo_URL { get; set; }
        public IEnumerable<IFormFile>? PhotoFiles { get; set; }
    }
}