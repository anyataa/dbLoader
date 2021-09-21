using System;
using System.Collections.Generic;
using System.Text;
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
using Microsoft.Data.SqlClient;

namespace WorkerService1
{
    class Customer
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        OracleConnection con = new OracleConnection();
        SqlConnection scon = new SqlConnection();
        private Config config = new Config();
        public Customer(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task mapCustomer() {
           
            con.ConnectionString = _configuration["ConnectionStrings:OracleDBConnection"];
            con.Open();
            scon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];
            scon.Open();

            string limitData = _configuration["DataConfig:LimitData"];
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = $"select id, f_name, l_name, insert_time  from CUSTOMER fetch first {limitData} rows only";
            OracleDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt16(0);
                string f_name = reader.GetString(1);
                string l_name = reader.GetString(2);
                DateTime insert_time = reader.GetDateTime(3);
                SqlCommand insert = new SqlCommand($"insert into trans_all(created_ts,a,b,d) values ( '{insert_time}' ,'{f_name}', '{l_name}', {id})", scon);
               insert.ExecuteNonQuery();
                _logger.LogInformation("Number of Data fetched : " + id + " " + f_name + " "+ l_name + " "+ insert_time, DateTimeOffset.Now);
                _logger.LogInformation("Worker running at: {time} ", DateTimeOffset.Now);
                await Task.Delay(1000);
            }
        }
       
    }
}
