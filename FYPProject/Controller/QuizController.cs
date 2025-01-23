using FYPProject.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RP.SOI.DotNet.Utils;
using System.Data;


namespace FYPProject.Controllers;

public class QuizController : Controller
{
    public IActionResult Main()
    {

        return View();
    }
    public IActionResult Management()
    {
        return View();
    }
    private readonly IWebHostEnvironment _env;


    public QuizController(IWebHostEnvironment environment)
    {

        _env = environment;
    }

    public IActionResult HistoQuiz()
    {

        List<HistoQuiz> quizlist = DBUtl.GetList<HistoQuiz>("SELECT Quiz.Quiz_Category AS 'QuizCategory',  SUM(Q.QuestionMarks) AS 'TotalQuestionMarks' FROM Quiz INNER JOIN Question Q ON Quiz.Quiz_ID = Q.Quiz_ID  GROUP BY Quiz.Quiz_Category");

        List<QuizStatistics> statisticslist = DBUtl.GetList<QuizStatistics>("SELECT User_Id AS 'UserId', Quiz_Category AS 'QuizCategory', Date_Attempted AS 'DateAttempted', Score FROM Quiz_Statistics WHERE User_Id = {0}", 1);

        string getPhotoUrl = @"SELECT Photo_URL FROM Photos WHERE Quiz_Id = {0}";



        var viewModel = new HistoQuizViewModel
        {
            HistoQuiz = quizlist,
            QuizStatistics = statisticslist
        };
        ViewData["Tissue_Info"] = GetListTissue();
        return View(viewModel);
    }

    public IActionResult AnswerList(int id)
    {

        List<Answer> answerList = DBUtl.GetList<Answer>("SELECT Answer_ID AS 'AnswerId', Question_ID AS 'QuestionId' , AnswerText, CAST(Is_Correct AS BIT) AS 'Is_Correct', AnswerMarks AS 'Marks' FROM Answer WHERE Question_ID =  {0}", id);
        if (answerList == null || answerList.Count == 0)
        {
            TempData["Message"] = "No data found. Please check your query.";
            TempData["MsgType"] = "danger";
            return View();
        }
        ViewBag.QuestionId = answerList[0].QuestionId;

        return View(answerList);
    }

    [HttpGet]
    public IActionResult TakeQuiz(string quizCategory)
    {
        int userId = 1; // Replace with the logged-in user's ID
        string questionWithAnswersSql = @"
    SELECT 
        q.Question_ID AS 'QuestionId', q.QuestionText, q.QuestionType, q.QuestionMarks AS 'QuestionMark', 
        q.Quiz_ID AS 'QuizId', Quiz.Quiz_Category AS 'QuizCategory', 
        a.Answer_ID AS 'AnswerId', a.AnswerText, a.Is_Correct, a.AnswerMarks AS 'Marks', P.Photo_URL AS 'Photo_Url'
    FROM 
        Question q
    INNER JOIN 
        Quiz ON q.Quiz_ID = Quiz.Quiz_ID
    INNER JOIN
        Photos P ON P.Quiz_ID = q.Quiz_ID
    LEFT JOIN 
        Answer a ON q.Question_ID = a.Question_ID
    WHERE 
        Quiz.Quiz_Category = '{0}' AND 
        q.Question_ID NOT IN (SELECT Question_Id FROM UserAnsweredQuestions WHERE User_Id = {1})
    ORDER BY 
        NEWID();";

        var rawResults = DBUtl.GetTable(questionWithAnswersSql, quizCategory, userId);

        if (rawResults.Rows.Count > 0)
        {
            var currentQuestionId = rawResults.Rows[0].Field<int>("QuestionId");
            string insertAnsweredQuestionSql = @"
        INSERT INTO UserAnsweredQuestions (User_Id, Question_Id, QuizCategory) 
        VALUES ({0}, {1}, '{2}')";

            int rowsAffected = DBUtl.ExecSQL(insertAnsweredQuestionSql, userId, currentQuestionId, quizCategory);

            if (rowsAffected == 1)
            {
                return PrepareQuestionView(rawResults);
            }
            else
            {
                TempData["Message"] = $"Error saving answered question (ID: {currentQuestionId}). Please try again.";
                TempData["MsgType"] = "danger";
                return RedirectToAction("HistoQuiz");
            }
        }
        else
        {
            TempData["Message"] = "No more questions available.";
            return RedirectToAction("HistoQuiz");
        }
    }
    private IActionResult PrepareQuestionView(DataTable rawResults)
    {
        var groupedData = rawResults.AsEnumerable()
            .GroupBy(row => new
            {
                QuestionId = row.Field<int>("QuestionId"),
                QuestionText = row.Field<string>("QuestionText"),
                QuestionType = row.Field<string>("QuestionType"),
                QuestionMark = row.Field<double>("QuestionMark"),
                QuizId = row.Field<int>("QuizId"),
                QuizCategory = row.Field<string>("QuizCategory"),
                Photo_Url = row.Field<string>("Photo_Url")
            });

        var firstQuestionGroup = groupedData.FirstOrDefault();
        if (firstQuestionGroup != null)
        {
            var questionKey = firstQuestionGroup.Key;

            var quizViewModel = new QuizViewModel
            {
                Quiz = new Quiz
                {
                    QuizCategory = questionKey.QuizCategory
                },
                Question = new Question
                {
                    QuestionId = questionKey.QuestionId,
                    QuestionText = questionKey.QuestionText,
                    QuestionType = questionKey.QuestionType,
                    QuestionMark = questionKey.QuestionMark,
                    QuizId = questionKey.QuizId
                },
                Photo = new Photo
                {
                    PhotoUrl = questionKey.Photo_Url
                },
                Answer = firstQuestionGroup.Select(row => new Answer
                {
                    AnswerId = row.Field<int?>("AnswerId") ?? 0,
                    QuestionId = questionKey.QuestionId,
                    AnswerText = row.Field<string>("AnswerText"),
                    Is_Correct = row.Field<bool?>("Is_Correct") ?? false,
                    Marks = row.Field<double?>("Marks") ?? 0
                }).ToList()
            };

            return View("TakeQuiz", quizViewModel);
        }
        return View();
    }
    [HttpPost]
    public IActionResult SubmitAnswer(QuizResponse model, List<int> selectedAnswerIds)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("TakeQuiz", new { quizCategory = model.QuizCategory });
        }

        double totalScore = 0;
        bool isFullyCorrect = true;

        foreach (var answerId in selectedAnswerIds)
        {
            string correctnessSql = @"SELECT Is_Correct, AnswerMarks AS Marks FROM Answer WHERE Answer_ID = {0}";
            var answer = DBUtl.GetList<Answer>(correctnessSql, answerId).FirstOrDefault();

            if (answer != null)
            {
                totalScore += answer.Is_Correct ? (double)answer.Marks : 0;
                if (!answer.Is_Correct)
                {
                    isFullyCorrect = false;
                }

                string insertResponseSql = @"
            INSERT INTO Quiz_Responses (User_ID, Quiz_ID, Question_ID, Answer_ID, Quiz_Category, Is_Correct, Response_Time, Score) 
            VALUES ({0}, {1}, {2}, {3}, '{4}', {5}, GETDATE(),{6})";

                int result = DBUtl.ExecSQL(
                    insertResponseSql,
                    1,
                    model.QuizId,
                    model.QuestionId,
                    answerId,
                    model.QuizCategory,
                    answer.Is_Correct ? 1 : 0,
                    answer.Marks
                );

                if (result != 1)
                {
                    TempData["Message"] = $"Error saving response for Answer ID {answerId}: {DBUtl.DB_Message}";
                    TempData["MsgType"] = "danger";
                }
                else
                {
                    TempData["Message"] = "Answer submitted successfully.";
                    TempData["MsgType"] = "success";
                }

            }
            else
            {
                TempData["Message"] = $"Invalid answer ID {answerId}.";
                TempData["MsgType"] = "danger";
            }
        }

        model.Score = totalScore;
        model.IsCorrect = isFullyCorrect;



        return RedirectToAction("NextQuestion", new { quizCategory = model.QuizCategory, questionId = model.QuestionId });
    }

    public IActionResult NextQuestion(string quizCategory)
    {
        int userId = 1; // Replace with the logged-in user's ID

        string questionWithAnswersSql = @"
            SELECT 
                q.Question_ID AS 'QuestionId', q.QuestionText, q.QuestionType, q.QuestionMarks AS 'QuestionMark', 
                q.Quiz_ID AS 'QuizId', Quiz.Quiz_Category AS 'QuizCategory', 
                a.Answer_ID AS 'AnswerId', a.AnswerText, a.Is_Correct, a.AnswerMarks AS 'Marks', P.Photo_URL AS 'Photo_Url'
            FROM 
                Question q
            INNER JOIN 
                Quiz ON q.Quiz_ID = Quiz.Quiz_ID
            INNER JOIN
                Photos P ON P.Quiz_ID = q.Quiz_ID
            LEFT JOIN 
                Answer a ON q.Question_ID = a.Question_ID
            WHERE 
                Quiz.Quiz_Category = '{0}' AND 
                q.Question_ID NOT IN (SELECT Question_Id FROM UserAnsweredQuestions WHERE User_Id = {1})
            ORDER BY 
                NEWID();"; // Randomize the next question

        var rawResults = DBUtl.GetTable(questionWithAnswersSql, quizCategory, userId);

        if (rawResults.Rows.Count > 0)
        {
            var currentQuestionId = rawResults.Rows[0].Field<int>("QuestionId");
            string insertAnsweredQuestionSql = @"
        INSERT INTO UserAnsweredQuestions (User_Id, Question_Id, QuizCategory) 
        VALUES ({0}, {1}, '{2}')";

            int rowsAffected = DBUtl.ExecSQL(insertAnsweredQuestionSql, userId, currentQuestionId, quizCategory);

            if (rowsAffected == 1)
            {
                var groupedData = rawResults.AsEnumerable()
                    .GroupBy(row => new
                    {
                        QuestionId = row.Field<int>("QuestionId"),
                        QuestionText = row.Field<string>("QuestionText"),
                        QuestionType = row.Field<string>("QuestionType"),
                        QuestionMark = row.Field<double>("QuestionMark"),
                        QuizId = row.Field<int>("QuizId"),
                        QuizCategory = row.Field<string>("QuizCategory"),
                        Photo_Url = row.Field<string>("Photo_Url")
                    });

                var nextQuestionGroup = groupedData.FirstOrDefault();
                if (nextQuestionGroup != null)
                {
                    var questionKey = nextQuestionGroup.Key;

                    var quizViewModel = new QuizViewModel
                    {
                        Quiz = new Quiz
                        {
                            QuizCategory = questionKey.QuizCategory
                        },
                        Question = new Question
                        {
                            QuestionId = questionKey.QuestionId,
                            QuestionText = questionKey.QuestionText,
                            QuestionType = questionKey.QuestionType,
                            QuestionMark = questionKey.QuestionMark,
                            QuizId = questionKey.QuizId
                        },
                        Photo = new Photo
                        {
                            PhotoUrl = questionKey.Photo_Url
                        },
                        Answer = nextQuestionGroup.Select(row => new Answer
                        {
                            AnswerId = row.Field<int?>("AnswerId") ?? 0,
                            QuestionId = questionKey.QuestionId,
                            AnswerText = row.Field<string>("AnswerText"),
                            Is_Correct = row.Field<bool?>("Is_Correct") ?? false,
                            Marks = row.Field<double?>("Marks") ?? 0
                        }).ToList()
                    };

                    return View("TakeQuiz", quizViewModel);
                }
            }
            else
            {
                TempData["Message"] = $"Error saving answered question (ID: {currentQuestionId}). Please try again.";
                TempData["MsgType"] = "danger";
                return RedirectToAction("HistoQuiz");
            }
        }
        else
        {
            TempData["Message"] = "You have completed this quiz or no more questions are available.";
            TempData["MsgType"] = "info";
            return RedirectToAction("QuizSummary", new { quizCategory });
        }

        return View(); // Default view if no data
    }
    public IActionResult QuizSummary(string quizCategory)
    {
        // Fetch responses for the quiz
        string summarySql = @"
    SELECT q.QuestionText, a.AnswerText, CAST(r.Is_Correct AS BIT) AS'IsCorrect', Score
    FROM Quiz_Responses r
    INNER JOIN Question q ON r.Question_ID = q.Question_ID
    INNER JOIN Answer a ON r.Answer_ID = a.Answer_ID
    WHERE r.Quiz_Category = '{0}'";

        var responses = DBUtl.GetList<QuizSummaryResponse>(summarySql, quizCategory);

        if (responses.Count == 0)
        {
            TempData["Message"] = "No responses found for this quiz.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("HistoQuiz");
        }

        double totalScore = responses.Sum(r => r.Score);
        ViewBag.TotalScore = totalScore;

        int userid = 1;

        string checkAttemptsSql = @"SELECT COUNT(*) FROM Quiz_Statistics WHERE User_Id = {0} and Quiz_Category = '{1}'";
        int checkAttempts = Convert.ToInt32(DBUtl.GetValue(checkAttemptsSql, userid, quizCategory));

        checkAttempts = checkAttempts + 1;
        string insertSql = @"
        INSERT INTO Quiz_Statistics (User_ID, Quiz_Category, Date_Attempted, Score)
        VALUES ({0}, '{1}', GETDATE(), {2})";
        int result = DBUtl.ExecSQL(insertSql, userid, quizCategory, totalScore);



        return View(responses);
    }
    public IActionResult Quit()
    {
        string deleteResponsesql = @"DELETE FROM Quiz_Responses";
        int resultResponse = DBUtl.ExecSQL(deleteResponsesql);
        string deleteSavedQuestionId = @"DELETE FROM UserAnsweredQuestions";
        int resultDeleteSaved = DBUtl.ExecSQL(deleteSavedQuestionId);

        return RedirectToAction("HistoQuiz");
    }
    private static SelectList GetListTissue()
    {
        //string tissueSql = @"SELECT LTRIM(CONVERT(Tissue_ID, CHAR)) as Value, Tissue_Name as Text FROM Tissue_Info;";
        string tissueSql = @"SELECT LTRIM(Tissue_ID) as 'Value', Tissue_Name as 'Text' FROM Tissue_Info";
        List<SelectListItem> lstTissue = DBUtl.GetList<SelectListItem>(tissueSql);
        return new SelectList(lstTissue, "Value", "Text");
    }
}