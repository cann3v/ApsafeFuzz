using System.Diagnostics;
using ApSafeFuzz.Data;
using ApSafeFuzz.Models;
using ApSafeFuzz.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApSafeFuzz.Controllers;

public class FuzzingLaunchController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FuzzingLaunchController> _logger;
    private readonly IOptions<UploadFileSettingsModel> _uploadFileSettings;
    private const long MaxFileSize = 10L * 1024L * 1024L * 1024L;

    public FuzzingLaunchController(
        ApplicationDbContext context,
        IOptions<UploadFileSettingsModel> uploadFileSettings,
        ILogger<FuzzingLaunchController> logger)
    {
        _context = context;
        _uploadFileSettings = uploadFileSettings;
        _logger = logger;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    [Authorize]
    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> Upload(IFormFile uploadedFile)
    {
        _logger.LogDebug($"User {HttpContext.User.Identity?.Name} is uploading a file");
        if (uploadedFile == null || uploadedFile.Length == 0)
        {
            _logger.LogError("Empty file");
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        _uploadFileSettings.Value.UploadName = uploadedFile.FileName;
        _uploadFileSettings.Value.UploadTime = DateTime.Now;
        _uploadFileSettings.Value.Owner = HttpContext.User.Identity?.Name;
        _uploadFileSettings.Value.InternalName = $"{Guid.NewGuid()}{FileHelper.GetExtension(uploadedFile.FileName)}";
        
        var filePath = Path.Combine(_uploadFileSettings.Value.FilePath, _uploadFileSettings.Value.InternalName); // TODO: FileName INJECTION!!
        _logger.LogDebug($"Saving file: {filePath}");
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await uploadedFile.CopyToAsync(stream);
        }
        _logger.LogInformation($"File saved: {filePath}");
        await _context.UploadFileSettings.AddAsync(_uploadFileSettings.Value);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
