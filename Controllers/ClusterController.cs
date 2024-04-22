using ApSafeFuzz.Data;
using ApSafeFuzz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApSafeFuzz.Controllers;

public class ClusterController : Controller
{
    private readonly ILogger<ClusterController> _logger;
    private readonly ApplicationDbContext _context;
    
    public ClusterController(ApplicationDbContext context, ILogger<ClusterController> logger)
    {
        _context = context;
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
        if (!ModelState.IsValid)
        {
            _logger.LogError("Received invalid model");
            return View("Error");
        }
        
        _logger.LogDebug($"Saving creds: {model.Username}@{model.IpAddress} ({model.Password})");
        _context.ClusterConfiguration.Add(model);
        _context.SaveChanges();
        return View("Index");
    }
}