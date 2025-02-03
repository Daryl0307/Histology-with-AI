using System.ComponentModel.DataAnnotations;

namespace FYPProject.Models
{
    public class Quiz
    {
        public int QuizId { get; set; }
        [Required(ErrorMessage = "The QuizCategory field is required.")]
        public string QuizCategory { get; set; }
    }
}