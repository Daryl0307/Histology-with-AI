using FYPProject.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RP.SOI.DotNet.Utils;
using System.Data;


namespace FYPProject.Controllers;

public class QuizController : Controller
{
    
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

        string deleteResponsesql = @"DELETE FROM Quiz_Responses";
        int resultResponse = DBUtl.ExecSQL(deleteResponsesql);
        string deleteSavedQuestionId = @"DELETE FROM UserAnsweredQuestions";
        int resultDeleteSaved = DBUtl.ExecSQL(deleteSavedQuestionId);

        double totalMarks = 0;
        double totalScore = 0;
        double noofpass = 0;
        double passingScore = 0;
        for (int i = 0; i < quizlist.Count; i++)
        {

            passingScore = quizlist[i].TotalQuestionMarks / 2;
            for (int j = 0; j < statisticslist.Count; j++)
            {
                totalMarks += quizlist[i].TotalQuestionMarks;
                totalScore += statisticslist[j].Score;
                if (statisticslist[j].Score >= passingScore)
                {
                    noofpass++;
                }
            }

        }

        double avgScore = (totalScore / totalMarks) * 100;

        double passpercent = ((noofpass / statisticslist.Count) * 100);

        ViewBag.passpercent = Math.Round(passpercent, 2);
        ViewBag.avgScore = Math.Round(avgScore, 2);

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

    public IActionResult QuizView()
    {
        List<HistoQuiz> quizlist = DBUtl.GetList<HistoQuiz>("SELECT Quiz.Quiz_Category AS 'QuizCategory',  SUM(Q.QuestionMarks) AS 'TotalQuestionMarks' FROM Quiz INNER JOIN Question Q ON Quiz.Quiz_ID = Q.Quiz_ID  GROUP BY Quiz.Quiz_Category");
        List<QuizStatistics> statisticslist = DBUtl.GetList<QuizStatistics>("SELECT User_Id AS 'UserId', Quiz_Category AS 'QuizCategory', Date_Attempted AS 'DateAttempted', Score FROM Quiz_Statistics ");

        double totalMarks = 0;
        double totalScore = 0;
        double noofpass = 0;
        double passingScore = 0;
        for (int i = 0; i < quizlist.Count; i++)
        {

            passingScore = quizlist[i].TotalQuestionMarks / 2;
            for (int j = 0; j < statisticslist.Count; j++)
            {
                totalMarks += quizlist[i].TotalQuestionMarks;
                totalScore += statisticslist[j].Score;
                if (statisticslist[j].Score >= passingScore)
                {
                    noofpass++;
                }
            }
            ViewData["tissue_info"] = GetListTissue();
            string totalAttemptssql = @"SELECT COUNT(*) FROM Quiz_Statistics";
            int totalAttempts = Convert.ToInt32(DBUtl.GetValue(totalAttemptssql));
            ViewBag.TotalAttempts = totalAttempts;

        }


        double passpercent = ((noofpass / statisticslist.Count) * 100);

        ViewBag.passpercent = Math.Round(passpercent, 2);

        return View(quizlist);
    }

    public IActionResult Management(string quizCategory)
    {
        string query = @"
        SELECT 
            Q.Question_ID, 
            Quiz.Quiz_Category, 
            Q.QuestionText, 
            Q.QuestionType, 
            Q.QuestionMarks, 
            P.Photo_URL,
            A.AnswerText,
            A.Is_Correct,
            A.AnswerMarks
        FROM 
            Question Q
        INNER JOIN 
            Quiz ON Q.Quiz_ID = Quiz.Quiz_ID
        INNER JOIN 
            Photos P ON P.Quiz_ID = Quiz.Quiz_ID
        LEFT JOIN 
            Answer A ON A.Question_ID = Q.Question_ID
        WHERE 
            Quiz.Quiz_Category = '{0}'
        ORDER BY 
            Q.Question_ID, A.AnswerText
       
        ";

        var data = DBUtl.GetTable(query, quizCategory);

        if (data.Rows.Count == 0)
        {
            TempData["Message"] = "No data found. Please check your query.";
            TempData["MsgType"] = "danger";
            return View();
        }

        // Group answers by Question_ID
        var groupedQuizzes = data.AsEnumerable()
            .GroupBy(row => Convert.ToInt32(row["Question_ID"])) // Explicitly convert group.Key
            .Select(group =>
            {
                var quiz = new QuizViewModel
                {
                    Question = new Question
                    {
                        QuestionId = group.Key, // Explicitly converted group.Key
                        QuestionText = group.First()["QuestionText"].ToString(),
                        QuestionType = group.First()["QuestionType"].ToString(),
                        QuestionMark = Convert.ToDouble(group.First()["QuestionMarks"])
                    },
                    Quiz = new Quiz
                    {
                        QuizCategory = group.First()["Quiz_Category"].ToString(),
                    },

                    Photo = new Photo
                    {
                        PhotoUrl = group.First()["Photo_URL"].ToString()
                    },
                    Answer = group.Select(row => new Answer
                    {
                        AnswerText = row["AnswerText"].ToString(),
                        Is_Correct = row["Is_Correct"] != DBNull.Value && Convert.ToBoolean(row["Is_Correct"]),
                        Marks = row["AnswerMarks"] == DBNull.Value ? 0 : Convert.ToDouble(row["AnswerMarks"])
                    }).ToList()
                };
                return quiz;
            })
            .ToList();

        return View(groupedQuizzes);

    }

    public IActionResult AddQuiz()
    {
        var model = new QuizViewModel
        {
            Quiz = new Quiz(),
            Question = new Question(),
            Answer = new List<Answer> { new Answer(), new Answer() }
        };
        ViewData["Tissue_Info"] = GetListTissue();
        return View(model);
    }

    [HttpPost]
    public IActionResult AddQuiz(QuizViewModel model)
    {
        ModelState.Remove("Photo");
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                Console.WriteLine($"{state.Key} :: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            return View("AddQuiz", model);
        }
        else
        {

            string insertQuiz = @"INSERT INTO Quiz(Quiz_Category) OUTPUT INSERTED.Quiz_ID VALUES('{0}')";
            int newQuizId = DBUtl.ExecSQLReturnId(insertQuiz, model.Quiz.QuizCategory);


            if (newQuizId >= 1)
            {
                if (model.Photo != null && model.Photo.PhotoFile != null)
                {
                    string picfilename = DoPhotoUpload(model.Photo.PhotoFile, model.Quiz.QuizCategory);
                    string insertPhoto = @"INSERT INTO Photos (Photo_URL, Quiz_ID) VALUES ('{0}', {1})";
                    int result = DBUtl.ExecSQL(insertPhoto, picfilename, newQuizId);
                }
                else
                {
                    string picfilename = "No Picture Inserted";
                    string insertPhoto = @"INSERT INTO Photos (Photo_URL, Quiz_ID) VALUES ('{0}', {1})";
                    int result = DBUtl.ExecSQL(insertPhoto, picfilename, newQuizId);
                }




                model.Question.QuizId = newQuizId;
                string insertQuestion = @"INSERT INTO Question(Quiz_ID, QuestionText, QuestionMarks, QuestionType) OUTPUT INSERTED.Question_ID VALUES('{0}', '{1}', '{2}', '{3}')";
                int newQuestionId = DBUtl.ExecSQLReturnId(insertQuestion, model.Question.QuizId, model.Question.QuestionText, model.Question.QuestionMark, model.Question.QuestionType);

                if (newQuestionId >= 1)
                {

                    foreach (var answer in model.Answer)
                    {
                        answer.QuestionId = newQuestionId;
                        string insertAnswer = @"INSERT INTO Answer(Question_ID, AnswerText, Is_Correct, AnswerMarks) VALUES({0}, '{1}', {2}, {3})";
                        int resultAnswer = DBUtl.ExecSQL(insertAnswer, answer.QuestionId, answer.AnswerText, answer.Is_Correct ? 1 : 0, answer.Marks);

                        if (resultAnswer <= 1)
                        {
                            TempData["Message"] = DBUtl.DB_Message;
                            TempData["MsgType"] = "danger";

                        }
                        else
                        {
                            TempData["Message"] = "Quiz created successfully!";
                            TempData["MsgType"] = "success";
                        }
                    }


                }
                else
                {
                    TempData["Message"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";
                }


            }
            else
            {
                TempData["Message"] = DBUtl.DB_Message;
                TempData["MsgType"] = "danger";
            }

            return RedirectToAction("QuizView");
        }


    }

    public IActionResult AddAnswer(int id)
    {
        string questionsql = @"SELECT * FROM Question WHERE Question_ID = {0}";
        int result = DBUtl.ExecSQL(questionsql, id);
        ViewBag.QuestionId = id;
        if (result == 0)
        {
            TempData["Message"] = "Answer record does not exist";
            TempData["MsgType"] = "danger";
            return RedirectToAction("Management");
        }
        return View("AddAnswer");

    }

    [HttpPost]
    public IActionResult AddAnswer(Answer model)
    {
        string categorySql = @"SELECT Quiz_Category AS 'Quiz Category' FROM Quiz WHERE Quiz_ID = {0}";
        string category = Convert.ToString(DBUtl.GetValue(categorySql, model.QuestionId));

        ModelState.Remove("AnswerId");
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                Console.WriteLine($"{state.Key} :: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            return View("AddAnswer", model);
        }
        else
        {
            string answertablesql = @"SELECT * FROM Answer WHERE Question_ID = {0}";
            var data = DBUtl.GetTable(answertablesql, model.QuestionId);
            if (data.Rows.Count == 4)
            {
                TempData["Message"] = "Only 4 answers or less";
                TempData["MsgType"] = "danger";
            }
            else
            {
                string insertAnswer = @"INSERT INTO Answer(Question_ID, AnswerText, Is_Correct, AnswerMarks) VALUES({0}, '{1}', {2}, {3})";
                int resultAnswer = DBUtl.ExecSQL(insertAnswer, model.QuestionId, model.AnswerText, model.Is_Correct ? 1 : 0, model.Marks);

                if (resultAnswer < 1)
                {


                    TempData["Message"] = DBUtl.DB_Message;
                    TempData["MsgType"] = "danger";

                }
                else
                {
                    string questionmarksSql = @"SELECT QuestionMarks FROM Question WHERE Question_ID = {0}";
                    double questionMarks = Convert.ToDouble(DBUtl.GetValue(questionmarksSql, model.QuestionId));
                    questionMarks = questionMarks + model.Marks;
                    string updateQuestionMarksSql = @"UPDATE Question SET QuestionMarks = {1} WHERE Question_ID = {0}";
                    int result = DBUtl.ExecSQL(updateQuestionMarksSql, model.QuestionId, questionMarks);

                    if (result == 1)
                    {
                        TempData["Message"] = "Answer created successfully!";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Message"] = DBUtl.DB_Message;
                        TempData["MsgType"] = "danger";
                    }

                }
            }
        }
        return RedirectToAction("Management", new { quizCategory = category });
    }

    [HttpGet]
    public IActionResult UpdateAnswer(int id)
    {
        string questionsql = @"SELECT Question_ID AS 'QuestionId', Answer_ID AS 'AnswerId', AnswerText,
            Is_Correct,
            AnswerMarks AS 'Marks' FROM Answer WHERE Answer_ID = {0}";
        List<Answer> lstAnswer = DBUtl.GetList<Answer>(questionsql, id);
        if (lstAnswer.Count == 1)
        {

            Answer quiz = lstAnswer[0];

            return View(quiz);
        }
        else
        {
            TempData["Message"] = "Answer record does not exist";
            TempData["MsgType"] = "danger";
            return RedirectToAction("UpdateAnswer");
        }

    }

    [HttpPost]
    public IActionResult UpdateAnswer(Answer model)
    {
        string categorySql = @"SELECT Quiz_Category AS 'Quiz Category' FROM Quiz WHERE Quiz_ID = {0}";
        string category = Convert.ToString(DBUtl.GetValue(categorySql, model.QuestionId));
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                Console.WriteLine($"{state.Key} :: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            return View("UpdateAnswer", model); // Return the view with the model
        }

        string questionTypeSql = @"SELECT Q.QuestionType FROM Answer INNER JOIN Question Q ON Q.Question_ID = Answer.Question_ID WHERE Answer.Question_ID = {0} AND Answer_ID = {1}";
        string questionType = Convert.ToString(DBUtl.GetValue(questionTypeSql, model.QuestionId, model.AnswerId));

        string noofCorrectAnswerSql = @"SELECT COUNT(*) FROM Answer WHERE Is_Correct = 1 AND Question_ID = {0}";
        int noofCorrectAnswer = Convert.ToInt32(DBUtl.GetValue(noofCorrectAnswerSql, model.QuestionId));

        string AnswerTextsql = @"SELECT AnswerText FROM Answer WHERE Answer_ID = {0}";
        string answerText = Convert.ToString(DBUtl.GetValue(AnswerTextsql, model.AnswerId));

        if (questionType == "Multiple Choice (Radio)" && noofCorrectAnswer == 1 && model.Is_Correct)
        {
            TempData["Message"] = "You're not allowed to have more than 1 correct answer in this question type.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("AnswerList", new { id = model.QuestionId });
        }
        else if (questionType == "Multiple Choice (Dropdown)" && noofCorrectAnswer == 1 && model.Is_Correct)
        {
            TempData["Message"] = "You're not allowed to have more than 1 correct answer in this question type.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("AnswerList", new { id = model.QuestionId });

        }
        else if (questionType == "Multiple Choice (Radio)" && noofCorrectAnswer == 1 && !model.Is_Correct && answerText == model.AnswerText)
        {
            TempData["Message"] = "You're not allowed to have no correct answer.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("AnswerList", new { id = model.QuestionId });
        }
        else if (questionType == "Multiple Choice (Dropdown)" && noofCorrectAnswer == 1 && !model.Is_Correct && answerText == model.AnswerText)
        {
            TempData["Message"] = "You're not allowed to have no correct answer.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("AnswerList", new { id = model.QuestionId });
        }


        string questionidsql = @"SELECT Question_ID FROM Answer WHERE Answer_ID = {0}";
        int questionid = Convert.ToInt32(DBUtl.GetValue(questionidsql, model.AnswerId));
        string getQuestionMarksSql = @"SELECT QuestionMarks FROM Question WHERE Question_ID = {0}";
        double questionMarks = Convert.ToDouble(DBUtl.GetValue(getQuestionMarksSql, questionid));
        string answerMarksSql = @"SELECT AnswerMarks FROM Answer WHERE Answer_ID = {0}";
        string existingAnswersSql = @"SELECT COUNT(*) FROM Answer WHERE Question_ID = {0} AND Is_Correct = 0";
        int incorrectAnswersCount = Convert.ToInt32(DBUtl.GetValue(existingAnswersSql, questionid));
        string noofanswersql = @"SELECT COUNT(*) FROM Answer WHERE Question_ID = {0}";
        int noofanswer = Convert.ToInt32(DBUtl.GetValue(noofanswersql, questionid));
        // Check if at least one correct answer exists for the question
        string correctAnswersSql = @"SELECT COUNT(*) FROM Answer WHERE Question_ID = {0} AND Is_Correct = 1";
        int correctAnswersCount = Convert.ToInt32(DBUtl.GetValue(correctAnswersSql, questionid));




        // Doesn't allow 0 for marks input
        if (model.Is_Correct && model.Marks == 0)
        {
            TempData["Message"] = "Please input more than 0 marks.";
            TempData["MsgType"] = "danger";
            return RedirectToAction("NewManagement", new { quizCategory = category });
        }

        bool hasOnlyOneIncorrect = incorrectAnswersCount == 1;

        if (hasOnlyOneIncorrect && model.Is_Correct == true)
        {
            TempData["Message"] = "Too much correct answers";
            TempData["MsgType"] = "danger";
            return RedirectToAction("NewManagement", new { quizCategory = category });
        }

        double answerMarks = Convert.ToDouble(DBUtl.GetValue(answerMarksSql, model.AnswerId));
        if (answerMarks == null)
        {
            throw new Exception($"No AnswerMarks found for Answer_ID = {model.AnswerId}");
        }
        double minusMarks = answerMarks - model.Marks;

        if (minusMarks > 0)
        {
            questionMarks = questionMarks - answerMarks;
            string updateQuestionMarksSql = @"UPDATE Question SET QuestionMarks = {1} WHERE Question_ID = {0} ";
            int resultQuestion = DBUtl.ExecSQL(updateQuestionMarksSql, questionid, questionMarks);

        }
        else if (minusMarks == 0)
        {
            string updateQuestionMarksSql = @"UPDATE Question SET QuestionMarks = {1} WHERE Question_ID = {0} ";
            int resultQuestion = DBUtl.ExecSQL(updateQuestionMarksSql, questionid, questionMarks);
        }
        else
        {
            questionMarks = questionMarks + model.Marks;
            string updateQuestionMarksSql = @"UPDATE Question SET QuestionMarks = {1} WHERE Question_ID = {0} ";
            int resultQuestion = DBUtl.ExecSQL(updateQuestionMarksSql, questionid, questionMarks);


        }
        string updateAnswersql = @"UPDATE Answer SET AnswerText = '{1}', Is_Correct = {2}, AnswerMarks = {3} WHERE Answer_ID = {0}";
        int result = DBUtl.ExecSQL(updateAnswersql, model.AnswerId, model.AnswerText, model.Is_Correct ? 1 : 0, model.Marks);


        if (result == 1)
        {
            TempData["Message"] = "Answer updated successfully";
            TempData["MsgType"] = "success";
        }
        else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }




        // Explicitly return RedirectToAction
        return RedirectToAction("NewManagement", new { quizCategory = category });
    }


    [HttpGet]
    public IActionResult UpdateQuiz(int id)
    {
        // Get the record from the database using the id
        string quizSql = @"SELECT Q.Question_ID, Quiz.Quiz_Category, Q.QuestionText, Q.QuestionType, Q.QuestionMarks, P.Photo_URL FROM Question Q INNER JOIN Quiz ON Q.Quiz_ID = Quiz.Quiz_ID INNER JOIN Photos P ON P.Quiz_ID = Quiz.Quiz_ID WHERE Q.Quiz_ID ={0}";
        List<QuizViewModelForManagement> lstQuiz = DBUtl.GetList<QuizViewModelForManagement>(quizSql, id);
        ViewData["Tissue_Info"] = GetListTissue();
        if (lstQuiz.Count == 1)
        {

            QuizViewModelForManagement quiz = lstQuiz[0];

            return View(quiz);
        }
        else
        {
            TempData["Message"] = "Quiz record does not exist";
            TempData["MsgType"] = "danger";
            return RedirectToAction("Management");
        }
    }
    [HttpPost]
    public IActionResult UpdateQuiz(QuizViewModelForManagement model)
    {

        string categorySql = @"SELECT Quiz_Category AS 'Quiz Category' FROM Quiz WHERE Quiz_ID = {0}";
        string category = Convert.ToString(DBUtl.GetValue(categorySql, model.Question_ID));
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                Console.WriteLine($"{state.Key} :: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            return View("UpdateQuiz", model);
        }

        string updateQuiz = @"UPDATE Quiz SET Quiz_Category = '{1}' WHERE Quiz_ID = {0}";
        int quizResult = DBUtl.ExecSQL(updateQuiz, model.Question_ID, model.Quiz_Category);
        if (quizResult == 1)
        {
            string queryPhotoUrl = @"SELECT Photo_URL FROM Question WHERE Question_ID = {0}";
            if (model.Photo_URL == "No Picture Inserted")
            {
                string updatePhoto = @"UPDATE Photos SET Photo_URL = '{1}'  WHERE Quiz_ID = {0}";
                int photoResult = DBUtl.ExecSQL(updatePhoto, model.Question_ID, model.Photo_URL);

            }
            else
            {
                string photoUrlSql = @"SELECT Photo_URL FROM Photos WHERE Quiz_ID = {0}";
                string photoUrl = Convert.ToString(DBUtl.GetValue(photoUrlSql, model.Question_ID));

                if (model.Photo_URL == null)
                {
                    TempData["Message"] = "Please delete the ones with the null url";
                    TempData["MsgType"] = "danger";
                }
                else
                {

                    if (model.Photo != null)
                    {
                        string fullpath = Path.Combine(_env.WebRootPath, "images", model.Quiz_Category, model.Photo_URL).Replace("\\", "/");
                        if (!System.IO.File.Exists(fullpath))
                        {
                            System.IO.File.Delete(fullpath);
                        }
                        string picfilename = DoPhotoUpload(model.Photo, model.Quiz_Category);
                        string updatePhoto = @"UPDATE Photos SET Photo_URL = '{1}'  WHERE Quiz_ID = {0}";
                        int photoResult = DBUtl.ExecSQL(updatePhoto, model.Question_ID, picfilename);
                    }


                }

            }
            string updateQuestion = @"UPDATE Question SET QuestionText = '{1}', QuestionType = '{2}' WHERE Question_ID = {0}";
            int questionResult = DBUtl.ExecSQL(updateQuestion, model.Question_ID, model.QuestionText, model.QuestionType);
            if (questionResult == 1)
            {
                TempData["Message"] = "Question Updated";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Message"] = DBUtl.DB_Message;
                TempData["MsgType"] = "danger";
            }

        }
        else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }


        return RedirectToAction("Management", new { quizCategory = category });
    }

    public IActionResult DeleteAnswer(int id)
    {
        string categorySql = @"SELECT Quiz_Category AS 'Quiz Category' FROM Quiz WHERE Quiz_ID = {0}";
        string category = Convert.ToString(DBUtl.GetValue(categorySql, id));
        string sql = @"SELECT * FROM Answer WHERE Answer_ID = {0}";
        DataTable ds = DBUtl.GetTable(sql, id);
        if (ds.Rows.Count != 1)
        {
            TempData["Message"] = "Answer Record does not exist";
            TempData["MsgType"] = "warning";
        }
        else
        {

            string questionidsql = @"SELECT Question_ID FROM Answer WHERE Answer_ID = {0}";
            int questionid = Convert.ToInt32(DBUtl.GetValue(questionidsql, id));
            string getQuestionMarksSql = @"SELECT QuestionMarks FROM Question WHERE Question_ID = {0}";
            double questionMarks = Convert.ToDouble(DBUtl.GetValue(getQuestionMarksSql, questionid));
            string answermarkssql = @"SELECT AnswerMarks FROM Answer WHERE Answer_ID = {0}";
            double answermarks = Convert.ToDouble(DBUtl.GetValue(answermarkssql, id));
            questionMarks = questionMarks - answermarks;
            string updateQuestionMarksSql = @"UPDATE Question SET QuestionMarks = {1} WHERE Question_ID = {0}";
            int result = DBUtl.ExecSQL(updateQuestionMarksSql, questionid, questionMarks);


            if (result == 1)
            {
                sql = @"DELETE FROM Answer WHERE Answer_ID = {0}";
                result = DBUtl.ExecSQL(sql, id);
                if (result == 1)
                {
                    TempData["Message"] = "Answer Deleted Successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = DBUtl.DB_Message;
                    TempData["ExecSQL"] = DBUtl.DB_SQL;
                    TempData["MsgType"] = "danger";
                }
            }
        }
        return RedirectToAction("Management", new { quizCategory = category });
    }

    public IActionResult DeleteQuiz(int id)
    {
        string categorySql = @"SELECT Quiz_Category AS 'Quiz Category' FROM Quiz WHERE Quiz_ID = {0}";
        string quizcategory = Convert.ToString(DBUtl.GetValue(categorySql, id));
        string sql = @"SELECT * FROM Quiz WHERE Quiz_ID = {0}";
        DataTable ds = DBUtl.GetTable(sql, id);
        if (ds.Rows.Count != 1)
        {
            TempData["Message"] = "Quiz Record does not exist";
            TempData["MsgType"] = "warning";
        }
        else
        {
            sql = @"SELECT * FROM Photos WHERE Quiz_ID = {0}";
            DataTable table = DBUtl.GetTable(sql, id);
            if (table.Rows[0]["Photo_URL"].ToString()! != "No Picture Inserted")
            {
                string category = ds.Rows[0]["Quiz_Category"].ToString()!;
                string photoFile = table.Rows[0]["Photo_URL"].ToString()!;
                string fullpath = Path.Combine(_env.WebRootPath, "images", category, photoFile).Replace("\\", "/");
                System.IO.File.Delete(fullpath);

                sql = @"DELETE FROM Quiz WHERE Quiz_ID = {0}";
                int result = DBUtl.ExecSQL(sql, id);
                if (result == 1)
                {
                    TempData["Message"] = "Quiz Record Deleted Successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = DBUtl.DB_Message;
                    TempData["ExecSQL"] = DBUtl.DB_SQL;
                    TempData["MsgType"] = "danger";
                }
            }
            else
            {
                sql = @"DELETE FROM Quiz WHERE Quiz_ID = {0}";
                int result = DBUtl.ExecSQL(sql, id);
                if (result == 1)
                {
                    TempData["Message"] = "Quiz Record Deleted Successfully";
                    TempData["MsgType"] = "success";
                }
                else
                {
                    TempData["Message"] = DBUtl.DB_Message;
                    TempData["ExecSQL"] = DBUtl.DB_SQL;
                    TempData["MsgType"] = "danger";
                }

            }



        }
        return RedirectToAction("Management", new { quizCategory = quizcategory });
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
        int userid = 1;

        // Fetch responses for the quiz
        string summarySql = @"
    SELECT q.Question_ID,q.QuestionText, a.AnswerText, CAST(r.Is_Correct AS BIT) AS'IsCorrect', Score
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

        List<HistoQuiz> quizlist = DBUtl.GetList<HistoQuiz>("SELECT Quiz.Quiz_Category AS 'QuizCategory',  SUM(Q.QuestionMarks) AS 'TotalQuestionMarks' FROM Quiz INNER JOIN Question Q ON Quiz.Quiz_ID = Q.Quiz_ID  GROUP BY Quiz.Quiz_Category");

        double totalQuestionMarks = 0;

        for (int i = 0; i < quizlist.Count; i++)
        {
            if (quizlist[i].QuizCategory == quizCategory)
            {
                totalQuestionMarks = quizlist[i].TotalQuestionMarks;
                break;
            }
        }
        ViewBag.TotalQuestionMarks = totalQuestionMarks;

        string checkAttemptsSql = @"SELECT COUNT(*) FROM Quiz_Statistics WHERE User_Id = {0} and Quiz_Category = '{1}'";
        int checkAttempts = Convert.ToInt32(DBUtl.GetValue(checkAttemptsSql, userid, quizCategory));

        checkAttempts = checkAttempts + 1;
        string insertSql = @"
        INSERT INTO Quiz_Statistics (User_ID, Quiz_Category, Date_Attempted, Score)
        VALUES ({0}, '{1}', GETDATE(), {2})";
        int result = DBUtl.ExecSQL(insertSql, userid, quizCategory, totalScore);

        string correctAnswerSql = @"SELECT 
                                        Quiz.Quiz_Category AS 'QuizCategory', 
                                        Q.Question_Id AS 'QuestionId',
                                        STRING_AGG(A.AnswerText, ',') AS 'GroupedAnswerText', 
                                        MAX(CAST(A.Is_Correct AS INT)) AS 'IsCorrect'
                                    FROM 
                                        Question Q 
                                    INNER JOIN 
                                        Quiz Quiz ON Quiz.Quiz_ID = Q.Quiz_ID 
                                    INNER JOIN 
                                        Answer A ON A.Question_ID = Q.Question_ID 
                                    WHERE 
                                        A.Is_Correct = 1 
                                        AND Quiz.Quiz_Category = '{0}'
                                    GROUP BY 
                                        Quiz.Quiz_Category, 
                                        Q.Question_Id";
        var correctAnswer = DBUtl.GetList<CorrectAnswers>(correctAnswerSql, quizCategory);
        var model = new QuizSummaryViewModel
        {
            QuizSummaryResponse = responses,
            CorrectAnswewrs = correctAnswer
        };


        return View(model);
    }
    public IActionResult Quit()
    {
        string deleteResponsesql = @"DELETE FROM Quiz_Responses";
        int resultResponse = DBUtl.ExecSQL(deleteResponsesql);
        string deleteSavedQuestionId = @"DELETE FROM UserAnsweredQuestions";
        int resultDeleteSaved = DBUtl.ExecSQL(deleteSavedQuestionId);

        return RedirectToAction("HistoQuiz");
    }

    private string DoPhotoUpload(IFormFile photo, string category)
    {
        try
        {
            if (photo == null)
            {
                return null; // Return null or a default value if no photo is provided
            }
            else
            {


                // Construct the subdirectory path based on the highest prediction result
                string subdirectory = Path.Combine(_env.WebRootPath, "images", category);
                subdirectory = subdirectory.Replace("\\", "/"); // Normalize path

                if (!Directory.Exists(subdirectory))
                {
                    Directory.CreateDirectory(subdirectory);
                }

                Console.WriteLine("Subdirectory: " + subdirectory);

                // Construct the final path for the uploaded image
                string fext = Path.GetExtension(photo.FileName);
                string uname = Guid.NewGuid().ToString();
                string fname = uname + fext;
                string fullpath = Path.Combine(subdirectory, fname).Replace("\\", "/"); // Normalize path

                // Save the image
                using (FileStream fs = new FileStream(fullpath, FileMode.Create))
                {
                    photo.CopyTo(fs);
                }

                return fname;
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static SelectList GetListTissue()
    {
        //string tissueSql = @"SELECT LTRIM(CONVERT(Tissue_ID, CHAR)) as Value, Tissue_Name as Text FROM Tissue_Info;";
        string tissueSql = @"SELECT LTRIM(Tissue_ID) as 'Value', Tissue_Name as 'Text' FROM Tissue_Info";
        List<SelectListItem> lstTissue = DBUtl.GetList<SelectListItem>(tissueSql);
        return new SelectList(lstTissue, "Value", "Text");
    }
}