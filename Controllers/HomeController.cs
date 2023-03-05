using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SandwichTracker.Models;

namespace SandwichTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var dic = new Dictionary<string, string>();
        foreach (var head in Request.Headers)
        {
            dic.Add(head.Key, head.Value);
        }
        return View(dic);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
