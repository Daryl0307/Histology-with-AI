using System.ComponentModel.DataAnnotations;

namespace FYPProject.Models
{
    public class Answer
    {
        public int QuestionId { get; set; }
        public int AnswerId { get; set; }
        [Required(ErrorMessage = "The AnswerText field is required.")]
        public string AnswerText { get; set; }
        [Required(ErrorMessage = "The IsCorrect field is required.")]
        public bool Is_Correct { get; set; }
        [Required(ErrorMessage = "The Marks field is required.")]
        public double Marks { get; set; }

    }

}