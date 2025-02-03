using Microsoft.AspNetCore.Mvc;
using RP.SOI.DotNet.Utils;
using System.Data;
namespace FYPProject.Controllers;
using System;
using System.Collections.Generic;


public class HistologyController : Controller
{
    public IActionResult Home()
    {
        ViewBag.Role = HttpContext.Session.GetInt32("Role");
        ViewBag.HideNavbar = false;
        return View();
    }

    public IActionResult Histopedia()
    {
        ViewBag.Role = HttpContext.Session.GetInt32("Role");
        ViewBag.HideNavbar = false;
        ViewBag.ActivePage = "Histopedia";
        return View();
    }

    public IActionResult Histoscanner()
    {
        ViewBag.Role = HttpContext.Session.GetInt32("Role");
        ViewBag.HideNavbar = false;
        return View();
    }
    public IActionResult Kidney()
    {
        ViewBag.Role = HttpContext.Session.GetInt32("Role");
        ViewBag.HideNavbar = false;
        return View();
    }
    public IActionResult Lung()
    {
        ViewBag.Role = HttpContext.Session.GetInt32("Role");
        ViewBag.HideNavbar = false;
        return View();
    }

}