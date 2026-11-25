using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Account");
    }

    // "Lodgo ทำอะไรได้บ้าง": the whole flow with every demo account and its password, so only Owner and Admin may open it.
    [HttpGet, Authorize(Policy = Nontiwat_7362_L002.Security.RolePolicies.Owner)]
    public IActionResult Guide([FromServices] IConfiguration config)
    {
        ViewData["ShowPasswords"] = config.GetValue("Demo:ShowAccounts", true);
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Lab2()
    {
        string Name , Lastname ;
        Name = "Nontiwat" ;
        Lastname = "Muensai" ;
        ViewBag.Stdname = Name+Lastname;
        
        string Sec = "002";
        ViewBag.Stdsec = Sec;

        char Grade;
        Grade = 'A';
        ViewBag.Stdgrade = Grade;

        string Language = "Javascript";
        ViewBag.Stdlanguage = Language;

        int A1, A2, A3, A4, A5, A6, A7, A8, A9, A10,Sum;
        
        A1 = 0;
        A2 = 0;
        A3 = 9;
        A4 = 10;
        A5 = 00;
        A6 = 10;
        A7 = 10;
        A8 = 10;
        A9 = 10;
        A10 = 10;
        Sum = A1 + A2 + A3 + A4 + A5 + A6 + A7 + A8 + A9 + A10;
        

        if (Sum >= 80 )
        {
            ViewBag.result = "A";
        }
        else if (Sum >= 76)
        {
            ViewBag.result = "B+";
        }
        else if (Sum >= 70)
        {
            ViewBag.result = "B";
        }
        else if (Sum >= 66)
        {
            ViewBag.result = "C+";
        }
        else if (Sum >= 56)
        {
            ViewBag.result = "C";
        }
          else if (Sum >= 49)
        {
            ViewBag.result = "F";
        }

        return View();
     }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
