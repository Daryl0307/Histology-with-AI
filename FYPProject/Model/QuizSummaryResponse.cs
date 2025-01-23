namespace FYPProject.Model
{
    public class QuizSummaryResponse
    {
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public double Score { get; set; }
    }

}