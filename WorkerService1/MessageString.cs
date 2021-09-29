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
        private SqlConnection sqlReaderCon = new SqlConnection();
        private SqlConnection sqlInsertCon = new SqlConnection();
        private Config config = new Config();
        private string toTable = "Table_4";
        private string configTable = "configApp2";
        private string fromTable = "message_string_a";
        private int fixedLength = 19;
        private string configurationType = "FixedString";
        private string configurationList;
        private string logMessage;
        private string insertDataDynamic;
        private string logDataDynamic;
     

        public MessageString(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task mapMessageConfig()
        {
           
            con.ConnectionString = _configuration["ConnectionStrings:OracleDBConnection"];
            sqlReaderCon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];
            sqlInsertCon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];

            string limitData = _configuration["DataConfig:LimitData"];
            OracleCommand cmd = con.CreateCommand();

            con.Open();
            cmd.CommandText = $"select id, created_ts, message  from message_string_a fetch first {limitData} rows only";
            OracleDataReader reader = cmd.ExecuteReader();

            while (reader.Read()) {
                //Configuration 
                sqlReaderCon.Open();
                using (SqlCommand readConfig = new SqlCommand($"select BU,configuration_1,configuration_2, configuration_3 from {configTable}  where BU = '{fromTable}'", sqlReaderCon))
                {
                    SqlDataReader configReader = readConfig.ExecuteReader();
                    while (configReader.Read())
                    {
                        logMessage = reader.GetString(2);
                        insertDataDynamic = insertDataDynamic + configReader.GetString(3) + ",";
                        logDataDynamic = logDataDynamic + $"'{logMessage.Substring(configReader.GetInt32(1), (configReader.GetInt32(2) - configReader.GetInt32(1)))}'" + ",";
                        _logger.LogInformation($"LOG: {logMessage.Substring(configReader.GetInt32(1), (configReader.GetInt32(2) - configReader.GetInt32(1)))}");
                        _logger.LogInformation($"COLUMN: {insertDataDynamic}");
                        _logger.LogInformation($"DATA: {logDataDynamic}");
                        _logger.LogInformation($"{configReader.GetString(0)}, {configReader.GetInt32(1)}, {configReader.GetInt32(2)}, {configReader.GetString(3)}");
                        
                    }
                
                    if (logMessage.Length == fixedLength)
                    {
                        insertDataDynamic = insertDataDynamic.Substring(0, insertDataDynamic.Length-1);
                        logDataDynamic = logDataDynamic.Substring(0, logDataDynamic.Length - 1);

                        //string InsertData = $"insert into {toTable}(created_ts,inserted_ts,configuration_type, configuration_1,configuration_2, configuration_3, BU) values ( @createdTime , @insertedTime ,@configurationType, @configuration1, @configuration2, @configuration3, @BU)";
                        string InsertData = $"insert into {toTable}({insertDataDynamic}) values ({logDataDynamic})";
                        SqlCommand insert = new SqlCommand($"insert into {toTable}({insertDataDynamic}) values ({logDataDynamic})", sqlInsertCon);
                        sqlInsertCon.Open();

                        //insertCmd.ExecuteNonQuery();
                        //Prevent IN to insert.executeNonQuery
                        _logger.LogInformation($"substring :{insert.ExecuteNonQuery()} {logDataDynamic} \n {reader.GetString(2).Substring(0, 1)}\nID: {reader.GetInt16(0)} \nCreated Date: {reader.GetString(1)} \nmessage {reader.GetString(2)}");
                        insertDataDynamic = "";
                        logDataDynamic = "";
                        await Task.Delay(0);
                        
                        sqlReaderCon.Close();
                        sqlInsertCon.Close();
                        con.Close();

                        //using (SqlCommand insertCmd = new SqlCommand(InsertData, sqlInsertCon))
                        //{
                        //    //insertCmd.Parameters.Add("@createdTime", SqlDbType.DateTime);
                        //    //insertCmd.Parameters.Add("@insertedTime", SqlDbType.DateTime);
                        //    //insertCmd.Parameters.Add("@configurationType", SqlDbType.VarChar, 50);
                        //    //insertCmd.Parameters.Add("@configuration1", SqlDbType.Int);
                        //    //insertCmd.Parameters.Add("@configuration2", SqlDbType.Int);
                        //    //insertCmd.Parameters.Add("@configuration3", SqlDbType.VarChar, 50);
                        //    //insertCmd.Parameters.Add("@BU", SqlDbType.VarChar, 50);

                        //    //using (SqlCommand readConfig = new SqlCommand($"select BU,configuration_1,configuration_2, configuration_3 from {configTable}", sqlReaderCon))


                        //    //insertCmd.Parameters["@createdTime"].Value = reader.GetDateTime(1);
                        //    //insertCmd.Parameters["@insertedTime"].Value = DateTime.Now;
                        //    //insertCmd.Parameters["@configurationType"].Value = configurationType;
                        //    //insertCmd.Parameters["@configuration1"].Value = 11;
                        //    //insertCmd.Parameters["@configuration2"].Value = 19;
                        //    //insertCmd.Parameters["@configuration3"].Value = "transaction_nominal";
                        //    //insertCmd.Parameters["@BU"].Value = fromTable;

                           
                        //}
                       
                    }
                    else
                    {
                        _logger.LogInformation($"Not A Fixed Length! \nLength: {reader.GetString(2).Length} & Value: {reader.GetString(2)}");
                    }
                }
            }
            _logger.LogInformation($"FINISH");
           
            }
        }
    }
