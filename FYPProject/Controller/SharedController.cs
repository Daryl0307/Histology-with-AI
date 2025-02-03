using FYPProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class SharedController : Controller
{
    private readonly ApplicationDBContext _context;

    public SharedController(ApplicationDBContext context)
    {
        _context = context;
    }

    public IActionResult Navbar()
    {
        // Fetch logo and background image URLs from the Photos table
        var logoUrl = _context.Photos
            .Where(photo => photo.Photo_Description == "AILogo")
            .Select(photo => photo.Photo_URL)
            .FirstOrDefault();

        var backgroundImageUrl = _context.Photos
            .Where(photo => photo.Photo_Description == "BackgroundImage")
            .Select(photo => photo.Photo_URL)
            .FirstOrDefault();

        // Return partial view for navbar
        return PartialView("_Navbar");
    }

    public void SetSharedViewData()
    {
        // Ensure logo and background image URLs are available globally for all views
        var logoUrl = _context.Photos
            .Where(photo => photo.Photo_Description == "AILogo")
            .Select(photo => photo.Photo_URL)
            .FirstOrDefault();

        var backgroundImageUrl = _context.Photos
            .Where(photo => photo.Photo_Description == "BackgroundImage")
            .Select(photo => photo.Photo_URL)
            .FirstOrDefault();

        ViewBag.LogoImage = logoUrl ?? "/images/default-logo.png";
        ViewBag.BackgroundImage = backgroundImageUrl ?? "/images/default-background.png";
    }
}
