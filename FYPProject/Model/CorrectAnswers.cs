namespace FYPProject.Model
{
    public class CorrectAnswers
    {
        public string QuizCategory { get; set; }
        public int QuestionId { get; set; }
        public string GroupedAnswerText { get; set; }
        public int IsCorrect { get; set; }
    }
}
