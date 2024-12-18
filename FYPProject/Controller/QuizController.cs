using Microsoft.AspNetCore.Mvc;
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
    /*
    public IActionResult ListQuiz()
    {
        List<project.Model.Quiz> quizzes = DBUtl.GetList<project.Model.Quiz>("SELECT * FROM Quiz");
        return View(quizzes);
    }

    public IActionResult AddQuiz()
    {
        var model = new QuizViewModel();
        return View("AddQuiz", model);
    }


    [HttpPost]
    public IActionResult AddQuiz(QuizViewModel model)
    {

        string errors = "";

        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState.Values)
            {
                foreach (var error in state.Errors)
                {
                    errors += error;
                }
            }
        }
        else
        {
            string insertQuiz = @"INSERT INTO Quiz(QuizCategory, Quiz_Total_Mark) OUTPUT INSERTED.QuizId VALUES('{0}', '{1}')";
            DataTable dt = DBUtl.GetTable(insertQuiz, model.Quiz.QuizCategory, model.Quiz.QuizTotalMarks);

            if (dt.Rows.Count == 1)
            {
                int quizId = Convert.ToInt32(dt.Rows[0]["QuizId"]);
                string insertQuestion = @"INSERT INTO Question(Quiz_Id, QuestionText, QuestionMarks, QuestionType) OUTPUT INSERTED.QuestionId VALUES('{0}', '{1}', '{2}', '{3}')";
                DataTable dtQuestion = DBUtl.GetTable(insertQuestion, quizId, model.Question.QuestionText, model.Question.QuestionMark, model.Question.QuestionType);

                if (dtQuestion.Rows.Count == 1)
                {
                    int questionId = Convert.ToInt32(dtQuestion.Rows[0]["QuestionId"]);
                    for (int i = 0; i < model.Answer.Count; i++)
                    {
                        string insertAnswer = @"INSERT INTO Answer(QuestionId, AnswerText, IsCorrect, AnswerMarks) VALUES({0}, '{1}', {2}, {3})";
                        int resultanswer = DBUtl.ExecSQL(insertAnswer, questionId, model.Answer[i].AnswerText, model.Answer[i].IsCorrect ? 1 : 0, model.Answer[i].Marks);

                        if (resultanswer != 1)
                        {
                            // Handle failure for individual answer insertion if needed
                            TempData["Message"] = "Failed to insert one or more answers.";
                            TempData["MsgType"] = "danger";
                            return RedirectToAction("AddQuiz");
                        }
                    }
                    TempData["Message"] = "Quiz created successfully!";
                    TempData["MsgType"] = "success";
                }
            }

        }

        return RedirectToAction("ListQuiz");
    }


    public IActionResult ManageQuestions(int quizId)
    {
        List<Question> questions = DBUtl.GetList<Question>("SELECT * FROM Question WHERE QuizId = {0}", quizId);
        ViewData["QuizId"] = quizId;
        return View(questions);
    }
    */
}