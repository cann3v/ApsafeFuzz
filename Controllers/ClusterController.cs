using ApSafeFuzz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApSafeFuzz.Controllers;

public class ClusterController : Controller
{
    private readonly ILogger<ClusterController> _logger;

    public ClusterController(ILogger<ClusterController> logger)
    {
        _logger = logger;
    }
    
    // GET
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public IActionResult GetCreds(ClusterConfigurationModel model)
    {
        _logger.LogDebug($"Provided creds: {model.Username}@{model.IpAddress} ({model.Password})");
        return View("Index");
    }
}