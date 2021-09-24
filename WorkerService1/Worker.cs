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
        private readonly ILogger<Worker> _logger;
        private readonly Customer customer;
        private readonly MessageString messageString;
        private readonly Transaction transaction;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            customer = new Customer(logger, configuration);
            messageString = new MessageString(logger, configuration);
            transaction = new Transaction(logger, configuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int holdNum = 0;
            while (true)
            {
                //await customer.mapCustomer();
                //holdNum = holdNum + 1;
                //_logger.LogInformation($"{holdNum}");
                
                 transaction.mapMessageConfig();
                await Task.Delay(1000);

            }
        }
    }
}
