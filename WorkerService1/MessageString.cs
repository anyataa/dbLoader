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

    class MessageString
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private OracleConnection con = new OracleConnection();
        private SqlConnection scon = new SqlConnection();
        private Config config = new Config();
        private string toTable = "configApp";
        private string fromTable = "message_string_a";
        private int fixedLength = 21;
        private string configurationType = "FixedString";

        public MessageString(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task mapMessageConfig()
        {
            int lastInsertedId;

            con.ConnectionString = _configuration["ConnectionStrings:OracleDBConnection"];
            con.Open();
            scon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];

            string limitData = _configuration["DataConfig:LimitData"];
            OracleCommand cmd = con.CreateCommand();
            
            cmd.CommandText = $"select id, created_ts, message  from message_string_a fetch first {limitData} rows only";

            OracleDataReader reader = cmd.ExecuteReader();

            string InsertData = $"insert into {toTable}(BU,created_time,inserted_time,item_id, a,b) values ( @BU2, @createdTime2 , @insertedTime2 , @itemId2, @fName2, @lName2)";
            using (SqlCommand insertCmd = new SqlCommand(InsertData, scon))
            {
                scon.Open();
                while (reader.Read())
                {
                    lastInsertedId = reader.GetInt16(0);
                    //insertCmd.ExecuteNonQuery();
                    _logger.LogInformation($"ID: {reader.GetInt16(0)} \nCreated Date: {reader.GetString(1)} \nmessage {reader.GetString(2)}");
                    await Task.Delay(1000);
                }
                scon.Close();
                con.Close();
            } 


              }
        }
    }
