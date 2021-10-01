using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using log4net;
using log4net.Repository;
using System.IO;
using System.Reflection;

namespace WorkerService1
{
    public class Program
        
    {
        
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();


        }
    

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddDebug();
                    logging.AddConsole();

                })


                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                });
    }
}
