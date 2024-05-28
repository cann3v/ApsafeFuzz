using System.Net.Sockets;
using ApSafeFuzz.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ApSafeFuzz;

public static class SSHExecutor
{
    /// <summary>
    /// Метод для проверки доступности ноды
    /// </summary>
    /// <param name="node">Нода</param>
    /// <param name="logger">Статический объект логгера</param>
    /// <returns>
    /// true - если нода доступна для подключения по SSH;
    /// false - в остальных случаях.
    /// </returns>
    public static async Task<bool> PingNode(ClusterConfigurationModel node, ILogger logger)
    {
        string host = node.IpAddress;
        string user = node.Username;
        string password = node.Password;
        
        logger.LogDebug($"Pinging node {user}@{host} ({password})");
        var sshClient = new SshClient(host, user, password);
        try
        {
            await sshClient.ConnectAsync(default(CancellationToken));
            sshClient.Disconnect();
            logger.LogDebug($"Ping node {user}@{host} successfully");
            return true;
        }
        catch (SshConnectionException e)
        {
            logger.LogError($"SSH connection exception with {user}@{host} ({password}): {e}");
            return false;
        }
        catch (SocketException e)
        {
            logger.LogError($"Socket exception with {user}@{host} ({password}): {e}");
            return false;
        }
    }

    /// <summary>
    /// Создает директории для фаззера AFL++
    /// </summary>
    /// <param name="node">Нода, где создавать</param>
    /// <param name="taskId">Идентификатор задачи</param>
    /// <param name="rootPath">Пусть к корню общей папки</param>
    /// <param name="logger">Статический объект логгера</param>
    /// <returns>
    /// true - директории созданы успешно;
    /// false - директории не созданы
    /// </returns>
    public static async Task<bool> CreateDirectoriesAFL(
        ClusterConfigurationModel node,
        int taskId,
        string rootPath,
        ILogger logger)
    {
        string host = node.IpAddress;
        string user = node.Username;
        string password = node.Password;

        logger.LogDebug($"Creating directories for AFL fuzzing");
        var sshClient = new SshClient(host, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        
        // Create directories
        string cmd =
            $"mkdir {Path.Combine(rootPath, $"task{taskId}-afl/")} " +
            $"&& mkdir {Path.Combine(rootPath, $"task{taskId}-afl/", "in")} " +
            $"&& mkdir {Path.Combine(rootPath, $"task{taskId}-afl/", $"out")}";
        logger.LogDebug($"Executing command on {node.IpAddress}: {cmd}");
        SshCommand command = sshClient.RunCommand(cmd);
        
        // Check directories
        cmd = $"ls {Path.Combine(rootPath, $"task{taskId}-afl/")}";
        logger.LogDebug($"Executing command on {node.IpAddress}: {cmd}");
        command = sshClient.RunCommand(cmd);

        if (command.Result.Contains("in") && command.Result.Contains("out"))
        {
            logger.LogInformation(
                $"Successfully created directories for AFL++ fuzzer: " +
                $"{Path.Combine(rootPath, $"task{taskId}-afl")}");
            return true;
        }
        else
        {
            logger.LogError($"Error while creating directories for AFL++ fuzzer: " +
                            $"{command.Result}. ");
            return false;
        }
    }
    
    /// <summary>
    /// Создает директории для фаззера libFuzzer
    /// </summary>
    /// <param name="node">Нода, где создавать</param>
    /// <param name="taskId">Идентификатор задачи</param>
    /// <param name="rootPath">Пусть к корню общей папки</param>
    /// <param name="logger">Статический объект логгера</param>
    /// <returns>
    /// true - директории созданы успешно;
    /// false - директории не созданы
    /// </returns>
    public static async Task<bool> CreateDirectoriesLibFuzzer(
        ClusterConfigurationModel node,
        int taskId,
        string rootPath,
        ILogger logger)
    {
        string host = node.IpAddress;
        string user = node.Username;
        string password = node.Password;

        logger.LogDebug($"Creating directories for libFuzzer fuzzing");
        var sshClient = new SshClient(host, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        
        // Create directories
        string cmd =
            $"mkdir {Path.Combine(rootPath, $"task{taskId}-libfuzzer/")} " +
            $"&& mkdir {Path.Combine(rootPath, $"task{taskId}-libfuzzer/", $"in")}";
        logger.LogDebug($"Executing command on {node.IpAddress}: {cmd}");
        SshCommand command = sshClient.RunCommand(cmd);
        
        // Check directories
        cmd = $"ls {Path.Combine(rootPath, $"task{taskId}-libfuzzer/")}";
        logger.LogDebug($"Executing command on {node.IpAddress}: {cmd}");
        command = sshClient.RunCommand(cmd);

        if (command.Result.Contains("in"))
        {
            logger.LogInformation(
                $"Successfully created directories for libFuzzer fuzzer: " +
                $"{Path.Combine(rootPath, $"task{taskId}-libfuzzer")}");
            return true;
        }
        else
        {
            logger.LogError($"Error while creating directories for libFuzzer fuzzer: " +
                            $"{command.Result}. ");
            return false;
        }
    }

    public static async Task<bool> CopyBuildToTaskDir(
        ClusterConfigurationModel node,
        FuzzingTaskModel task,
        string rootPath,
        string fuzzer,
        ILogger logger)
    {
        string host = node.IpAddress;
        string user = node.Username;
        string password = node.Password;

        string src = Path.Combine(
            $"{task.UploadFileSettingsModel.FilePath}",
            $"{task.UploadFileSettingsModel.InternalName}");
        string dst = Path.Combine(
            rootPath,
            $"task{task.Id}-{fuzzer}/",
            task.UploadFileSettingsModel.InternalName);
        logger.LogDebug($"Copy build {src} to {dst}");
        var scpClient = new ScpClient(host, user, password);
        await scpClient.ConnectAsync(default(CancellationToken));
        scpClient.Upload(new FileInfo(src), dst);
        scpClient.Disconnect();
        
        // Check build on remote
        var sshClient = new SshClient(host, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        string cmd = $"ls -l {dst}";
        SshCommand command= sshClient.RunCommand(cmd);
        if (command.ExitStatus == 0)
        {
            logger.LogDebug("File successfully uploaded");
            return true;
        }
        else
        {
            logger.LogError($"Can not access uploaded file: {command.Result}");
            return false;
        }
    }

    public static async Task<bool> DeleteTask(
        ClusterConfigurationModel node,
        FuzzingTaskModel task,
        string rootPath,
        string fuzzer,
        ILogger logger)
    {
        string host = node.IpAddress;
        string user = node.Username;
        string password = node.Password;
        string dst = Path.Combine(
            rootPath,
            $"task{task.Id}-{fuzzer}/");
        
        var sshClient = new SshClient(host, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        string cmd = $"rm -rf {dst}";
        SshCommand command= sshClient.RunCommand(cmd);
        if (command.ExitStatus == 0)
        {
            logger.LogDebug("Task successfully deleted");
            return true;
        }
        else
        {
            logger.LogError($"Can not delete task: {command.Result}");
            return false;
        }
    }
}
