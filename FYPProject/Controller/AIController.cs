using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using FYPProject.Models;
using FYPProject.Controllers;
using System.Text.Json;

namespace FYPProject.Controllers
{
    public class AIController : Controller
    {

        private readonly ApplicationDBContext _context;

        // Constructor to inject the database context
        public AIController(ApplicationDBContext context)
        {
            _context = context;
        }

        public IActionResult Histoscanner()
        {
            ViewBag.HideNavbar = false;
            ViewBag.ActivePage = "Histoscanner";
            return View();
        }


        [HttpGet]
        [Route("api/Histology/GetPhotoUrl")]
        public async Task<IActionResult> GetPhotoUrl(string photoDescription)
        {
            if (string.IsNullOrEmpty(photoDescription))
            {
                return BadRequest(new { message = "Photo description is required." });
            }

            try
            {
                // Use raw SQL to ensure exact matching with the database
                var query = @"SELECT Photo_ID, Photo_Description, Photo_URL, Tissue_ID, Question_ID
                      FROM Photos
                      WHERE LOWER(Photo_Description) = LOWER(@photoDescription)";

                using (var connection = _context.Database.GetDbConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@photoDescription";
                        parameter.Value = photoDescription; // Use parameter to prevent SQL injection
                        command.Parameters.Add(parameter);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                return NotFound(new { message = $"No photo found for {photoDescription}" });
                            }

                            var photos = new List<object>();
                            while (await reader.ReadAsync())
                            {
                                photos.Add(new
                                {
                                    Photo_ID = reader.GetInt32(0),
                                    Photo_Description = reader.GetString(1),
                                    Photo_URL = reader.GetString(2),
                                    Tissue_ID = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                    Question_ID = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)
                                });
                            }

                            return Ok(photos.FirstOrDefault());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPhotoUrl: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving the photo.", error = ex.Message });
            }
        }




    }
}



