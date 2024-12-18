using Microsoft.AspNetCore.Mvc;

namespace FYPProject.Controllers;


public class HistologyController : Controller
{
    public IActionResult Home()
    {
        return View();
    }

    public IActionResult Histopedia()
    {
        return View();
    }

    public IActionResult Histoscanner()
    {
        return View();
    }

}
