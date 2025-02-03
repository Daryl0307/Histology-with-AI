using FYPProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FYPProject.Controllers
{
    public class ControlPanelController : Controller
    {
        private readonly string _connectionString;


        public ControlPanelController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public IActionResult ControlPanel()

        {
            ViewBag.HideNavbar = false;
            ViewBag.ActivePage = "ControlPanel";
            return View("ControlPanel");
        }


        [HttpGet]
        public IActionResult GetUserInfo()
        {
            try
            {
                List<dynamic> userInfoList = new List<dynamic>();
                string query = "SELECT User_ID, Username, Role_Status, Email FROM User_Info";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userInfoList.Add(new
                                {
                                    userId = Convert.ToInt32(reader["User_ID"]),
                                    Username = reader["Username"].ToString(),
                                    AdminAccess = Convert.ToBoolean(reader["Role_Status"]) ? "✔" : "",
                                    Email = reader["Email"].ToString()
                                });
                            }
                        }
                    }
                }

                return Json(userInfoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult DeleteUser([FromBody] DeleteUserPayload payload)
        {
            try
            {

                int userId = payload.UserId;

                if (userId == 0)
                {
                    return BadRequest(new { message = "Invalid User ID." });
                }


                string query = "DELETE FROM User_Info WHERE User_ID = @UserID";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = $"User with ID {userId} deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return StatusCode(500, new { message = "Failed to delete user." });
            }
        }


        [HttpGet]
        public IActionResult GetUserDetails(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid User ID provided." });
                }

                string query = "SELECT User_ID, Username, Role_Status, Email FROM User_Info WHERE User_ID = @UserID";
                dynamic userDetails = null;

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userDetails = new
                                {
                                    userId = Convert.ToInt32(reader["User_ID"]),
                                    username = reader["Username"].ToString(),
                                    adminAccess = Convert.ToInt32(reader["Role_Status"]) == 1 ? "yes" : "no",
                                    email = reader["Email"].ToString()
                                };
                            }
                        }
                    }
                }

                if (userDetails != null)
                {
                    return Json(userDetails);
                }
                else
                {
                    return NotFound(new { message = "User not found." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user details: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while fetching user details." });
            }
        }



        public IActionResult ControlPanelEdit(int userId)
        {
            ViewBag.HideNavbar = false;
            ViewBag.UserId = userId;
            return View("ControlPanelEdit");
        }

        [HttpPost]
        public IActionResult ResetPassword(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid User ID provided." });
                }


                string query = "UPDATE User_Info SET Password = @DefaultPassword WHERE User_ID = @UserID";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DefaultPassword", "Password123");
                        cmd.Parameters.AddWithValue("@UserID", userId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Ok(new { message = "Password reset successfully." });
                        }
                        else
                        {
                            return NotFound(new { message = "User not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting password: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while resetting the password." });
            }
        }

        [HttpPost]
        public IActionResult UpdateUser([FromBody] UpdateUserPayload payload)
        {
            try
            {
                if (payload.UserId <= 0)
                {
                    return BadRequest(new { message = "Invalid User ID provided." });
                }

                string query = "UPDATE User_Info SET Username = @Username, Role_Status = @RoleStatus, Email = @Email WHERE User_ID = @UserId";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", payload.Username);
                        cmd.Parameters.AddWithValue("@RoleStatus", payload.RoleStatus == "yes" ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Email", payload.Email);
                        cmd.Parameters.AddWithValue("@UserId", payload.UserId);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Ok(new { message = "User updated successfully." });
                        }
                        else
                        {
                            return NotFound(new { message = "User not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while updating the user." });
            }
        }

    }

    public class DeleteUserPayload
    {
        public int UserId { get; set; }
    }
    public class UpdateUserPayload
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string RoleStatus { get; set; }
        public string Email { get; set; }
    }
}