using System.ComponentModel.DataAnnotations;

namespace FYPProject.Models
{
    public class QuizResponse
    {
        public int ResponseId { get; set; }               // Response_ID is the primary key
        public int UserId { get; set; }                   // User_ID from the table
        public int QuizId { get; set; }                   // Quiz_ID from the table
        public int QuestionId { get; set; }               // Question_ID from the table
        public int? AnswerId { get; set; }                // Answer_ID is nullable, as it may not have one
        public bool IsCorrect { get; set; }
        public string QuizCategory { get; set; }               // Is_Correct from the table
        public DateTime ResponseTime { get; set; }        // Response_Time from the table
        public double Score { get; set; }                 // This can be inferred from Is_Correct and Marks (if needed)
    }


}