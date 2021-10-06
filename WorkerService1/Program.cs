using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using log4net;
using System.Reflection;

namespace WorkerService1
{
    public class Program
        
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
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
