namespace FYPProject.Model
{
    public class QuizStatistics
    {
        public int UserId { get; set; }
        public string Username { get; set; }

        public string QuizCategory { get; set; }
        public DateTime DateAttempted { get; set; }
        public double Score { get; set; }

    }

}