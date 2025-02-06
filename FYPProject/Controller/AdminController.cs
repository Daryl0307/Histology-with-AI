using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using FYPProject.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using RP.SOI.DotNet.Utils;
using FYPProject.Attributes;

namespace FYPProject.Controllers
{

    [Route("Histology")]

    public class AdminController : Controller
    {
        protected IActionResult JsonResponse(bool success, string message)
       => new JsonResult(new { success, message });

        private readonly ApplicationDBContext _context;
        private const int MaxContentCards = 8;

        public AdminController(ApplicationDBContext context)
        {
            _context = context;
        }

        public class UpdatePhotoDescriptionDto
        {
            public int Id { get; set; }
            public string Description { get; set; }
        }

        [HttpGet("/Histology/Home")]
        public async Task<IActionResult> Home()
        {
            ViewBag.HideNavbar = false;

            string sql = "SELECT * FROM HomeContent";
            var photos = await _context.HomeContent.FromSqlRaw(sql).ToListAsync();

            return View("/Views/Histology/Home.cshtml", photos);
        }

        // Get photo
        [HttpGet("GetPhotos")]
        public async Task<IActionResult> GetPhotos()
        {
            try
            {
                string sql = @"
        SELECT Id, Description, Url, CAST(NULL AS VARBINARY(MAX)) AS Photo_Data
        FROM HomeContent";

                var photos = await _context.HomeContent.FromSqlRaw(sql).ToListAsync();

                Console.WriteLine($"🔹 Photos Retrieved: {photos.Count}");

                return View("/Views/Histology/Home.cshtml", photos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error retrieving photos: {ex.Message}");
            }
        }

        [HttpGet("/Histology/UpdatePhoto")]

        public async Task<IActionResult> UpdatePhoto(int id)
        {
            ViewBag.HideNavbar = false;
            Console.WriteLine($"🔍 Debug: Received UpdatePhoto request for ID {id}");

            if (id == 0)
            {
                Console.WriteLine("❌ Invalid ID (0)");
                return BadRequest("Invalid photo ID.");
            }

            var photo = await _context.HomeContent.FirstOrDefaultAsync(p => p.Id == id);

            if (photo == null)
            {
                Console.WriteLine("❌ No photo found in database!");
                return NotFound("Photo not found.");
            }

            return View("/Views/Histology/UpdatePhoto.cshtml", photo);
        }


        [HttpPost("UpdatePhoto")]
        [AdminOnly]
        public async Task<IActionResult> UpdatePhoto(int Id, IFormFile file, string description)
        {
            if (HttpContext.Session.GetString("UserRole") != "1")
                return Unauthorized();

            if (string.IsNullOrEmpty(description))
                return BadRequest("Description is required.");

            try
            {
                string filePath = null;
                byte[] fileData = null;

                if (file != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        fileData = memoryStream.ToArray();
                    }

                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    filePath = Path.Combine("wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    string sql = @"
                UPDATE HomeContent 
                SET Description = @Description, Url = @FilePath, Photo_Data = @FileData 
                WHERE Id = @Id";

                    await _context.Database.ExecuteSqlRawAsync(sql,
                        new Microsoft.Data.SqlClient.SqlParameter("@Description", description),
                        new Microsoft.Data.SqlClient.SqlParameter("@FilePath", "/images/" + fileName),
                        new Microsoft.Data.SqlClient.SqlParameter("@FileData", fileData),
                        new Microsoft.Data.SqlClient.SqlParameter("@Id", Id));
                }
                else
                {
                    string sql = @"
                UPDATE HomeContent 
                SET Description = @Description 
                WHERE Id = @Id";

                    await _context.Database.ExecuteSqlRawAsync(sql,
                        new Microsoft.Data.SqlClient.SqlParameter("@Description", description),
                        new Microsoft.Data.SqlClient.SqlParameter("@Id", Id));
                }

                return RedirectToAction("Home", "Histology");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating photo: {ex.Message}");
            }
        }

        [HttpGet("/Histology/DeletePhoto")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            ViewBag.HideNavbar = false;
            Console.WriteLine($"🔍 Debug: Received DeletePhoto request for ID {id}");

            if (id == 0)
            {
                Console.WriteLine("❌ Invalid ID (0)");
                return BadRequest("Invalid photo ID.");
            }

            var photo = await _context.HomeContent.FirstOrDefaultAsync(p => p.Id == id);

            if (photo == null)
            {
                Console.WriteLine("❌ No photo found in database!");
                return NotFound("Photo not found.");
            }

            return View("/Views/Histology/DeletePhoto.cshtml", photo);
        }





        [HttpPost("ConfirmDelete")]
        [AdminOnly]
        public async Task<IActionResult> ConfirmDelete(int Id)
        {
            if (HttpContext.Session.GetString("UserRole") != "1")
                return Unauthorized();

            try
            {
                string query = "SELECT Url FROM HomeContent WHERE Id = @Id";
                var photoUrl = await _context.HomeContent
                                             .FromSqlRaw(query, new Microsoft.Data.SqlClient.SqlParameter("@Id", Id))
                                             .Select(p => p.Url)
                                             .FirstOrDefaultAsync();

                if (photoUrl == null)
                    return NotFound();

                string filePath = Path.Combine("wwwroot", photoUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                string sql = "DELETE FROM HomeContent WHERE Id = @Id";
                await _context.Database.ExecuteSqlRawAsync(sql, new Microsoft.Data.SqlClient.SqlParameter("@Id", Id));

                return RedirectToAction("Home", "Histology");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting photo: {ex.Message}");
            }
        }


        [HttpGet("/Histology/AddPhoto")]
        public IActionResult AddPhoto()
        {
            ViewBag.HideNavbar = false;
            return View("/Views/Histology/AddPhoto.cshtml");
        }
        [AdminOnly]
        [HttpPost("AddPhoto")]
        public async Task<IActionResult> AddPhoto(IFormFile file, string description)
        {
            if (HttpContext.Session.GetString("UserRole") != "1")
                return Unauthorized();

            if (file == null || string.IsNullOrEmpty(description))
                return BadRequest("File and description are required.");

            try
            {
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine("wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string sql = @"
            INSERT INTO HomeContent (Description, Url, Photo_Data) 
            VALUES (@Description, @FilePath, @FileData)";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@Description", description),
                    new Microsoft.Data.SqlClient.SqlParameter("@FilePath", "/images/" + fileName),
                    new Microsoft.Data.SqlClient.SqlParameter("@FileData", fileData));

                return RedirectToAction("Home", "Histology");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving photo: {ex.Message}");
            }
        }
    }
}
