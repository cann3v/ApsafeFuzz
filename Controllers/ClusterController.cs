using System.Diagnostics;
using System.Net.Sockets;
using ApSafeFuzz.Data;
using ApSafeFuzz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApSafeFuzz.Utilities;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ApSafeFuzz.Controllers;

public class ClusterController : Controller
{
    private readonly ILogger<ClusterController> _logger;
    private readonly ApplicationDbContext _context;
    
    public ClusterController(
        ApplicationDbContext context,
        ILogger<ClusterController> logger
        )
    {
        _context = context;
        _logger = logger;
    }
    
    [Authorize]
    public async Task<IActionResult> Index()
    {
        List<ClusterConfigurationModel> nodes = _context.ClusterConfiguration.ToList();
        if (nodes.Count == 0)
        {
            ViewBag.nodesData = new List<string>();
        }
        else
        {
            foreach (ClusterConfigurationModel node in nodes)
            {
                string host = node.IpAddress;
                string user = node.Username;
                string password = node.Password;
                _logger.LogDebug($"Pinging node {user}@{host} ({password})");
                var sshClient = new SshClient(host, user, password);
                try
                {
                    await sshClient.ConnectAsync(default(CancellationToken));
                    node.ConnectionState = "Success";
                }
                catch (SshConnectionException e)
                {
                    _logger.LogError($"SSH connection exception with {user}@{host} ({password}): {e}");
                    node.ConnectionState = "Error";
                }
                catch (SocketException e)
                {
                    _logger.LogError($"Socket exception with {user}@{host} ({password}): {e}");
                    node.ConnectionState = "Error";
                }
                sshClient.Disconnect();
            }
            ViewBag.nodesData = nodes;
        }
        
        // Get shared storage info
        SharedStorageModel? storage = await _context.SharedStorage.FirstAsync();
        if (storage == null)
        {
            _logger.LogError($"Shared storage not defined");
            storage = new SharedStorageModel()
                { IpAddress = "Ip address", Password = "Password", Username = "Username", LastState = false };
        }
        
        return View("Index", storage);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GetCreds(ClusterConfigurationModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Received invalid model");
            return View("Error",
                new ErrorViewModel()
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, ErrorMessage = "Invalid Model"
                });
        }
        
        _logger.LogDebug($"Saving creds: {model.Username}@{model.IpAddress} ({model.Password})");
        await _context.ClusterConfiguration.AddAsync(model);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(int nodeId)
    {
        _logger.LogDebug($"Deleting node with id {nodeId}");
        var nodeToDelete = await _context.ClusterConfiguration.FindAsync(nodeId);
        if (nodeToDelete == null)
        {
            _logger.LogError($"Node with id {nodeId} not found");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        _context.ClusterConfiguration.Remove(nodeToDelete);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Node with id {nodeId} was deleted");
        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SaveSharedStorage(SharedStorageModel storage)
    {
        _logger.LogDebug(
            $"Saving shared storage settings: " +
            $"{storage.IpAddress}: ***{storage.Username[^4..]}@***{storage.Password[^4..]}");

        SharedStorageModel? existingStorage = await _context.SharedStorage.FirstAsync();
        if (existingStorage == null)
        {
            await _context.SharedStorage.AddAsync(storage);
        }
        else
        {
            existingStorage.IpAddress = storage.IpAddress;
            existingStorage.Username = storage.Username;
            existingStorage.Password = existingStorage.Password;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CheckConnection()
    {
        SharedStorageModel storage = await _context.SharedStorage.FirstAsync();
        if (storage == null)
        {
            _logger.LogDebug("No storage to ping...");
        }
        else
        {
            ILogger staticLogger = LogHelper.CreateStaticLogger("SSHExecutor");
            bool result = await SSHExecutor.PingNode(new ClusterConfigurationModel()
            {
                ConnectionState = null, Id = -1, IpAddress = storage.IpAddress, Password = storage.Password,
                Username = storage.Username
            }, staticLogger);
            storage.LastState = result;
            await _context.SaveChangesAsync();
        }
        
        
        return RedirectToAction("Index");
    }
}
