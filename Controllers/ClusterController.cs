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
}