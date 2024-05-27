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
    private readonly IConfiguration _configuration;
    private const long MaxFileSize = 10L * 1024L * 1024L * 1024L;

    public FuzzingLaunchController(
        ApplicationDbContext context,
        IOptions<UploadFileSettingsModel> uploadFileSettings,
        IConfiguration configuration,
        ILogger<FuzzingLaunchController> logger)
    {
        _context = context;
        _uploadFileSettings = uploadFileSettings;
        _configuration = configuration;
        _logger = logger;
    }
    
    [Authorize]
    public IActionResult Index()
    {
        ViewBag.buildsCount = _context.UploadFileSettings.ToList().Count;
        ViewBag.fuzzingTasks = _context.FuzzingTasks.ToList();
        return View("Index");
    }

    [Authorize]
    [HttpGet]
    public IActionResult Upload()
    {
        List<UploadFileSettingsModel> buildsOptions = _context.UploadFileSettings.ToList();
        return View("Upload", buildsOptions);
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
        
        var filePath = Path.Combine(_uploadFileSettings.Value.FilePath, _uploadFileSettings.Value.InternalName);
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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(int fileId)
    {
        _logger.LogDebug($"Deleting file with id {fileId}");
        UploadFileSettingsModel? fileToDelete = await _context.UploadFileSettings.FindAsync(fileId);
        if (fileToDelete == null)
        {
            _logger.LogError($"File with id {fileId} not found");
            return View("Error",
                new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        string filePath = Path.Combine(fileToDelete.FilePath, fileToDelete.InternalName);
        int result = FileHelper.DeleteFile(filePath);
        switch (result)
        {
            case 0:
                _logger.LogDebug($"File ({fileToDelete.Id}/{fileToDelete.InternalName}) was deleted from disk");
                break;
            case 1:
                _logger.LogWarning($"File ({fileToDelete.Id}/{fileToDelete.InternalName}) does not exist");
                return View("Error",
                    new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                        ErrorMessage = "FIle does not exist"
                    });
            case 2:
                _logger.LogError(
                    $"File ({fileToDelete.Id}/{fileToDelete.InternalName}): error while deleting from disk");
                return View("Error",
                    new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorMessage = "Unknown error"
                    });
        }
        _context.UploadFileSettings.Remove(fileToDelete);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"File ({fileToDelete.Id}/{fileToDelete.InternalName}) was deleted");
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(FuzzingTaskModel model)
    {
        model.CreateTime = DateTime.Now;
        if (model.Fuzzer == null || model.BuildId == null)
        {
            _logger.LogError("Received invalid model");
            return View("Error",
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorMessage = "Invalid model"
                });
        }

        await _context.FuzzingTasks.AddAsync(model);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        _logger.LogDebug($"Deleting fuzzing task {taskId}");
        FuzzingTaskModel? task = await _context.FuzzingTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogError($"Task with id {taskId} not found");
            return View("Error",
                new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        _context.FuzzingTasks.Remove(task);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Fuzzing task {taskId} was deleted");
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetTask(int taskId)
    {
        FuzzingTaskModel? task = await _context.FuzzingTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogError($"Task with id {taskId} not found");
            return View("Error",
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "Task with this id not found"
                });
        }

        await _context.UploadFileSettings.FindAsync(task.BuildId);

        // Add nodes to ViewBag
        List<ClusterConfigurationModel> nodes = _context.ClusterConfiguration.ToList();
        if (nodes.Count == 0)
        {
            return View("Error",
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorMessage = "There are no nodes"
                });
        }
        ViewBag.nodes = nodes;

        return View("Task", task);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> RunTask(int taskId, int[] selectedNodes)
    {
        if (selectedNodes.Length == 0)
        {
            _logger.LogError("No one node was selected");
            return View("Error",
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "No one node was selected"
                });   
        }
        
        FuzzingTaskModel? task = await _context.FuzzingTasks.FindAsync(taskId);
        if (task == null)
        {
            _logger.LogError($"Task with id {taskId} not found");
            return View("Error",
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "Task with this id not found"
                });
        }
        await _context.UploadFileSettings.FindAsync((task.BuildId));
        
        // Here starts SSH magic
        ILogger staticLogger = LogHelper.CreateStaticLogger("SSHExecutor");
        
        // All selected nodes
        List<ClusterConfigurationModel> nodes = _context.ClusterConfiguration
            .Where(c => selectedNodes.Contains(c.Id))
            .ToList();
        foreach (ClusterConfigurationModel node in nodes)
        {
            // Check connection
            bool isAvailable = await SSHExecutor.PingNode(node, staticLogger);
            if (!isAvailable)
            {
                _logger.LogError($"Selected node unavailable: {node.IpAddress}");
                return View("Error",
                    new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                        ErrorMessage = "Selected node unavailable"
                    });
            }
            
            // mkdirs
            bool isDirsCreated;
            if (task.Fuzzer == "AFL++")
            {
                isDirsCreated =
                    await SSHExecutor.CreateDirectoriesAFL(node, task.Id, _configuration["NFSRoot"], staticLogger);
            }
            else if (task.Fuzzer == "libFuzzer")
            {
                isDirsCreated =
                    await SSHExecutor.CreateDirectoriesLibFuzzer(node, task.Id, _configuration["NFSRoot"],
                        staticLogger);
            }
            else
            {
                isDirsCreated = false;
            }
            if (!isDirsCreated)
            { 
                _logger.LogError($"Can not create directory for task {taskId}");
                return View("Error",
                    new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                        ErrorMessage = "Can not create directories"
                    });
            }
        }
        
        return RedirectToAction("GetTask", new { taskId = taskId });
    }
}
