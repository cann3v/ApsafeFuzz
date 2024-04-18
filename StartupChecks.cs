using System.Diagnostics;

namespace ApSafeFuzz;

public class StartupChecks
{
    private readonly ILogger<StartupChecks> _logger;

    public StartupChecks(ILogger<StartupChecks> logger)
    {
        _logger = logger;
    }
    public void IsAnsibleInstalled()
    {
        string fileName = "ansible";
        string arguments = "--version";
        string output;
        string error;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            _logger.LogDebug($"Trying to start process {fileName} with args {arguments}");
            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogError($"var process == null");
            }

            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            process.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            _logger.LogCritical(e.Message);
            _logger.LogCritical("Ansible is not installed");
            throw new Exception("Startup checks failed!");
        }
        
        if (!string.IsNullOrEmpty(output) && output.Contains(fileName))
        {
            _logger.LogDebug("Ansible is installed");
        }
        else
        {
            _logger.LogDebug($"stdout: {output}");
            _logger.LogDebug($"stderr: {error}");
            _logger.LogCritical("Ansible is not installed");
            throw new Exception("Startup checks failed!");
        }
        return;
    }
}