using System.Diagnostics;
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
        ViewBag.data = _context.ClusterConfiguration.ToList();
        
        /*
        PING NODES HELE
        */
        ViewBag.pingResult = "Success";
        
        return View("Index");
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
        ViewBag.data = _context.ClusterConfiguration.ToList();
        return View("Index");
    }

    [Authorize]
    [HttpPost]
    public IActionResult Delete(int nodeId)
    {
        _logger.LogInformation($"Deleting node with id {nodeId}");
        var nodeToDelete = _context.ClusterConfiguration.Find(nodeId);
        if (nodeToDelete == null)
        {
            _logger.LogError($"Node with id {nodeId} not found");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        _context.ClusterConfiguration.Remove(nodeToDelete);
        _context.SaveChanges();
        _logger.LogInformation($"Node with id {nodeId} deleted");
        return RedirectToAction("Index");
    }
}