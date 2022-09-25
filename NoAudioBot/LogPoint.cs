using Serilog;

namespace NoAudioBot;

public class LogPoint
{
    static LogPoint()
    {
        var configuration = Configure();

        Log.Logger = configuration.CreateLogger();
    }
    private static LoggerConfiguration Configure()
    {
        var template = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] — {TypeName} — {Message}{NewLine}{Exception}";
        
        var configuration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: template)
            .WriteTo.File("logs/logs.txt", rollingInterval: RollingInterval.Month, outputTemplate: template);


        configuration.MinimumLevel.Information();
        
        return configuration;
    }
    
    public static ILogger GetLogger<T>()
    {
        return Log.Logger.ForContext("TypeName", typeof(T).Name, true);
    }
}