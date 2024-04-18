namespace ApSafeFuzz;

public static class LogHelper
{
    public static ILoggerFactory LoggerFactory { get; set; }

    public static ILogger<T> CreateLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }
}
