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
using System.Data;


namespace WorkerService1
{
    class Customer
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        OracleConnection con = new OracleConnection();
        SqlConnection scon = new SqlConnection();
        private Config config = new Config();
        private int fetchPerSeconds = 300;
        private string fromTable = "customer";
        private string toTable = "bu_table";
       

        private int id;
        private string f_name;
        private string l_name;
        private DateTime insert_time;
        
        public Customer(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task mapCustomer() {
           
            con.ConnectionString = _configuration["ConnectionStrings:OracleDBConnection"];
            con.Open();
            scon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];
  

            string limitData = _configuration["DataConfig:LimitData"];
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = $"select id, f_name, l_name, insert_time  from CUSTOMER fetch first {limitData} rows only";
            OracleDataReader reader = cmd.ExecuteReader();
            string InsertData = $"insert into {toTable}(BU,created_time,inserted_time,item_id, a,b) values ( @BU2, @createdTime2 , @insertedTime2 , @itemId2, @fName2, @lName2)";
            using (SqlCommand insertCmd = new SqlCommand(InsertData, scon)) {
                insertCmd.Parameters.Add("@BU2", SqlDbType.VarChar, 50);
                insertCmd.Parameters.Add("@createdTime2", SqlDbType.DateTime);
                insertCmd.Parameters.Add("@insertedTime2", SqlDbType.DateTime);
                insertCmd.Parameters.Add("@itemId2", SqlDbType.BigInt);
                insertCmd.Parameters.Add("@fName2", SqlDbType.VarChar, 100);
                insertCmd.Parameters.Add("@lName2", SqlDbType.VarChar, 100);
                scon.Open();
                while (reader.Read())
                {
                    insertCmd.Parameters["@BU2"].Value = fromTable;
                    insertCmd.Parameters["@createdTime2"].Value = reader.GetDateTime(3);
                    insertCmd.Parameters["@insertedTime2"].Value = DateTime.Now;
                    insertCmd.Parameters["@itemId2"].Value = reader.GetInt16(0);
                    insertCmd.Parameters["@fName2"].Value = reader.GetString(1);
                    insertCmd.Parameters["@lName2"].Value = reader.GetString(2);
                    insertCmd.ExecuteNonQuery();
                    _logger.LogInformation("Number of Data fetched : " + id + " " + f_name + " " + l_name + " " + insert_time, DateTime.Now);
                    await Task.Delay(1000);
                }
                scon.Close();
            }

            //while (reader.Read())
            //{
            //    id = reader.GetInt16(0);
            //    f_name = reader.GetString(1);
            //    l_name = reader.GetString(2);
            //    insert_time = reader.GetDateTime(3);
            //    SqlCommand insert = new SqlCommand($"insert into {toTable}(BU,created_time,inserted_time,a,b,d) values ( '{fromTable}','{insert_time}' , '{DateTime.Now}' , '{f_name}', '{l_name}', {id})", scon);
            //   insert.ExecuteNonQuery();
            //    _logger.LogInformation("Number of Data fetched : " + id + " " + f_name + " "+ l_name + " "+ insert_time, DateTimeOffset.Now);
            //    _logger.LogInformation("Worker running at: {time} ", DateTimeOffset.Now);
            //    await Task.Delay(1000);
            //}
        }
       
    }
}
