using Microsoft.AspNetCore.Mvc;
using RP.SOI.DotNet.Utils;
using System.Data;
namespace FYPProject.Controllers;
using System;
using System.Collections.Generic;
public class AccountController : Controller
{
    public IActionResult AccessDenied()
    {
        return View();
    }
   
    public IActionResult Login()
    {
        return View();
    }
    public IActionResult Signup()
    {
        return View();
    }

}
