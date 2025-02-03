using System.ComponentModel.DataAnnotations;

namespace FYPProject.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        [Required(ErrorMessage = "The QuestionText field is required.")]
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public double QuestionMark { get; set; }
        public int QuizId { get; set; } // Foreign Key


    }

}