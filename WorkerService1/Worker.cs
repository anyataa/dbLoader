using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;


namespace WorkerService1
{
    public class Worker : BackgroundService
    {
        private Customer customer ;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            customer = new Customer(logger, configuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
           
            await customer.mapCustomer();

        }
    }
}
