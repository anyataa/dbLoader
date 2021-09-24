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

    class Transaction
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private OracleConnection con = new OracleConnection();
        private SqlConnection sqlReaderCon = new SqlConnection();
        private SqlConnection sqlInsertCon = new SqlConnection();
        private Config config = new Config();
        private string toTable = "MESSAGE_TRANSACTION";
        private string configTable = "configuration_database";
        private string fromTable = "TABLE_TRANSACTION2";
        private string bu = "DBLOADER";
        private string configurationList;
        private string logMessage;
        private string insertDataDynamic;
        private string logDataDynamic;
        


        public Transaction(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
           
        }

        public async Task mapMessageConfig()
        {
           
            con.ConnectionString = _configuration["ConnectionStrings:OracleDBConnection"];
            sqlReaderCon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];
            sqlInsertCon.ConnectionString = _configuration["ConnectionStrings:SqlServerDBConnection"];

            getOracleDataSource();
        }

        public async void readConfig(OracleDataReader reader ) 
        {
            //Configuration 
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"select 
                ID,
                SOURCE,
                DESTINATION,
                DATATYPE,
                BU, 
                TABLE_SOURCE,  
                TABLE_DESTINATION 
                FROM {configTable} WHERE BU = '{bu}'";

            SqlDataReader configReader = configCommand.ExecuteReader();
            while (configReader.Read())
            {

                switch (configReader.GetString(3))
                {
                    case "STRING":
                        _logger.LogInformation($"String => TYPE = {configReader.GetString(3)}");
                        //reader.GetString(i);
                        break;

                    case "DATE":
                        _logger.LogInformation($" Date => TYPE = {configReader.GetString(3)}");
                        //reader.GetDateTime(i);
                        break;

                    case "INT32":
                        _logger.LogInformation($"Integer => TYPE = {configReader.GetString(3)}");
                        //reader.GetInt32(i);
                        break;
                }

            }
            sqlReaderCon.Close();
            await Task.Delay(1000);
        }

      

        public  void getOracleDataSource() {
           
            OracleCommand cmd = con.CreateCommand();
            string limitData = _configuration["DataConfig:LimitData"];
            string[] sourceArray = getListSource().ToArray();
            string selectDataSource = "";

            //set insert based on configuration 
            string insertParam = "";
            string insertValue = "";

            getListDestination().ForEach(delegate (string item)
                {
                    insertParam = insertParam + item + ",";
                    insertValue = insertValue + "@" + item + ",";
                });

            string[] destinationArray =  getListDestination().ToArray();
            insertParam = insertParam.Substring(0, insertParam.Length - 1);
            insertValue = insertValue.Substring(0, insertValue.Length - 1);
            _logger.LogInformation(insertValue);

            string insertData = $"INSERT INTO {toTable} ({insertParam}) values ({insertValue})";
            //using(SqlCommand insertCmd = new SqlCommand(insertParam, sqlInsertCon))
            //{
            //    for (int x = 0; x < destinationArray.Length; x++) 
            //    {
            //        switch (getListDatatype()[x])
            //        {
            //            case "STRING":
            //                _logger.LogInformation($" {destinationArray[x]} = {getListDatatype()[x]}");
            //                insertCmd.Parameters.Add($"@{destinationArray}", SqlDbType.VarChar, 50);
            //                //reader.GetString(i);
            //                break;

            //            case "DATE":
            //                _logger.LogInformation($"{destinationArray[x]} = {getListDatatype()[x]}");
            //                insertCmd.Parameters.Add($"@{destinationArray}", SqlDbType.DateTime);
                            
            //                //reader.GetDateTime(i);
            //                break;

            //            case "INT32":
            //                _logger.LogInformation($"{destinationArray[x]} = {getListDatatype()[x]}");
            //                insertCmd.Parameters.Add($"@{destinationArray}", SqlDbType.Int);
            //                //reader.GetInt32(i);
            //                break;
            //        }
                    
            //    }
            //}


            getListSource().ForEach(delegate (string item)
            {
                selectDataSource = selectDataSource + item + "," ;
            });
            selectDataSource = selectDataSource.Substring(0, selectDataSource.Length - 1);
            
            cmd.CommandText = $"select {selectDataSource} from {fromTable} fetch first {limitData} rows only";
            con.Open();
            OracleDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                using (SqlCommand insertCmd = new SqlCommand(insertData, sqlInsertCon))
                {
                    //Reader can get information
                    for (int i = 0; i < sourceArray.Length; i++)
                    {

                        switch (getListDatatype()[i])
                        {
                            case "STRING":
                                _logger.LogInformation($"String => TYPE = {reader.GetString(i)}");
                                insertCmd.Parameters.AddWithValue($"@{destinationArray[i]}", reader.GetString(i));
                                //reader.GetString(i);
                                break;

                            case "DATE":
                                _logger.LogInformation($" Date => TYPE = {reader.GetDateTime(i)}");
                                insertCmd.Parameters.AddWithValue($"@{destinationArray[i]}", reader.GetDateTime(i));
                                //reader.GetDateTime(i);
                                break;

                            case "INT32":
                                _logger.LogInformation($"Integer => TYPE = {reader.GetInt32(i)}");
                                insertCmd.Parameters.AddWithValue($"@{destinationArray[i]}", reader.GetInt32(i));
                                //reader.GetInt32(i);
                                break;
                        }
                    }
                    sqlInsertCon.Open();
                    insertCmd.ExecuteNonQuery();
                    sqlInsertCon.Close();
                    //_logger.LogInformation($"{insertData}");
                }
                 
            }
            con.Close();
        }

        public string[] configSourceDestination() {
            string destination = "";
            string source = "";

            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"SELECT 
                ID,
                SOURCE,
                DESTINATION,
                DATATYPE,
                BU, 
                TABLE_SOURCE,  
                TABLE_DESTINATION 
                FROM {configTable} WHERE BU = '{bu}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read()) {
                source = source + configReader.GetString(1) + ",";
                destination = destination + configReader.GetString(2) + ",";
            }
            sqlReaderCon.Close();
            source = source.Substring(0, (source.Length - 1));
            destination = destination.Substring(0, (destination.Length - 1));
            return new string[] { source, destination };
        }

        public List<string> getListSource()
        {

            List<string> sources = new List<string>();
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"SELECT 
                ID,
                SOURCE,
                DESTINATION,
                DATATYPE,
                BU, 
                TABLE_SOURCE,  
                TABLE_DESTINATION 
                FROM {configTable} WHERE BU = '{bu}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                sources.Add(configReader.GetString(1));
            }
            sqlReaderCon.Close();
            return sources;
        }
        public List<string> getListDestination()
        {

            List<string> destination = new List<string>();
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"SELECT 
                ID,
                SOURCE,
                DESTINATION,
                DATATYPE,
                BU, 
                TABLE_SOURCE,  
                TABLE_DESTINATION 
                FROM {configTable} WHERE BU = '{bu}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                destination.Add(configReader.GetString(1));
            }
            sqlReaderCon.Close();
            return destination;
        }

        public List<string> getListDatatype()
        {

            List<string> dataType = new List<string>();
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"SELECT 
                ID,
                SOURCE,
                DESTINATION,
                DATATYPE,
                BU, 
                TABLE_SOURCE,  
                TABLE_DESTINATION 
                FROM {configTable} WHERE BU = '{bu}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                dataType.Add(configReader.GetString(3));
            }
            sqlReaderCon.Close();
            return dataType;
        }


    }
}
