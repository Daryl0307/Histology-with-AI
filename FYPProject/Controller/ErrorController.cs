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


public class ErrorController : Controller
{

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        ViewBag.HideNavbar = true;
        return View("AccessDenied");
    }
}

