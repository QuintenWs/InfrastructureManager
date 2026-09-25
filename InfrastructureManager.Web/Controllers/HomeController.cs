using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InfrastructureManager.Web.Models;

namespace InfrastructureManager.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Vervangt de standaard scaffolding-placeholder: het echte startpunt van
    // de app is Dashboard/Index (zie de default route in Program.cs), dus
    // stuur iedereen die hier toch belandt daar gewoon naartoe in plaats van
    // een lege demo-pagina te tonen.
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard");
    }

    // Genoemde foutafhandelingspagina (zie app.UseExceptionHandler("/Home/Error")
    // in Program.cs) — moet bereikbaar zijn ongeacht of de gebruiker
    // ingelogd is, anders krijgt iemand die niet-ingelogd een fout tegenkomt
    // een verwarrende doorverwijzing naar de login-pagina te zien in plaats
    // van een nette foutmelding.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}