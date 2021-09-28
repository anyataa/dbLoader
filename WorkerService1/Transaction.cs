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
        private string toTable = "MESSAGE_TRANSACTION2";
        private string configTable = "configuration_database";
        private string fromTable = "TABLE_TRANSACTION2";
        private string bu = "DBLOADER";


        //Parameter
        private string paramTable = "param_table";
        private int currentId = 0;
        private DateTime current_timestamp;
        private int paramItemIndex = 0;


        public Transaction(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
           
        }

        private string returnOracleDB()
        {
            return _configuration["ConnectionStrings:OracleDBConnection"];
        }
        private string returnSqlDB()
        {
            return _configuration["ConnectionStrings:SqlServerDBConnection"];
        }

        public async Task mapMessageConfig()
        {

            con.ConnectionString = returnOracleDB();
            sqlReaderCon.ConnectionString = returnSqlDB();
            sqlInsertCon.ConnectionString = returnSqlDB();

            getOracleDataSource();

        }

        public int getIndexCreatedTime()
        {
            int index = 0;
            for (int i = 0; i < getListSource().ToArray().Length; i++)
            {
                if (getListSource()[i] == "CREATED_TIME")
                {
                    index = i;
                }

            };
            return index;
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
                                               break;

                    case "DATE":
                        _logger.LogInformation($" Date => TYPE = {configReader.GetString(3)}");
                       
                        break;

                    case "INT32":
                        _logger.LogInformation($"Integer => TYPE = {configReader.GetString(3)}");
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
            string listData = "";
            string updateLastTimestamp;


            getListDestination().ForEach(delegate (string item)
                {
                    insertParam = insertParam + item + ",";
                    insertValue = insertValue + "@" + item + ",";
                });

            string[] destinationArray =  getListDestination().ToArray();
            insertParam = insertParam.Substring(0, insertParam.Length - 1);
            insertValue = insertValue.Substring(0, insertValue.Length - 1);
            _logger.LogInformation(insertValue);

       

            getListSource().ForEach(delegate (string item)
            {
                selectDataSource = selectDataSource + item + "," ;
            });

            selectDataSource = selectDataSource.Substring(0, selectDataSource.Length - 1);

            string lastUpdate =  getParam();

            string selectOracleQuery = @$"select {selectDataSource} from {fromTable} 
                                WHERE CREATED_TIME > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3')
                                FETCH NEXT {limitData} ROWS ONLY";

            cmd.CommandText = selectOracleQuery;
            _logger.LogInformation(selectOracleQuery);
            con.Open();
            OracleDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                    for (int i = 0; i < sourceArray.Length; i++)
                    {

                        switch (getListDatatype()[i])
                        {
                            case "STRING":
                             _logger.LogInformation($"STRING => TYPE = {reader.GetString(i)}");
                             listData = listData + $"'{reader.GetString(i)}'"+","; 
                             break;

                            case "DATE":
                            _logger.LogInformation($"DATE => TYPE = {reader.GetString(i)}");
                            listData = listData + $"CONVERT(date,'{reader.GetString(i)}',111)" + ",";
                            break;

                            case "DATETIME":
                            _logger.LogInformation($"DATETIME => TYPE = {reader.GetDateTime(i).ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                            listData = listData + $"CAST('{reader.GetDateTime(i).ToString("yyyy-MM-dd hh:mm:ss.fff")}' AS datetime2)" + ",";
                            break;

                            case "INT32":
                            _logger.LogInformation($"INT32 => TYPE = {reader.GetString(i)}");
                            listData = listData + $"{reader.GetString(i)}" + ",";
                            break;
                        }
                    }
               
                string insertData = $"INSERT INTO {toTable} ({insertParam+$", INSERTED_TIME"}) values ({listData + $"CONVERT(datetime2,'{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}')"})";
                SqlCommand insertCmd = new SqlCommand(insertData, sqlInsertCon);
                StringBuilder errorMessages = new StringBuilder();
                
                    try
                    {
                    sqlInsertCon.Open();
                    insertCmd.ExecuteNonQuery();
                    sqlInsertCon.Close();
                    updateLastTimestamp = reader.GetDateTime(getIndexCreatedTime()).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    updateParam(bu, updateLastTimestamp);
                    listData = "";     
                    }
                    catch (SqlException ex)
                    {
                        for (int i = 0; i < ex.Errors.Count; i++)
                        {
                            errorMessages.Append("Index #" + i + "\n" +
                                "Message: " + ex.Errors[i].Message + "\n" +
                                "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                                "Source: " + ex.Errors[i].Source + "\n" +
                                "Procedure: " + ex.Errors[i].Procedure + "\n");
                        }
                        _logger.LogInformation(errorMessages.ToString());
                    }

                    _logger.LogInformation($"{insertData}");        
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
                FROM {configTable} WHERE BU = '{bu}' ";

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
                destination.Add(configReader.GetString(2));
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

        public void updateParam(string BU, string timeStampParam) {
            string updateCmdSql = @$"UPDATE param_table 
                                     SET TIMESTAMP_PARAM = CAST('{timeStampParam}' AS Datetime2)  
                                     WHERE BU = '{bu}'";

            SqlCommand cmd = new SqlCommand(updateCmdSql,sqlInsertCon);
            StringBuilder errorMessages = new StringBuilder();
            _logger.LogInformation($"{updateCmdSql}");
            try
            {
                sqlInsertCon.Open();
                cmd.ExecuteNonQuery();
                sqlInsertCon.Close();
            }
            catch (SqlException ex)
            {
                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                _logger.LogInformation(errorMessages.ToString());
            }
           
        }

        public string getParam() {
            StringBuilder errorMessages = new StringBuilder();
            string lastUpdate = "";
            string getCmdSql = @$"SELECT TIMESTAMP_PARAM
                               FROM {paramTable}";

            SqlCommand cmd = new SqlCommand(getCmdSql, sqlReaderCon);

            try
            {
                sqlReaderCon.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lastUpdate = reader.GetDateTime(0).ToString("dd-MMM-yyyy HH:mm:ss.fff");
                    _logger.LogInformation($"PARAM : {reader.GetDateTime(0).ToString("dd-MMM-yyyy hh:mm:ss.fff")}");
                }
                sqlReaderCon.Close();
                return lastUpdate;
            }
            catch (SqlException ex)
            {
                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }
                _logger.LogInformation(errorMessages.ToString());
                return "Error";
            }

        }
    }
}
