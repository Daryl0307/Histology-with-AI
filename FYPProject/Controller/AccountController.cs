using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Text;
using System.Security.Cryptography;
using System.Net.Mail;
using System.Net;
using System.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using RP.SOI.DotNet.Utils;
using System.Security.Claims;
using Newtonsoft.Json;
using FYPProject.Models;


namespace FYPProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new ArgumentNullException("DefaultConnection", "Connection string is missing from configuration.");
        }

        public IActionResult Login(string usernameOrEmail = null, string password = null)
        {
            ViewBag.HideNavbar = true;
            ViewBag.IsLoggedIn = false;
            ViewBag.ErrorMessage = "";

            string imageUrl = "/images/logo.png";
            try
            {
                string query = "SELECT TOP 1 Photo_URL FROM Photos WHERE Photo_Description = 'Logo'";
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            imageUrl = result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching logo image: {ex.Message}");
            }

            ViewBag.ImageUrl = imageUrl;

            if (!string.IsNullOrEmpty(usernameOrEmail) && !string.IsNullOrEmpty(password))
            {
                try
                {
                    // Hash the password using SHA256 from DBUtl
                    string hashedPassword = DBUtl.HashPassword(password);

                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        // Get User_Id
                        string userIdQuery = @"
                SELECT User_Id 
                FROM User_Info 
                WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail)";

                        using (SqlCommand cmd = new SqlCommand(userIdQuery, conn))
                        {
                            cmd.Parameters.Add("@UsernameOrEmail", SqlDbType.NVarChar).Value = usernameOrEmail;

                            object userIdObj = cmd.ExecuteScalar();
                            if (userIdObj == null)
                            {
                                ViewBag.ErrorMessage = "Wrong credentials. Please try again. ";
                                return View("Login");
                            }

                            HttpContext.Session.SetString("UserId", userIdObj.ToString());
                        }

                        // Get Role_Status
                        string roleQuery = @"
                SELECT Role_Status 
                FROM User_Info 
                WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail) 
                AND Password = @Password";

                        using (SqlCommand cmd = new SqlCommand(roleQuery, conn))
                        {
                            cmd.Parameters.Add("@UsernameOrEmail", SqlDbType.NVarChar).Value = usernameOrEmail;
                            cmd.Parameters.Add("@Password", SqlDbType.NVarChar).Value = hashedPassword;

                            object roleStatusObj = cmd.ExecuteScalar();
                            if (roleStatusObj == null)
                            {
                                ViewBag.ErrorMessage = "Wrong credentials. Please try again. ";
                                return View("Login");
                            }

                            int userRole = Convert.ToInt32(roleStatusObj);
                            HttpContext.Session.SetString("UserRole", userRole.ToString());
                        }

                        // Get Username
                        string usernameQuery = @"
                SELECT Username 
                FROM User_Info 
                WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail) 
                AND Password = @Password";

                        using (SqlCommand cmd = new SqlCommand(usernameQuery, conn))
                        {
                            cmd.Parameters.Add("@UsernameOrEmail", SqlDbType.NVarChar).Value = usernameOrEmail;
                            cmd.Parameters.Add("@Password", SqlDbType.NVarChar).Value = hashedPassword;

                            object usernameObj = cmd.ExecuteScalar();
                            if (usernameObj != null)
                            {
                                HttpContext.Session.SetString("Username", usernameObj.ToString());
                            }
                        }

                        // Get Email
                        string emailQuery = @"
                SELECT Email 
                FROM User_Info 
                WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail) 
                AND Password = @Password";

                        using (SqlCommand cmd = new SqlCommand(emailQuery, conn))
                        {
                            cmd.Parameters.Add("@UsernameOrEmail", SqlDbType.NVarChar).Value = usernameOrEmail;
                            cmd.Parameters.Add("@Password", SqlDbType.NVarChar).Value = hashedPassword;

                            object emailObj = cmd.ExecuteScalar();
                            if (emailObj != null)
                            {
                                HttpContext.Session.SetString("Email", emailObj.ToString());
                            }
                        }
                    }

                    return RedirectToAction("Home", "Histology");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during login: {ex.Message}");
                    ViewBag.ErrorMessage = "An error occurred. Please try again.";
                    return View("Login");
                }
            }

            return View("Login");
        }

        public IActionResult Logout()
        {
            ViewBag.HideNavbar = true;
            // Clear the session
            HttpContext.Session.Clear();

            return View();
        }







        //private string HashPassword(string password)
        //{
        //    using (SHA256 sha256 = SHA256.Create())
        //    {
        //        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //        StringBuilder builder = new StringBuilder();
        //        foreach (byte b in bytes)
        //        {
        //            builder.Append(b.ToString("x2"));
        //        }
        //        return builder.ToString();
        //    }
        //}

        public IActionResult ResetPW()
        {
            ViewBag.HideNavbar = true;
            SetLogoUrl();
            return View("ResetPW");
        }

        [HttpPost]
        public IActionResult SendResetPasswordEmail(string email)
        {
            ViewBag.HideNavbar = true;
            SetLogoUrl(); // Ensure the logo URL is set

            if (string.IsNullOrEmpty(email))
            {
                email = TempData["UserEmail"]?.ToString();
            }

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                ViewBag.ErrorMessage = "Invalid email format. Please try again.";
                return View("ResetPW");
            }

            try
            {
                // Check if the email exists in the database
                bool emailExists = false;
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(1) FROM User_Info WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        emailExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }

                if (!emailExists)
                {
                    ViewBag.ErrorMessage = "The entered email does not exist in our records. Please try again.";
                    return View("ResetPW");
                }

                // Generate a random 4-digit verification code
                Random random = new Random();
                int verificationCode = random.Next(1000, 9999);

                // Store the verification code and email in TempData
                TempData["VerificationCode"] = verificationCode.ToString();
                TempData["UserEmail"] = email;

                // Send the email
                SendEmail(email, verificationCode);

                // Keep TempData values for the next request
                TempData.Keep("VerificationCode");
                TempData.Keep("UserEmail");

                ViewBag.IsVerificationCodeSent = true;
                ViewBag.MaskedEmail = MaskEmail(email);
                ViewBag.SuccessMessage = "A new verification code has been sent to your email.";
                return View("ResetPW");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while sending the verification code. Please try again.";
                return View("ResetPW");
            }
        }
        //[HttpGet]
        //public IActionResult TestEmail()
        //{
        //    try
        //    {
        //        // Replace with a valid recipient email for testing
        //        string toEmail = "leeyongchuan0374@gmail.com"; // Test recipient
        //        string fromEmail = "studio037418@gmail.com";   // Your Gmail
        //        string fromPassword = "hfir ebuz npyp xpio";  // Your App Password
        //        string subject = "SMTP Test";
        //        string body = "This is a test email to verify SMTP configuration.";

        //        using (MailMessage mail = new MailMessage())
        //        {
        //            mail.From = new MailAddress(fromEmail);
        //            mail.To.Add(toEmail);
        //            mail.Subject = subject;
        //            mail.Body = body;

        //            // Use Gmail's SMTP settings
        //            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
        //            {
        //                smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
        //                smtp.EnableSsl = true;
        //                smtp.Timeout = 10000; // Set a timeout of 10 seconds
        //                smtp.Send(mail);
        //            }
        //        }

        //        return Content("Test email sent successfully!");
        //    }
        //    catch (SmtpException smtpEx)
        //    {
        //        // Log SMTP-specific errors
        //        Console.WriteLine($"SMTP Exception: {smtpEx.Message}");
        //        return Content($"SMTP error: {smtpEx.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log generic errors
        //        Console.WriteLine($"General Exception: {ex.Message}");
        //        return Content($"Error sending email: {ex.Message}");
        //    }
        //}


        private void SendEmail(string toEmail, int verificationCode)
        {
            string fromEmail = "aihistoemail@gmail.com";
            string fromPassword = "fmxa frop szee ahnr";
            string subject = "Reset Your Password - Verification Code";


            string body = $@"
        <html>
            <body style='font-family: Arial, sans-serif; text-align: center;'>
                <h2 style='color: #005792;'>Welcome to Histology</h2>
                <p>We received a request to reset the password for your account.</p>
                <p>Please use the verification code below to proceed. This code will expire in <strong>10 minutes</strong>:</p>
                <h3 style='color: #FF5722;'>{verificationCode}</h3>
                <p>If you did not request this, please ignore this email. Your account is safe.</p>
                <p>Thank you for using Histology. If you have any questions, feel free to contact our support team.</p>
                <img src='cid:LogoImage' style='margin-top: 20px; width: 150px;' alt='Histology Logo' />
                <p style='margin-top: 10px;'>Best regards,<br><strong>Histology Team</strong></p>
            </body>
        </html>";

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(fromEmail);
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.IsBodyHtml = true;


                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, null, "text/html");


                LinkedResource logo = new LinkedResource("wwwroot/images/logo.png", "image/png");
                logo.ContentId = "LogoImage";
                htmlView.LinkedResources.Add(logo);

                mail.AlternateViews.Add(htmlView);

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }

        private void SetLogoUrl()
        {
            string imageUrl = "/images/logo.png";
            try
            {
                string query = "SELECT TOP 1 Photo_URL FROM Photos WHERE Photo_Description = 'Logo'";
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            imageUrl = result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching logo image: {ex.Message}");
            }

            ViewBag.ImageUrl = imageUrl;
        }

        private string MaskEmail(string email)
        {
            int atIndex = email.IndexOf("@");
            if (atIndex < 3) return email;

            string maskedPart = new string('*', atIndex - 2);
            return email.Substring(0, 2) + maskedPart + email.Substring(atIndex);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            ViewBag.HideNavbar = true;
            try
            {

                string storedCode = TempData["VerificationCode"]?.ToString();
                string userEmail = TempData["UserEmail"]?.ToString();


                TempData.Keep("VerificationCode");
                TempData.Keep("UserEmail");

                if (!string.IsNullOrEmpty(storedCode) && code == storedCode)
                {
                    ViewBag.IsPasswordReset = true;
                    SetLogoUrl();
                    return View("ResetPW");
                }

                ViewBag.ErrorMessage = "The verification code is not correct. Please try again.";
                ViewBag.IsVerificationCodeSent = true;
                ViewBag.MaskedEmail = MaskEmail(userEmail);
                SetLogoUrl();
                return View("ResetPW");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyCode: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred. Please try again.";
                return RedirectToAction("ResetPW");
            }
        }


        [HttpPost]
        public IActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            ViewBag.HideNavbar = true;

            // Check if passwords match
            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                ViewBag.IsPasswordReset = true;
                SetLogoUrl();
                return View("ResetPW");
            }

            // Validate password using regex
            if (!IsValidPassword(newPassword))
            {
                ViewBag.ErrorMessage = "Password must be 8-24 characters long, contain at least one uppercase letter, one number, and no symbols.";
                ViewBag.IsPasswordReset = true;
                SetLogoUrl();
                return View("ResetPW");
            }

            try
            {
                string userEmail = TempData["UserEmail"]?.ToString();
                if (string.IsNullOrEmpty(userEmail))
                {
                    ViewBag.ErrorMessage = "Session expired. Please start the process again.";
                    return RedirectToAction("ResetPW");
                }

                // Hash the new password using SHA256
                string hashedPassword = DBUtl.HashPassword(newPassword);

                // Update the password in the database
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "UPDATE User_Info SET Password = @Password WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", hashedPassword);
                        cmd.Parameters.AddWithValue("@Email", userEmail);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            ViewBag.ErrorMessage = "An error occurred. Please try again.";
                            ViewBag.IsPasswordReset = true;
                            SetLogoUrl();
                            return View("ResetPW");
                        }
                    }
                }

                // Clear TempData and show success message
                TempData.Clear();
                ViewBag.ShowSuccessModal = true;
                SetLogoUrl();
                return View("ResetPW");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred while updating the password. Please try again.";
                ViewBag.IsPasswordReset = true;
                SetLogoUrl();
                return View("ResetPW");
            }
        }



        private bool IsValidPassword(string password)
        {
            var passwordPattern = "^(?=.*[A-Z])(?=.*[0-9])[A-Za-z0-9]{8,24}$";
            return System.Text.RegularExpressions.Regex.IsMatch(password, passwordPattern);
        }

        public IActionResult AccessDenied()
        {

            return View();
        }


        [HttpGet]
        public IActionResult Signup()
        {
            ViewBag.HideNavbar = true;
            SetLogoUrl();

            // Populate TempData if not already set
            ViewData["Username"] = TempData["Username"] ?? string.Empty;
            ViewData["Email"] = TempData["Email"] ?? string.Empty;

            return View();
        }



        [HttpPost]
        public IActionResult Signup(Account account, string confirmPassword)
        {
            SetLogoUrl();

            Dictionary<string, string> errorMessages = new();

            // Validation checks
            ValidateAccount(account, confirmPassword, errorMessages);

            // If validation errors exist, redirect back to Signup
            if (errorMessages.Count > 0)
            {
                StoreErrorsInTempData(errorMessages, account);
                return RedirectToAction("Signup");
            }

            // Check if the username or email already exists
            if (IsAccountExists(account.Username, account.Email))
            {
                TempData["Errors"] = new Dictionary<string, string>
{
    { "Username", "An account with this username or email already exists." }
};
                TempData["Username"] = account.Username;
                TempData["Email"] = account.Email;
                return RedirectToAction("Signup");
            }

            // Generate a verification code
            string verificationCode = GenerateVerificationCode();

            // Send the verification email
            if (!SendVerificationEmail(account.Email, verificationCode))
            {
                TempData["Errors"] = new Dictionary<string, string>
{
    { "General", "Failed to send verification email. Please try again." }
};
                return RedirectToAction("Signup");
            }

            // Temporarily store the user data and verification code in TempData
            TempData["VerificationCode"] = verificationCode;
            TempData["PendingUser"] = JsonConvert.SerializeObject(account);

            // Redirect to the Verify Email page
            return RedirectToAction("VerifyEmail");
        }


        // Helper methods
        private void ValidateAccount(Account account, string confirmPassword, Dictionary<string, string> errorMessages)
        {
            if (string.IsNullOrEmpty(account.Username))
                errorMessages["Username"] = "Username is required.";
            if (string.IsNullOrEmpty(account.Email))
                errorMessages["Email"] = "Email is required.";
            if (string.IsNullOrEmpty(account.Password))
                errorMessages["Password"] = "Password is required.";

            if (!string.IsNullOrEmpty(account.Email) && !IsValidEmail(account.Email))
            {
                errorMessages["Email"] = "Invalid email format.";
            }

            if (!string.IsNullOrEmpty(account.Password) && account.Password != confirmPassword)
            {
                errorMessages["ConfirmPassword"] = "Passwords do not match.";
            }

            if (!string.IsNullOrEmpty(account.Username) &&
                !System.Text.RegularExpressions.Regex.IsMatch(account.Username, @"^[a-zA-Z0-9]{6,24}$"))
            {
                errorMessages["Username"] = "Username must be 6-24 characters long and contain only letters and numbers.";
            }

            if (!string.IsNullOrEmpty(account.Password) &&
                !System.Text.RegularExpressions.Regex.IsMatch(account.Password, @"^(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,24}$"))
            {
                errorMessages["Password"] = "Password must be 8-24 characters, include an uppercase letter, a number, and contain no symbols.";
            }
        }

        private void StoreErrorsInTempData(Dictionary<string, string> errorMessages, Account account)
        {
            TempData["Errors"] = errorMessages;
            TempData["Username"] = account.Username;
            TempData["Email"] = account.Email;
        }

        private bool IsAccountExists(string username, string email)
        {
            string checkSql = $"SELECT * FROM User_Info WHERE Username = '{username}' OR Email = '{email}'";
            return DBUtl.GetTable(checkSql).Rows.Count > 0;
        }

        private bool SaveAccountToDatabase(Account account)
        {
            string hashedPassword = DBUtl.HashPassword(account.Password);
            string insertSql = $"INSERT INTO User_Info (Username, Password, Email) VALUES ('{account.Username}', '{hashedPassword}', '{account.Email}')";
            return DBUtl.ExecSQL(insertSql) == 1;
        }

        private static string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(1000, 9999).ToString(); // Generates a random 4-digit code
        }
        private bool SendVerificationEmail(string recipientEmail, string verificationCode)
        {
            try
            {
                string fromEmail = "aihistoemail@gmail.com";
                string fromPassword = "fmxa frop szee ahnr";
                string subject = "Email Verification Code";

                // HTML body with embedded logo and verification code
                string body = $@"
<html>
    <body style='font-family: Arial, sans-serif; text-align: center;'>
        <h2 style='color: #005792;'>Welcome to Histology</h2>
        <p>Thank you for signing up! Please use the verification code below to verify your email address. This code will expire in <strong>10 minutes</strong>:</p>
        <h3 style='color: #FF5722;'>{verificationCode}</h3>
        <p>If you did not request this, please ignore this email. Your account is safe.</p>
        <p>Thank you for using Histology. If you have any questions, feel free to contact our support team.</p>
        <img src='cid:LogoImage' style='margin-top: 20px; width: 150px;' alt='Histology Logo' />
        <p style='margin-top: 10px;'>Best regards,<br><strong>Histology Team</strong></p>
    </body>
</html>";

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(fromEmail);
                    mail.To.Add(recipientEmail);
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;

                    // Create an alternate view for the HTML body
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, null, "text/html");

                    // Embed the logo in the email
                    LinkedResource logo = new LinkedResource("wwwroot/images/logo.png", "image/png");
                    logo.ContentId = "LogoImage"; // Ensure this matches the `src` attribute in the HTML
                    htmlView.LinkedResources.Add(logo);

                    mail.AlternateViews.Add(htmlView);

                    // Send the email using SMTP
                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }

                return true; // Email sent successfully
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
                return false; // Email sending failed
            }
        }
        [HttpGet]
        public IActionResult VerifyEmail()
        {
            ViewBag.HideNavbar = true;
            SetLogoUrl();

            // Render the verification page
            return View();
        }


        [HttpPost]
        public IActionResult VerifyEmail(string verificationCode)
        {
            SetLogoUrl();

            // Retrieve the stored verification code and user data from TempData
            string expectedCode = TempData["VerificationCode"]?.ToString();
            Account pendingUser = TempData["PendingUser"] != null
                ? JsonConvert.DeserializeObject<Account>(TempData["PendingUser"].ToString())
                : null;

            // Check if the verification session is valid
            if (expectedCode == null || pendingUser == null)
            {
                TempData["Errors"] = new Dictionary<string, string>
        {
            { "General", "Verification session expired. Please try signing up again." }
        };
                return RedirectToAction("Signup");
            }

            // Verify if the entered code matches the stored verification code
            if (verificationCode == expectedCode)
            {
                // Insert the user into the database after successful verification
                string hashedPassword = DBUtl.HashPassword(pendingUser.Password);
                string insertSql = $"INSERT INTO User_Info (Username, Password, Email) VALUES ('{pendingUser.Username}', '{hashedPassword}', '{pendingUser.Email}')";

                if (DBUtl.ExecSQL(insertSql) == 1)
                {
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["Errors"] = new Dictionary<string, string>
            {
                { "General", "An error occurred while creating your account. Please try again." }
            };
                    return RedirectToAction("Signup");
                }
            }
            else
            {
                TempData["Errors"] = new Dictionary<string, string>
        {
            { "VerificationCode", "Invalid verification code. Please try again." }
        };
                return RedirectToAction("VerifyEmail");
            }
        }
        [HttpPost]
        public IActionResult ResendVerificationCode()
        {
            ViewBag.HideNavbar = true;
            Account pendingUser = TempData["PendingUser"] != null
                ? JsonConvert.DeserializeObject<Account>(TempData["PendingUser"].ToString())
                : null;

            if (pendingUser == null)
            {
                TempData["Errors"] = new Dictionary<string, string>
        {
            { "General", "Verification session expired. Please try signing up again." }
        };
                return RedirectToAction("Signup");
            }

            // Generate a new verification code
            string newVerificationCode = GenerateVerificationCode();

            // Send the new verification email
            if (!SendVerificationEmail(pendingUser.Email, newVerificationCode))
            {
                TempData["Errors"] = new Dictionary<string, string>
        {
            { "General", "Failed to resend verification email. Please try again later." }
        };
                return RedirectToAction("VerifyEmail");
            }

            // Overwrite the previous code in TempData
            TempData["VerificationCode"] = newVerificationCode;

            // Keep the user data in TempData for verification
            TempData["PendingUser"] = JsonConvert.SerializeObject(pendingUser);

            TempData["Success"] = "A new verification code has been sent to your email address.";
            return RedirectToAction("VerifyEmail");
        }



        public IActionResult Profile()
        {
            ViewBag.HideNavbar = false;


            string userId = HttpContext.Session.GetString("UserId");


            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            User_Info userAccount = new User_Info();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT Username, Password, Email, Role_Status FROM User_Info WHERE User_Id = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userAccount.Username = reader["Username"].ToString();
                                userAccount.Password = reader["Password"].ToString();
                                userAccount.Email = reader["Email"].ToString();
                                userAccount.Role_Status = Convert.ToByte(reader["Role_Status"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving profile: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading your profile. Please try again.";
                return View();
            }

            return View(userAccount);
        }
        [HttpPost]
        public IActionResult UpdateUsername(string newUsername)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            string currentUsername = "";
            bool usernameExists = false;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string getCurrentUsernameQuery = "SELECT Username FROM User_Info WHERE User_Id = @UserId";
                using (SqlCommand cmd = new SqlCommand(getCurrentUsernameQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        currentUsername = result.ToString();
                    }
                }


                string checkUsernameQuery = "SELECT COUNT(*) FROM User_Info WHERE Username = @Username";
                using (SqlCommand cmd = new SqlCommand(checkUsernameQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", newUsername);
                    usernameExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }

            if (newUsername == currentUsername)
            {
                return Json(new { success = false, message = "You are already using this username. Try a different one." });
            }

            if (usernameExists)
            {
                return Json(new { success = false, message = "Username is already taken, please try another." });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE User_Info SET Username = @Username WHERE User_Id = @UserId";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", newUsername);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                HttpContext.Session.SetString("Username", newUsername);
                TempData["SuccessMessage"] = "Your username has been successfully updated!";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating username: {ex.Message}");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }



        [HttpPost]
        public IActionResult RequestEmailChange(string newEmail)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            if (string.IsNullOrWhiteSpace(newEmail) || !IsValidEmail(newEmail))
            {
                return Json(new { success = false, message = "Invalid email format." });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();


                    string currentEmail = "";
                    using (SqlCommand cmd = new SqlCommand("SELECT Email FROM User_Info WHERE User_Id = @UserId", conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            currentEmail = result.ToString();
                        }
                    }

                    if (newEmail == currentEmail)
                    {
                        return Json(new { success = false, message = "You are already using this email. Try a different one." });
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM User_Info WHERE Email = @NewEmail", conn))
                    {
                        cmd.Parameters.AddWithValue("@NewEmail", newEmail);
                        int emailCount = Convert.ToInt32(cmd.ExecuteScalar());

                        if (emailCount > 0)
                        {
                            return Json(new { success = false, message = "Email already used, please try again." });
                        }
                    }
                }


                Random random = new Random();
                int verificationCode = random.Next(1000, 9999);

                TempData["NewEmail"] = newEmail;
                TempData["EmailVerificationCode"] = verificationCode.ToString();
                TempData.Keep("NewEmail");
                TempData.Keep("EmailVerificationCode");

                SendEmailVerificationCode(newEmail, verificationCode);

                return Json(new { success = true, message = "Verification code sent successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email verification: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while sending the verification code. Please try again." });
            }
        }






        [HttpGet]
        public IActionResult VerifyNewEmail()
        {
            ViewBag.HideNavbar = true;


            string newEmail = TempData["NewEmail"]?.ToString();
            ViewBag.MaskedEmail = newEmail != null ? MaskEmail(newEmail) : "";

            return View();
        }

        [HttpPost]
        public IActionResult VerifyNewEmail(string code)
        {
            string storedCode = TempData["EmailVerificationCode"]?.ToString();
            string newEmail = TempData["NewEmail"]?.ToString();
            string userId = HttpContext.Session.GetString("UserId");


            TempData.Keep("NewEmail");
            TempData.Keep("EmailVerificationCode");

            if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Verification session expired. Please try again.";
                return RedirectToAction("Profile");
            }

            if (code != storedCode)
            {
                TempData["ErrorMessage"] = "Invalid verification code. Please try again.";
                return RedirectToAction("VerifyNewEmail");
            }


            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE User_Info SET Email = @Email WHERE User_Id = @UserId";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", newEmail);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                HttpContext.Session.SetString("Email", newEmail);
                TempData["SuccessMessage"] = "Your email has been successfully updated!";
                return RedirectToAction("EmailUpdateSuccess");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating email: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("Profile");
            }
        }



        [HttpGet]
        public IActionResult EmailUpdateSuccess()
        {
            ViewBag.HideNavbar = true;
            return View();
        }

        private void SendEmailVerificationCode(string toEmail, int verificationCode)
        {
            string fromEmail = "aihistoemail@gmail.com";
            string fromPassword = "fmxa frop szee ahnr";
            string subject = "Confirm Your Email Change - Verification Code";

            string body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; text-align: center;'>
                        <h2 style='color: #005792;'>Welcome to Histology</h2>
                        <p>We received a request to change the email associated with your account.</p>
                        <p>Please use the verification code below to confirm your new email address. This code will expire in <strong>10 minutes</strong>:</p>
                        <h3 style='color: #FF5722;'>{verificationCode}</h3>
                        <p>If you did not request this change, please ignore this email. Your account is safe.</p>
                        <p>Thank you for using Histology. If you have any questions, feel free to contact our support team.</p>
                        <img src='cid:LogoImage' style='margin-top: 20px; width: 150px;' alt='Histology Logo' />
                        <p style='margin-top: 10px;'>Best regards,<br><strong>Histology Team</strong></p>
                    </body>
                </html>";

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(fromEmail);
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.IsBodyHtml = true;

                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, null, "text/html");

                LinkedResource logo = new LinkedResource("wwwroot/images/logo.png", "image/png");
                logo.ContentId = "LogoImage";
                htmlView.LinkedResources.Add(logo);

                mail.AlternateViews.Add(htmlView);

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
        }

        [HttpGet]
        public IActionResult CgEmail()
        {
            ViewBag.HideNavbar = true;
            ViewBag.IsVerificationCodeSent = TempData["EmailVerificationCode"] != null;
            ViewBag.SuccessMessage = TempData["SuccessMessage"] ?? "";


            string newEmail = TempData["NewEmail"]?.ToString();
            ViewBag.MaskedEmail = newEmail != null ? MaskEmail(newEmail) : "";

            TempData.Keep("NewEmail");
            TempData.Keep("EmailVerificationCode");


            string imageUrl = "/images/logo.png";
            try
            {
                string query = "SELECT TOP 1 Photo_URL FROM Photos WHERE Photo_Description = 'Logo'";
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            imageUrl = result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching logo image: {ex.Message}");
            }

            ViewBag.ImageUrl = imageUrl;

            return View();
        }




        [HttpPost]
        public IActionResult VerifyCgEmail(string code)
        {
            string storedCode = TempData["EmailVerificationCode"]?.ToString();
            string newEmail = TempData["NewEmail"]?.ToString();
            string userId = HttpContext.Session.GetString("UserId");

            TempData.Keep("NewEmail");
            TempData.Keep("EmailVerificationCode");

            if (string.IsNullOrEmpty(storedCode) || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Verification session expired. Please try again.";
                return RedirectToAction("Profile");
            }

            if (code != storedCode)
            {
                TempData["ErrorMessage"] = "The verification code is not correct . Please try again.";
                return RedirectToAction("CgEmail");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE User_Info SET Email = @Email WHERE User_Id = @UserId";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", newEmail);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                HttpContext.Session.SetString("Email", newEmail);
                TempData["SuccessMessage"] = "Your email has been successfully updated!";
                return RedirectToAction("CgEmail");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating email: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("CgEmail");
            }
        }


        private bool IsCurrentPasswordCorrect(string userId, string currentPassword)
        {
            string storedPassword = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT Password FROM User_Info WHERE User_Id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    storedPassword = cmd.ExecuteScalar()?.ToString();
                }
            }

            return storedPassword != null && storedPassword == currentPassword;
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            if (!IsCurrentPasswordCorrect(userId, currentPassword))
            {
                return Json(new { success = false, field = "currentPassword", message = "Current password is incorrect." });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, field = "confirmPassword", message = "Passwords do not match." });
            }

            if (!UserIsValidPassword(newPassword))
            {
                return Json(new { success = false, field = "newPassword", message = "Password must be 8-24 characters, include at least one number, and mix uppercase & lowercase letters." });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string updateQuery = "UPDATE User_Info SET Password = @Password WHERE User_Id = @UserId";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", newPassword);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Your password has been successfully updated!";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating password: {ex.Message}");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }




        private bool UserIsValidPassword(string password)
        {
            var passwordPattern = "^(?=.*[A-Z])(?=.*\\d)[A-Za-z\\d]{8,24}$";
            bool isValid = System.Text.RegularExpressions.Regex.IsMatch(password, passwordPattern);
            Console.WriteLine($"User password validation result: {isValid}");
            return isValid;
        }


    }
}



