using System.Globalization;
using System.Net.Sockets;
using ApSafeFuzz.Models;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace ApSafeFuzz;

public static class SSHExecutor
{
    /// <summary>
    /// Метод для проверки доступности хоста по SSH
    /// </summary>
    /// <param name="host">Хост</param>
    /// <param name="logger">Статический объект логгера</param>
    /// <returns>
    /// true - если нода доступна для подключения по SSH;
    /// false - в остальных случаях.
    /// </returns>
    public static async Task<bool> PingNode(Models.HostModel host, ILogger logger)
    {
        string ip = host.IpAddress;
        string user = host.Username;
        string password = host.Password;
        
        logger.LogDebug($"Pinging node {user}@{ip} ({password})");
        var sshClient = new SshClient(ip, user, password);
        try
        {
            await sshClient.ConnectAsync(default(CancellationToken));
            sshClient.Disconnect();
            logger.LogDebug($"Ping node {user}@{ip} successfully");
            return true;
        }
        catch (SshConnectionException e)
        {
            logger.LogError($"SSH connection exception with {user}@{ip} ({password}): {e}");
            return false;
        }
        catch (SocketException e)
        {
            logger.LogError($"Socket exception with {user}@{ip} ({password}): {e}");
            return false;
        }
    }

    /// <summary>
    /// Создает директории для фаззера AFL++
    /// </summary>
    /// <param name="host">Нода, где создавать</param>
    /// <param name="taskId">Идентификатор задачи</param>
    /// <param name="rootPath">Пусть к корню общей папки</param>
    /// <param name="logger">Статический объект логгера</param>
    /// <returns>
    /// true - директории созданы успешно;
    /// false - директории не созданы
    /// </returns>
    public static async Task<bool> CreateDirectoriesAFL(HostModel host,
        int taskId,
        string rootPath,
        ILogger logger)
    {
        string ip = host.IpAddress;
        string user = host.Username;
        string password = host.Password;

        logger.LogDebug($"Creating directories for AFL fuzzing");
        var sshClient = new SshClient(ip, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        
        // Create directories
        string cmd =
            $"mkdir {Path.Combine(rootPath, $"task{taskId}-afl/")} " +
            $"&& mkdir {Path.Combine(rootPath, $"task{taskId}-afl/", "in")} " +
            $"&& mkdir {Path.Combine(rootPath, $"task{taskId}-afl/", $"out")} " +
            $"&& echo AAAAA > {Path.Combine(rootPath, $"task{taskId}-afl/", "in/", "1.txt")}";
        logger.LogDebug($"Executing command on {host.IpAddress}: {cmd}");
        SshCommand command = sshClient.RunCommand(cmd);
        
        // Check directories
        cmd = $"ls {Path.Combine(rootPath, $"task{taskId}-afl/")}";
        logger.LogDebug($"Executing command on {host.IpAddress}: {cmd}");
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
    /// <param name="host">Нода, где создавать</param>
    /// <param name="taskId">Идентификатор задачи</param>
    /// <param name="rootPath">Пусть к корню общей папки</param>
    /// <param name="logger">Статический объект логгера</param>
    /// <returns>
    /// true - директории созданы успешно;
    /// false - директории не созданы
    /// </returns>
    public static async Task<bool> CreateDirectoriesLibFuzzer(
        Models.HostModel host,
        int taskId,
        string rootPath,
        ILogger logger)
    {
        string ip = host.IpAddress;
        string user = host.Username;
        string password = host.Password;

        logger.LogDebug($"Creating directories for libFuzzer fuzzing");
        var sshClient = new SshClient(ip, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        
        // Create directories
        string cmd =
            $"mkdir {Path.Combine(rootPath, $"task{taskId}-libfuzzer/")} " +
            $"&& mkdir {Path.Combine(rootPath, $"task{taskId}-libfuzzer/", $"in")}";
        logger.LogDebug($"Executing command on {host.IpAddress}: {cmd}");
        SshCommand command = sshClient.RunCommand(cmd);
        
        // Check directories
        cmd = $"ls {Path.Combine(rootPath, $"task{taskId}-libfuzzer/")}";
        logger.LogDebug($"Executing command on {host.IpAddress}: {cmd}");
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

    public static async Task<bool> CopyBuildToTaskDir(HostModel host,
        FuzzingTaskModel task,
        string rootPath,
        ILogger logger)
    {
        string ip = host.IpAddress;
        string user = host.Username;
        string password = host.Password;
        string fuzzer;

        if (task.Fuzzer == "libFuzzer")
        {
            fuzzer = "libfuzzer";
        }
        else if (task.Fuzzer == "AFL++")
        {
            fuzzer = "afl";
        }
        else
        {
            fuzzer = "afl";
        }

        string src = Path.Combine(
            $"{task.UploadFileSettingsModel.FilePath}",
            $"{task.UploadFileSettingsModel.InternalName}");
        string dst = Path.Combine(
            rootPath,
            $"task{task.Id}-{fuzzer}/",
            task.UploadFileSettingsModel.InternalName);
        
        logger.LogDebug($"Copy build {src} to {dst}");
        
        var scpClient = new ScpClient(ip, user, password);
        await scpClient.ConnectAsync(default(CancellationToken));
        scpClient.Upload(new FileInfo(src), dst);
        scpClient.Disconnect();
        
        // Check build on remote
        var sshClient = new SshClient(ip, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        string cmd = $"chmod +x {dst} && ls -l {dst}";
        SshCommand command= sshClient.RunCommand(cmd);
        
        if (command.ExitStatus == 0)
        {
            logger.LogDebug("File successfully uploaded");
            return true;
        }
        else
        {
            logger.LogError($"Can not access uploaded file: {command.Result}");
            logger.LogError($"Command: {command.CommandText}");
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

    public static async Task<int> RunTask(
        ClusterConfigurationModel node,
        FuzzingTaskModel task,
        string rootPath,
        ILogger logger)
    {
        string taskPath;
        string buildPath;
        string inPath;
        string outPath;
        string pidPath;
        string fuzzCmd;
        string runCmd;
        
        
        // Check node availability
        HostModel host = new HostModel()
        {
            IpAddress = node.IpAddress,
            Username = node.Username,
            Password = node.Password
        };
        bool result = await PingNode(host, logger);
        if (!result)
        {
            throw new SshException($"Can not ping node {host.IpAddress}");
        }
        
        // Define variables
        string ip = node.IpAddress;
        string user = node.Username;
        string password = node.Password;

        if (task.Fuzzer == "AFL++")
        {
            taskPath = Path.Combine(rootPath, $"task{task.Id}-afl/");
            buildPath = Path.Combine(rootPath, $"task{task.Id}-afl/", task.UploadFileSettingsModel.InternalName);
            inPath = Path.Combine(rootPath, $"task{task.Id}-afl/", "in/");
            outPath = Path.Combine(rootPath, $"task{task.Id}-afl/", "out/");
            pidPath = Path.Combine(taskPath, "fuzz.pid");
            fuzzCmd = $"afl-fuzz -i {inPath} -o {outPath} -M node{node.Id} -b 0 -- {buildPath}";
            runCmd = $"nohup {fuzzCmd} > /dev/null 2>&1 & echo $! > {pidPath}";
        }
        else if (task.Fuzzer == "libFuzzer")
        {
            taskPath = Path.Combine(rootPath, $"task{task.Id}-libfuzzer/");
            buildPath = Path.Combine(rootPath, $"task{task.Id}-libfuzzer/", task.UploadFileSettingsModel.InternalName);
            inPath = Path.Combine(rootPath, $"task{task.Id}-libfuzzer/", "in/");
            outPath = Path.Combine(rootPath, $"task{task.Id}-libfuzzer/", "output.log");
            pidPath = Path.Combine(taskPath, "fuzz.pid");
            fuzzCmd = $"{buildPath} {inPath}";
            runCmd = $"nohup {fuzzCmd} > {outPath} 2>&1 & echo $! > {pidPath}";
        }
        else
        {
            taskPath = Path.Combine(rootPath, $"task{task.Id}-afl/");
            buildPath = Path.Combine(rootPath, $"task{task.Id}-afl/", task.UploadFileSettingsModel.InternalName);
            inPath = Path.Combine(rootPath, $"task{task.Id}-afl/", "in/");
            outPath = Path.Combine(rootPath, $"task{task.Id}-afl/", "out/");
            pidPath = Path.Combine(taskPath, "fuzz.pid");
            fuzzCmd = $"afl-fuzz -i {inPath} -o {outPath} -M node{node.Id} -b 0 -- {buildPath}";
            runCmd = $"nohup {fuzzCmd} > /dev/null 2>&1 & echo $! > {pidPath}";
        }
        
        logger.LogDebug($"Connecting to node {user}@{ip}");
        
        // Run fuzzer
        var sshClient = new SshClient(ip, user, password);
        await sshClient.ConnectAsync(default(CancellationToken));
        logger.LogDebug($"Command to execute: {runCmd}");
        SshCommand command = sshClient.RunCommand(runCmd);
        
        // Get PID
        command = sshClient.RunCommand($"cat {pidPath}");
        Int32.TryParse(command.Result, out int pid);
        
        sshClient.Disconnect();
        
        return pid;
    }

    public static async Task<bool> TaskInit(
        Models.HostModel host,
        FuzzingTaskModel task,
        string rootPath,
        ILogger logger)
    {
        bool result;
        
        // Check availability
        result = await PingNode(host, logger);
        if (!result)
        {
            return false;
        }
        
        // Create directory
        if (task.Fuzzer == "libFuzzer")
        {
            result = await CreateDirectoriesLibFuzzer(host, task.Id, rootPath, logger);
            if (!result)
            {
                return false;
            }
        }
        else if (task.Fuzzer == "AFL++")
        {
            result = await CreateDirectoriesAFL(host, task.Id, rootPath, logger);
            if (!result)
            {
                return false;
            }
        }
        else
        {
            return false;
        }
        
        // Copy to shared storage
        result = await CopyBuildToTaskDir(host, task, rootPath, logger);
        if (!result)
        {
            return false;
        }
        
        return true;
    }
}
