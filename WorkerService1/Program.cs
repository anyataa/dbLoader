using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Serilog;
using Microsoft.Extensions.Configuration;

namespace WorkerService1
{
    public class Program

    {
    

        public static void Main(string[] args)
        {

            var configuration = new ConfigurationBuilder()
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
             .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(configuration["LogConfiguration:LogSaveDestination"])
                .CreateLogger();

            try
            {
                Log.Information("Starting up the service");
                CreateHostBuilder(args).Build().Run();
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "There was a probelem starting this service");
            }
            finally
            {
                Log.CloseAndFlush();
            }

      
   
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()

                    .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                })
            .UseSerilog();


    }
}
