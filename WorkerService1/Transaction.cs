using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Data.SqlClient;
using System.Reflection;
using log4net;
using log4net.Config;
using System.IO;

namespace WorkerService1
{

    class Transaction
    {
        //private readonly ILogger<Worker> _logger;
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IConfiguration _configuration;
        private OracleConnection con = new OracleConnection();
        private SqlConnection sqlReaderCon = new SqlConnection();
        private SqlConnection sqlInsertCon = new SqlConnection();
        private Config config = new Config();
        private Database dataBase;

        public Transaction(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            dataBase = new Database(configuration);
            //_logger = logger;

        }

        //private string returnOracleDB()
        //{
        //    return _configuration["ConnectionStrings:OracleDBConnection"];
        //}
        //private string returnSqlDB()
        //{
        //    return _configuration["ConnectionStrings:SqlServerDBConnection"];
        //}

        private string returnBU(int NumberBU)
        {
            return _configuration[$"DataConfig:BU{NumberBU}"];
        }

        private string returnLimitData() 
        {
            return _configuration["DataConfig:LimitData"];
        }
        private string returnConfigTableName()
        {
            return _configuration["DataConfig:ConfigTableName"];
        }

        private int returnTotalDb() {
            return int.Parse(_configuration["DataConfig:TotalDB"]);         
        }

        private string returnParameterType(int parameterNumber) 
        { 
        return _configuration[$"DataConfig:Parameter{parameterNumber}"];
        }

        private List<string> returnParameterColumnName(int parameterNumber) {

            return new List<string> 
            { 
                _configuration[$"DataConfig:ParameterColumn1.{parameterNumber}"],
                _configuration[$"DataConfig:ParameterColumn2.{parameterNumber}"]
            };
        }

        public async Task mapMessageConfig()
        {

            log4NetConfiguration();
            con.ConnectionString = dataBase.returnOracleDB();
            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            sqlInsertCon.ConnectionString = dataBase.returnSqlDB();


            //for (int i = 1; i <= returnTotalDb(); i++)
            //{
            //    _logger.Info($"___________  Start processing from BU : {returnBU(i)} ________\n");
            //    //_logger.Info($"{returnParameterColumnName(i)[0]} {returnParameterColumnName(i)[1] }");

            //    mapOracleData(
            //        _configuration[$"DataConfig:SourceDB{i}"],
            //        _configuration[$"DataConfig:DestinationDB{i}"],
            //        i,
            //        returnParameterType(i),
            //        returnParameterColumnName(i)[0], 
            //        returnParameterColumnName(i)[1]
            //        );
            //}

            _logger.Info(dataBase.returnOracleDB());

        }

        public void log4NetConfiguration() {
            
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

   

        public  void mapOracleData(string sourceTable, string destinationTable, int numberBU, string parameter, string parameterColumn1, string parameterColumn2) {

            OracleCommand cmd = con.CreateCommand();
            string limitData = returnLimitData();
            string[] sourceArray = getListSource(numberBU).ToArray();
            string selectDataSource = "";

            //set insert based on configuration 
            string insertParam = "";
            string insertValue = "";
            string listData = "";
            //string updateLastTimestamp;


            getListDestination(numberBU).ForEach(delegate (string item)
                {
                    insertParam = insertParam + item + ",";
                    insertValue = insertValue + "@" + item + ",";
                });

            string[] destinationArray =  getListDestination(numberBU).ToArray();
            insertParam = insertParam.Substring(0, insertParam.Length - 1);
            insertValue = insertValue.Substring(0, insertValue.Length - 1);
       
            getListSource(numberBU).ForEach(delegate (string item)
            {
                selectDataSource = selectDataSource + item + "," ;
            });

            selectDataSource = selectDataSource.Substring(0, selectDataSource.Length - 1);

            string lastUpdate =  "";
            string lastUpdateOptional = "";

            string parameterCondition = "";

            switch (parameter)
            {
                case "TIMESTAMP_ONLY":
                    lastUpdate = getParam(numberBU, parameter)[0];
                    parameterCondition = $"{parameterColumn1} > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3')";
                    break;

                case "ID_ONLY":
                    lastUpdate = getParam(numberBU, parameter)[0];
                    parameterCondition = $"{parameterColumn1} > {lastUpdate}";
                    break;

                case "TIMESTAMP_ID":
                    lastUpdate = getParam(numberBU, parameter)[0];
                    lastUpdateOptional = getParam(numberBU, parameter)[1];
                    parameterCondition = $"{parameterColumn1} > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3') AND {parameterColumn2} != {lastUpdateOptional} ";
                    break;

                case "TIMESTAMP_NOMINAL":
                    lastUpdate = getParam(numberBU, parameter)[0];
                    lastUpdateOptional = getParam(numberBU, parameter)[1];

                    parameterCondition = $"{parameterColumn1} > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3') AND {parameterColumn2} != {lastUpdateOptional} ";

                    break;

                case "ID_NOMINAL":
                    lastUpdate = getParam(numberBU, parameter)[0];
                    lastUpdateOptional = getParam(numberBU, parameter)[1];

                    parameterCondition = $"{parameterColumn1} > {lastUpdate} AND {parameterColumn2} != {lastUpdateOptional}";

                    break;
            }
            
            string selectOracleQuery = @$"SELECT {selectDataSource} FROM {sourceTable} WHERE  {parameterCondition} FETCH NEXT {limitData} ROWS ONLY";

            cmd.CommandText = selectOracleQuery;
            _logger.Info(selectOracleQuery);
            con.Open();
            OracleDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                    for (int i = 0; i < sourceArray.Length; i++)
                    {

                        switch (getListDatatype(numberBU)[i])
                        {
                            case "STRING":
                             listData = listData + $"'{reader.GetString(i)}'"+","; 
                             break;

                            case "DATE":
                            listData = listData + $"CONVERT(date,'{reader.GetString(i)}',111)" + ",";
                            break;

                            case "DATETIME":
                            listData = listData + $"CAST('{reader.GetDateTime(i).ToString("yyyy-MM-dd hh:mm:ss.fff")}' AS datetime2)" + ",";
                            break;

                            case "NUMERIC":
                           listData = listData + $"{reader.GetString(i)}" + ",";
                            break;
                        }
                    }
               
                string insertData = $"INSERT INTO {destinationTable} ({insertParam+$", INSERTED_TIME"}) VALUES ({listData + $"CONVERT(datetime2,'{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}')"})";
                SqlCommand insertCmd = new SqlCommand(insertData, sqlInsertCon);
                StringBuilder errorMessages = new StringBuilder();
                
                    try
                    {
                    sqlInsertCon.Open();
                    insertCmd.ExecuteNonQuery();
                    sqlInsertCon.Close();

                    switch (parameter)
                    {
                        case "TIMESTAMP_ONLY":
                            lastUpdate = reader.GetDateTime(getIndexFor(numberBU, "CREATED_TIME")).ToString("yyyy-MM-dd HH:mm:ss.fff");
                            updateParam(lastUpdate,"", numberBU, parameter);
                            break;

                        case "ID_ONLY":
                            lastUpdate = reader.GetString(getIndexFor(numberBU, "ID"));
                            updateParam(lastUpdate, "", numberBU, parameter);
                            break;

                        case "TIMESTAMP_ID":
                            lastUpdate = reader.GetDateTime(getIndexFor(numberBU, "CREATED_TIME")).ToString("yyyy-MM-dd HH:mm:ss.fff");
                            lastUpdateOptional = reader.GetString(getIndexFor(numberBU, "ID"));
                            updateParam(lastUpdate, lastUpdateOptional, numberBU, parameter);
                            break;

                        case "TIMESTAMP_NOMINAL":
                            lastUpdate = reader.GetDateTime(getIndexFor(numberBU, "CREATED_TIME")).ToString("yyyy-MM-dd HH:mm:ss.fff");
                            lastUpdateOptional = reader.GetDecimal(getIndexFor(numberBU, "NOMINAL")).ToString();
                            updateParam(lastUpdate, lastUpdateOptional, numberBU, parameter);
                            break;

                        case "ID_NOMINAL":
                            lastUpdate = reader.GetString(getIndexFor(numberBU, "ID"));
                            lastUpdateOptional = reader.GetDecimal(getIndexFor(numberBU, "NOMINAL")).ToString();
                            updateParam(lastUpdate, lastUpdateOptional, numberBU, parameter);
                            break;
                    }
                    _logger.Info($"_____________  INSERTED SUCCESSFULLY  ______________\nINSERTED AT : {DateTime.Now}\nPARAMETER UPDATED : {lastUpdate} | {lastUpdateOptional}");
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
                        _logger.Error(errorMessages.ToString());
                    }

                    _logger.Info($"{insertData}");        
            }
            con.Close();
        }


        public int getIndexFor(int numberBU, string columnParam)
        {
            int index = 0;
            for (int i = 0; i < getListSource(numberBU).ToArray().Length; i++)
            {
                if (getListSource(numberBU)[i] == columnParam)
                {
                    index = i;
                }

            };
            return index;
        }

        public List<string> getListSource(int numberBU)
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
                FROM {returnConfigTableName()} WHERE BU = '{returnBU(numberBU)}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                sources.Add(configReader.GetString(1));
            }
            sqlReaderCon.Close();
            return sources;
        }
        public List<string> getListDestination(int numberBU)
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
                FROM {returnConfigTableName()} WHERE BU = '{returnBU(numberBU)}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                destination.Add(configReader.GetString(2));
            }
            sqlReaderCon.Close();
            return destination;
        }

        public List<string> getListDatatype(int numberBU)
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
                FROM {returnConfigTableName()} WHERE BU = '{returnBU(numberBU)}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                dataType.Add(configReader.GetString(3));
            }
            sqlReaderCon.Close();
            return dataType;
        }

        public void updateParam(string paramValue, string paramValueOptional, int numberBU, string parameter) {
            string parameter_table = _configuration["DataConfig:ParamTableName"];

            string setParameter = "";

            switch (parameter) 
            {   
                case "TIMESTAMP_ONLY":
                    setParameter = $"SET VAL_PARAM01 = CAST('{paramValue}' AS Datetime2)";
                    break;

                case "ID_ONLY":
                    setParameter = $"SET VAL_PARAM02 = {paramValue}";
                    break;

                case "TIMESTAMP_ID":
                    setParameter = $"SET VAL_PARAM01 = CAST('{paramValue}' AS Datetime2) , VAL_PARAM02 = {paramValueOptional}";
                    break;

                case "TIMESTAMP_NOMINAL":
                    setParameter = $"SET VAL_PARAM01 = CAST('{paramValue}' AS Datetime2) , VAL_PARAM03 = {paramValueOptional}";
                    break;

                case "ID_NOMINAL":
                    setParameter = $"SET VAL_PARAM02 = {paramValue}, VAL_PARAM03 = {paramValueOptional}";
                    break;
            }
            

            string updateCmdSql = @$"UPDATE {parameter_table} 
            {setParameter}  
            WHERE BU = '{returnBU(numberBU)}'";

            SqlCommand cmd = new SqlCommand(updateCmdSql,sqlInsertCon);
            StringBuilder errorMessages = new StringBuilder();
            //_logger.LogInformation($"{updateCmdSql}");
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
                _logger.Warn(errorMessages.ToString());
            }
        }

        public List<string> getParam(int numberBU, string parameter) {
            string parameter_table = _configuration["DataConfig:ParamTableName"];
            StringBuilder errorMessages = new StringBuilder();
            //string lastUpdate = "";
            string setParameter = "";
            List<string> lastUpdate = new List<string>();

            switch (parameter) {
                case "TIMESTAMP_ONLY":
                    setParameter = " VAL_PARAM01";
                    break;

                case "ID_ONLY":
                    setParameter = $"VAL_PARAM02";
                    break;

                case "TIMESTAMP_ID":
                    setParameter = $"VAL_PARAM01, VAL_PARAM02";
                    break;
                case "TIMESTAMP_NOMINAL":
                    setParameter = $"VAL_PARAM01, VAL_PARAM03";
                    break;

                case "ID_NOMINAL":
                    setParameter = $"VAL_PARAM02, VAL_PARAM03";
                    break;
            }

            string getCmdSql = @$"SELECT {setParameter}
                               FROM {parameter_table}
                               WHERE BU = '{returnBU(numberBU)}'";

            SqlCommand cmd = new SqlCommand(getCmdSql, sqlReaderCon);

            try
            {
                sqlReaderCon.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    switch (parameter)
                    {
                        case "TIMESTAMP_ONLY":
                            lastUpdate.Add(reader.GetDateTime(0).ToString("dd-MMM-yyyy HH:mm:ss.fff"));
                            //_logger.LogInformation($"PARAM TIMESTAMP_ONLY : {reader.GetDateTime(0).ToString("dd-MMM-yyyy hh:mm:ss.fff")}");
                            break;

                        case "ID_ONLY":
                            lastUpdate.Add(reader.GetDecimal(0).ToString());
                            break;

                        case "TIMESTAMP_ID":
                            lastUpdate.Add(reader.GetDateTime(0).ToString("dd-MMM-yyyy HH:mm:ss.fff"));
                            lastUpdate.Add(reader.GetDecimal(1).ToString());
                            break;
                        case "TIMESTAMP_NOMINAL":
                            
                            lastUpdate.Add(reader.GetDateTime(0).ToString("dd-MMM-yyyy HH:mm:ss.fff"));
                            lastUpdate.Add(reader.GetDecimal(1).ToString());
                            break;

                        case "ID_NOMINAL":
                            lastUpdate.Add(reader.GetDecimal(0).ToString());
                            lastUpdate.Add(reader.GetDecimal(1).ToString());
                            break;
                    }
                   
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
                _logger.Error(errorMessages.ToString());
                return null;
            }

        }
    }
}
