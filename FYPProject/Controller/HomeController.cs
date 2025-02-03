using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using FYPProject.Controllers;
using System.Threading.Tasks;
using FYPProject.Models;

namespace FYPProject.Controllers
{
    [ApiController]
    [Route("api/Histology")]
    public class HomeController : Controller
    {

        private readonly ApplicationDBContext _context;

        public HomeController(ApplicationDBContext context)
        {
            _context = context;
        }
        public IActionResult Home()
        {
            ViewBag.HideNavbar = false;
            ViewBag.ActivePage = "Home";
            return View();
        }

        // API Endpoint for fetching thumbnails
        [HttpGet("Thumbnail")]
        public async Task<IActionResult> GetThumbnail(string photoDescription)
        {

            if (string.IsNullOrEmpty(photoDescription))
            {
                return BadRequest(new { message = "Photo description is required." });
            }

            try
            {
                // Use raw SQL query to fetch the image details
                var query = @"
                    SELECT Photo_ID, Photo_Description, Photo_URL, Tissue_ID, Question_ID
                    FROM Photos
                    WHERE LOWER(Photo_Description) = LOWER(@photoDescription)";

                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;

                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@photoDescription";
                        parameter.Value = photoDescription;
                        command.Parameters.Add(parameter);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                return NotFound(new { message = $"No photo found for {photoDescription}" });
                            }

                            while (await reader.ReadAsync())
                            {
                                return Ok(new
                                {
                                    Photo_ID = reader.GetInt32(0),
                                    Photo_Description = reader.GetString(1),
                                    Photo_URL = reader.GetString(2),
                                    Tissue_ID = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                    Question_ID = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)
                                });
                            }
                        }
                    }
                }

                return NotFound(new { message = $"No photo found for {photoDescription}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetThumbnail: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while retrieving the photo.", error = ex.Message });
            }
        }
    }
}