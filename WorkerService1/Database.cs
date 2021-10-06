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
        public string returnBU(int NumberBU)
        {
            return _configuration[$"DataConfig:BU{NumberBU}"];
        }

        public string returnLimitData()
        {
            return _configuration["DataConfig:LimitData"];
        }
        public int returnTotalDb()
        {
            return int.Parse(_configuration["DataConfig:TotalDB"]);
        }
    }
}
