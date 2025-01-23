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
    private static SelectList GetListTissue()
    {
        //string tissueSql = @"SELECT LTRIM(CONVERT(Tissue_ID, CHAR)) as Value, Tissue_Name as Text FROM Tissue_Info;";
        string tissueSql = @"SELECT LTRIM(Tissue_ID) as 'Value', Tissue_Name as 'Text' FROM Tissue_Info";
        List<SelectListItem> lstTissue = DBUtl.GetList<SelectListItem>(tissueSql);
        return new SelectList(lstTissue, "Value", "Text");
    }
}