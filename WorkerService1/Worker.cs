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
        private readonly IConfiguration _configuration;
        private readonly Customer customer;
        private readonly MessageString messageString;
        private readonly Transaction transaction;


        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            transaction = new Transaction(logger, configuration);
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int upatePerSeconds = int.Parse(_configuration["DataConfig:UpdateTimePerSeconds"]);
            while (true)
            {
                await transaction.mapMessageConfig();
                await Task.Delay(upatePerSeconds*1000);

            }
        }
    }
}
