using System.ComponentModel.DataAnnotations;

namespace FYPProject.Model
{
    public class QuizViewModelForManagement
    {
        public int Question_ID { get; set; }
        public string Quiz_Category { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public double QuestionMarks { get; set; }
        public string? Photo_URL { get; set; }
        public IFormFile? Photo { get; set; }
    }
}