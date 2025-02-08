using FYPProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FYPProject.Components
{
    public class NavbarViewComponent : ViewComponent
    {
        private readonly ApplicationDBContext _context;

        public NavbarViewComponent(ApplicationDBContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var logoUrl = _context.Photos
                .Where(photo => photo.Photo_Description == "AILogo")
                .Select(photo => photo.Photo_URL)
                .FirstOrDefault() ?? "/images/default-logo.png";

            var backgroundImageUrl = _context.Photos
                .Where(photo => photo.Photo_Description == "BackgroundImage")
                .Select(photo => photo.Photo_URL)
                .FirstOrDefault() ?? "/images/default-background.png";

            ViewBag.LogoImage = logoUrl;
            ViewBag.BackgroundImage = backgroundImageUrl;
            ViewBag.IsLoggedIn = false;
            ViewBag.HasNotifications = false; 

            return View("~/Views/Shared/Components/Navbar/_Navbar.cshtml");
        }
    }
}
