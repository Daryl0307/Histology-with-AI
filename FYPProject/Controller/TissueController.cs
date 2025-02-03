using Microsoft.AspNetCore.Mvc;
namespace FYPProject.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using FYPProject.Models;
using System;
using System.Linq;
using System.Data;

using RP.SOI.DotNet.Utils;
using System.Collections.Generic;


public class TissueController : Controller
{
    private readonly IWebHostEnvironment _env;

    public TissueController(IWebHostEnvironment environment)
    {

        _env = environment;
    }

    public IActionResult TissueView(int id)
    {
        ViewBag.HideNavbar = false;

        // SQL query to retrieve tissue and related photo data
        string getTissue = @"
        SELECT 
            TI.Tissue_ID AS 'TissueId', 
            Tissue_Name AS 'TissueName', 
            Tissue_Description AS 'TissueDescription', 
            P.Photo_ID AS 'PhotoId', 
            P.Photo_URL AS 'PhotoURL'
        FROM 
            Tissue_Info TI
        LEFT JOIN 
            Photos P ON P.Tissue_ID = TI.Tissue_ID
        WHERE 
            TI.Tissue_ID = {0};";

        // Get data from database
        var data = DBUtl.GetTable(getTissue, id);

        // If no data is found, show a message
        if (data.Rows.Count == 0)
        {
            TempData["Message"] = "No tissue found. Please check the tissue ID.";
            TempData["MsgType"] = "danger";
            return View();
        }

        // Group data if needed (though in this case we are fetching a single tissue by ID)
        var tissueViewModels = data.AsEnumerable()
        .Select(row => new TissueViewModel
        {
            TissueId = Convert.ToInt32(row["TissueId"]),
            TissueName = row["TissueName"].ToString(),
            TissueDescription = row["TissueDescription"].ToString(),
            PhotoURL = row["PhotoURL"].ToString()
        })
        .ToList(); // This returns a list of TissueViewModels

            return View(tissueViewModels);

        }


    public IActionResult Histopedia()
    {
        ViewBag.HideNavbar = false;
        ViewBag.ActivePage = "Histopedia";

        List<TissueViewModel> list = DBUtl.GetList<TissueViewModel>(
            "WITH RankedPhotos AS (SELECT TI.Tissue_ID, TI.Tissue_Name, TI.Tissue_Description,  P.Photo_URL,  ROW_NUMBER() OVER (PARTITION BY TI.Tissue_Name ORDER BY P.Photo_URL) AS RowNum    FROM Tissue_Info TI  INNER JOIN Photos P ON P.Tissue_ID = TI.Tissue_ID)SELECT Tissue_ID AS 'TissueId', Tissue_Name AS 'TissueName', Tissue_Description AS 'TissueDescription', Photo_URL AS 'PhotoURL' FROM RankedPhotos WHERE RowNum = 1;"
        );

        if (list == null || list.Count == 0)
        {
            TempData["Message"] = "No data found. Please check your query.";
            TempData["MsgType"] = "danger";
            return View(new List<TissueInfo>()); // Return empty list instead of null
        }

        

        return View(list); // Pass the list directly to the view
    }

    public IActionResult ManageLesson()
    {
        ViewBag.HideNavbar = false;
        ViewBag.ActivePage = "ManageLesson";

        List<TissueInfo> tissueList = DBUtl.GetList<TissueInfo>("SELECT DISTINCT TI.Tissue_ID AS 'TissueId', Tissue_Name AS 'TissueName', Tissue_Description AS 'TissueDescription'FROM Tissue_Info TI INNER JOIN Photos P ON P.Tissue_ID = TI.Tissue_ID");
        int noofLesson = tissueList.Count;
        ViewBag.noofLesson = noofLesson;
        ViewData["Tissue_Info"] = GetListTissue();

        return View(tissueList);
    }
    [HttpGet]
    public IActionResult UpdateTissue(int id)
    {
        ViewBag.HideNavbar = false;

        string getTissue = @"SELECT 
                            TI.Tissue_ID AS 'TissueId', 
                            Tissue_Name AS 'TissueName', 
                            Tissue_Description AS 'TissueDescription', 
                            P.Photo_ID AS 'PhotoId', 
                            P.Photo_URL AS 'PhotoURL'
                        FROM 
                            Tissue_Info TI
                        LEFT JOIN 
                            Photos P ON P.Tissue_ID = TI.Tissue_ID
                        WHERE 
                            TI.Tissue_ID = {0};";

        List<TissueUpdateModel> tissueList = DBUtl.GetList<TissueUpdateModel>(getTissue, id);
        ViewData["Tissue_Info"] = GetListTissue();

        if (tissueList.Count >= 1)
        {
            TissueUpdateModel tissue = tissueList[0];

            // Retrieve all photos related to the tissue ID
            string getPhotos = @"SELECT Photo_ID AS 'PhotoId', Photo_URL AS 'PhotoURL'
                             FROM Photos WHERE Tissue_ID = {0};";
            List<Photos> photoList = DBUtl.GetList<Photos>(getPhotos, id);
            tissue.Photos = photoList;
            if (photoList != null && photoList.Any())
            {
                tissue.Photos = photoList;
            }
            else
            {
                // Optionally log or check why photos are not retrieved
                TempData["Message"] = "No photos found for this tissue.";
                TempData["MsgType"] = "warning";
                RedirectToAction("ManageLesson");
            }

            return View(tissue);
        }
        else
        {
            TempData["Message"] = "Tissue record does not exist";
            TempData["MsgType"] = "danger";
            return RedirectToAction("ManageLesson");
        }
    }

    [HttpPost]
    public IActionResult UpdateTissue(TissueUpdateModel model)
    {
        ViewBag.HideNavbar = false;

        if (!ModelState.IsValid)
        {
            // Log the validation errors
            foreach (var state in ModelState)
            {
                Console.WriteLine($"{state.Key} :: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            ViewData["Tissue_Info"] = GetListTissue();
            return View("UpdateTissue", model);
        }

        // Get Quiz ID based on category
        string getQuizIdsQuery = "SELECT Quiz_ID FROM Quiz WHERE Quiz_Category = '{0}'";
        List<int> quizIds = DBUtl.GetList<int>(getQuizIdsQuery, model.TissueName);
        int quizId = quizIds.Any() ? quizIds.First() : 0;

        // Ensure the quiz exists, otherwise insert a new one
        if (quizId == 0)
        {
            string insertQuizCategory = "INSERT INTO Quiz(Quiz_Category) OUTPUT INSERTED.Quiz_ID VALUES('{0}')";
            quizId = DBUtl.ExecSQLReturnId(insertQuizCategory, model.TissueName);
        }

        // Update the tissue information
        string updateTissueQuery = "UPDATE Tissue_Info SET Tissue_Name = '{0}', Tissue_Description = '{1}' WHERE Tissue_ID = {2}";
        int updateResult = DBUtl.ExecSQL(updateTissueQuery, model.TissueName, model.TissueDescription, model.TissueId);

        if (updateResult > 0)
        {
            List<int> photoIds = new List<int>();

            if (model.PhotoFiles != null && model.PhotoFiles.Any())
            {
                foreach (var photo in model.PhotoFiles)
                {
                    string photoFileName = DoPhotoUpload(photo, model.TissueName);
                    string insertPhotoQuery = "INSERT INTO Photos (Photo_URL, Quiz_ID) OUTPUT INSERTED.Photo_ID VALUES ('{0}', {1})";
                    int insertedPhotoId = DBUtl.ExecSQLReturnId(insertPhotoQuery, photoFileName, quizId);
                    if (insertedPhotoId > 0)
                    {
                        photoIds.Add(insertedPhotoId);
                    }
                }
            }

            // Update existing photos with the new Tissue_ID
            if (photoIds.Count > 0)
            {
                foreach (int pId in photoIds)
                {
                    string updatePhotos = "UPDATE Photos SET Tissue_ID = {1} WHERE Photo_ID = {0}";
                    DBUtl.ExecSQL(updatePhotos, pId, model.TissueId);
                }
            }

            TempData["Message"] = "Tissue updated successfully.";
            TempData["MsgType"] = "success";
        }
        else
        {
            TempData["Message"] = "Update failed. Please try again.";
            TempData["MsgType"] = "danger";
        }

        return RedirectToAction("ManageLesson");
    }


    [HttpGet]
    public IActionResult AddTissue()
    {
        ViewBag.HideNavbar = false;

        ViewData["Tissue_Info"] = GetListTissue();

        return View();
    }

    [HttpPost]
    public IActionResult AddTissue(TissueInfo model)
    {
        ViewBag.HideNavbar = false;

        ModelState.Remove("Photos");
        ModelState.Remove("PhotoFiles");
        if (!ModelState.IsValid)
        {
            // Log the validation errors
            foreach (var state in ModelState)
            {
                Console.WriteLine($"{state.Key} :: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            TempData["Message"] = "Please correct the errors.";
            TempData["MsgType"] = "danger";
            ViewData["Tissue_Info"] = GetListTissue();
            return View("AddTissue", model);
        }
        int photoId = 0;
        int quizId = 0;

        string getQuizIdsQuery = @"SELECT Quiz_ID FROM Quiz WHERE Quiz_Category = '{0}'";
        List<int> quizIds = DBUtl.GetList<int>(getQuizIdsQuery, model.TissueName);
        string checkQuestionQuery = @"SELECT COUNT(*) FROM Question WHERE Quiz_ID IN ({0})";
        string quizIdsJoined = string.Join(",", quizIds);

        int questionCount = Convert.ToInt32(DBUtl.GetValue(checkQuestionQuery, quizIdsJoined));

        if (questionCount > 0) 
        {
            quizId = quizIds.First();
        } 
        else
        {
            string insertQuizCategory = @"INSERT INTO Quiz(Quiz_Category) OUTPUT INSERTED.Quiz_ID VALUES('{0}')";
            quizId = DBUtl.ExecSQLReturnId(insertQuizCategory, model.TissueName);
        }


        List<int> photoIds = new List<int>();

        if (model.PhotoFiles != null && model.PhotoFiles.Any())
        {
            foreach (var photo in model.PhotoFiles)
            {
                string photoFileName = DoPhotoUpload(photo, model.TissueName);
                string insertPhotoQuery = @"INSERT INTO Photos (Photo_URL, Quiz_ID) OUTPUT INSERTED.Photo_ID VALUES ('{0}', {1})";
                int insertedPhotoId = DBUtl.ExecSQLReturnId(insertPhotoQuery, photoFileName, quizId);

                if (insertedPhotoId > 0)
                {
                    photoIds.Add(insertedPhotoId); // Store each inserted Photo_ID
                }
            }
        }
        else
        {
            // Handle no photo upload
            string picfilename = "No Picture Inserted";
            string insertPhoto = @"INSERT INTO Photos (Photo_URL, Quiz_ID) OUTPUT INSERTED.Photo_ID VALUES ('{0}', {1})";
            photoId = DBUtl.ExecSQLReturnId(insertPhoto, picfilename, quizId);
            if (photoId > 0)
            {
                photoIds.Add(photoId);
            }
        }

        // After inserting Tissue_Info, update all photos with Tissue_ID
        if (photoIds.Count > 0)
        {
            string addTissueQuery = @"INSERT INTO Tissue_Info(Tissue_Name, Tissue_Description, Photo_ID) OUTPUT INSERTED.Tissue_ID VALUES ('{0}', '{1}', {2})";
            int tissueId = DBUtl.ExecSQLReturnId(addTissueQuery, model.TissueName, model.TissueDescription, photoIds.First());

            if (tissueId > 0)
            {
                foreach (int pId in photoIds)
                {
                    string updatePhotos = @"UPDATE Photos SET Tissue_ID = {1} WHERE Photo_ID = {0}";
                    int result = DBUtl.ExecSQL(updatePhotos, pId, tissueId);

                    if (result > 0)
                    {
                        TempData["Message"] = "Tissue added successfully.";
                        TempData["MsgType"] = "success";
                    }
                    else
                    {
                        TempData["Message"] = DBUtl.DB_Message;
                        TempData["MsgType"] = "danger";
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
            TempData["Message"] = "Photo upload failed.";
            TempData["MsgType"] = "danger";
        }

        return RedirectToAction("ManageLesson");
        // Redirect after successful submission
    }

    public IActionResult DeleteTissue(int id)
    {
        string getPhotosSql = @"SELECT Photo_Id AS 'PhotoId' FROM Photos WHERE Tissue_ID = {0}";
        List<Photos> photos = DBUtl.GetList<Photos>(getPhotosSql, id);

        if(photos.Count == 0)
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";

            return RedirectToAction("ManageLesson");
        }
        int result1 = 0;
        string getPhotoUrlSql = @"SELECT Photo_URL FROM Photos WHERE Photo_ID = {0}";
        string getCategorySql = @"SELECT Tissue_Name FROM Photos P INNER JOIN Tissue_Info TI ON TI.Photo_ID = P.Photo_ID WHERE P.Photo_ID = {0}";
        
        for (int i = 0; i < photos.Count(); i++)
        {
            string photoUrl = Convert.ToString(DBUtl.GetValue(getPhotoUrlSql, photos[i].Photo_ID));
            string category = Convert.ToString(DBUtl.GetValue(getCategorySql, photos[i].Photo_ID));

            
            if(photoUrl != "No Picture Inserted")
            {
                string fullpath = Path.Combine(_env.WebRootPath, "images", category, photoUrl).Replace("\\", "/");
                System.IO.File.Delete(fullpath);
                result1 =result1 + 1;

            }

        }

        if (result1 > 0) {
            string deleteTissueSql = @"DELETE FROM Tissue_Info WHERE Tissue_ID = {0}";
            int result = DBUtl.ExecSQL(deleteTissueSql, id);
            if (result == 1)
            {
                TempData["Message"] = "Tissue deleted successfully.";
                TempData["MsgType"] = "success";
            }
            else
            {
                TempData["Message"] = DBUtl.DB_Message;
                TempData["MsgType"] = "danger";
            }
        } else
        {
            TempData["Message"] = DBUtl.DB_Message;
            TempData["MsgType"] = "danger";
        }

       
        return RedirectToAction("ManageLesson");
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
        //string tissueSql = @"SELECT LTRIM(CONVERT(Tissue_ID, CHAR)) as `Value`, Tissue_Name as `Text` FROM Tissue_Info;";
        string tissueSql = @"SELECT DISTINCT Quiz_Category as 'Value', Quiz_Category as 'Text' FROM Quiz";
        List<SelectListItem> lstTissue = DBUtl.GetList<SelectListItem>(tissueSql);
        return new SelectList(lstTissue, "Value", "Text");
    }
}
