using System.ComponentModel.DataAnnotations;

namespace FYPProject.Model
{
    public class Quiz
    {
        public int QuizId { get; set; }
        [Required(ErrorMessage = "The QuizCategory field is required.")]
        public string QuizCategory { get; set; }
    }
}