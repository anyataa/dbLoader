using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using log4net;
using System.IO;
using System.Reflection;
using log4net.Config;

namespace WorkerService1
{
    public class Program
        
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

            // Load log4net configuration
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
          

            // Log some things
            //log.Info("Hello logging world!");
            //log.Error("Error!");
            //log.Warn("Warn!");

            Console.ReadLine();


        }
    

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
            
                    .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                });
    }
}
