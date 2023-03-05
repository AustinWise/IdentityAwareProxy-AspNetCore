﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SandwichTracker.Models;
using Google.Apis.Auth;

namespace SandwichTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var headersDic = new Dictionary<string, string?>();
        foreach (var head in Request.Headers)
        {
            headersDic.Add(head.Key, head.Value);
        }

        string errorMessage = string.Empty;
        JsonWebSignature.Payload? jwtPayload = null;
        if (headersDic.TryGetValue("x-goog-iap-jwt-assertion", out string? jwtStr))
        {
            try
            {
                var valSettings = new SignedTokenVerificationOptions()
                {
                    CertificatesUrl = "https://www.gstatic.com/iap/verify/public_key-jwk",
                };
                valSettings.TrustedAudiences.Add("/projects/72643967898/global/backendServices/1079754107036193628");
                valSettings.TrustedIssuers.Add("https://cloud.google.com/iap");
                jwtPayload = await JsonWebSignature.VerifySignedTokenAsync(jwtStr, valSettings);
            }
            catch (InvalidJwtException ex)
            {
                errorMessage = ex.ToString();
            }
        }



        var model = new HomeModel(headersDic, jwtPayload, errorMessage);
        return View(model);
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
