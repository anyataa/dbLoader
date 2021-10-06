using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace WorkerService1
{
    class Database
    {
        private readonly IConfiguration _configuration;

        public Database(IConfiguration configuration) {
            _configuration = configuration;
        }

       public string returnOracleDB()
        {
            return _configuration["ConnectionStrings:OracleDBConnection"];
        }
        public string returnSqlDB()
        {
            return _configuration["ConnectionStrings:SqlServerDBConnection"];
        }
    }
}
