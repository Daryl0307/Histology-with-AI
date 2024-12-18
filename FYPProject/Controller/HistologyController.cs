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
    public IActionResult Kidney()
    {
        return View();
    }
    public IActionResult Lung()
    {
        return View();
    }
    public IActionResult ControlPanel()
    {
        return View();
    }
}