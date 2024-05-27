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
}
