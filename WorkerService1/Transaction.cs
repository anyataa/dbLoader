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
        private Database dataBase;
        private Configuration configurationTable;
        private Parameter parameterTable;
        public Transaction(ILogger<Worker> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            dataBase = new Database(configuration);
            configurationTable = new Configuration(configuration);
            parameterTable = new Parameter(configuration);
            //_logger = logger;

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

            //(parameterTable.getListSource(1)).ForEach(delegate (string item)
            //{
            //    _logger.Info(item);
            //});


            for (int i = 1; i <= dataBase.returnTotalDb(); i++)
            {
                _logger.Info($"___________  Start processing from BU : {dataBase.returnBU(i)} ________\n");

                mapOracleData(
                    _configuration[$"DataConfig:SourceDB{i}"],
                    _configuration[$"DataConfig:DestinationDB{i}"],
                    i,
                    parameterTable.returnParameterType(i),
                    returnParameterColumnName(i)[0],
                    returnParameterColumnName(i)[1]
                    );
            }



        }

        public void log4NetConfiguration() {
            
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            
        }

   

        public  void mapOracleData(string sourceTable, string destinationTable, int numberBU, string parameter, string parameterColumn1, string parameterColumn2) {

            OracleCommand cmd = con.CreateCommand();
            string limitData = dataBase.returnLimitData();
            string[] sourceArray = configurationTable.getListSource(numberBU).ToArray();
            string selectDataSource = "";

            //set insert based on configuration 
            string insertParam = "";
            string insertValue = "";
            string listData = "";
          


            configurationTable.getListDestination(numberBU).ForEach(delegate (string item)
                {
                    insertParam = insertParam + item + ",";
                    insertValue = insertValue + "@" + item + ",";
                });

            string[] destinationArray = configurationTable.getListDestination(numberBU).ToArray();
            insertParam = insertParam.Substring(0, insertParam.Length - 1);
            insertValue = insertValue.Substring(0, insertValue.Length - 1);

            configurationTable.getListSource(numberBU).ForEach(delegate (string item)
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
                    lastUpdate = parameterTable.getParam(numberBU, parameter)[0];
                    parameterCondition = $"{parameterColumn1} > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3')";
                    break;

                case "ID_ONLY":
                    lastUpdate = parameterTable.getParam(numberBU, parameter)[0];
                    parameterCondition = $"{parameterColumn1} > {lastUpdate}";
                    break;

                case "TIMESTAMP_ID":
                    lastUpdate = parameterTable.getParam(numberBU, parameter)[0];
                    lastUpdateOptional = parameterTable.getParam(numberBU, parameter)[1];
                    parameterCondition = $"{parameterColumn1} > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3') AND {parameterColumn2} != {lastUpdateOptional} ";
                    break;

                case "TIMESTAMP_NOMINAL":
                    lastUpdate = parameterTable.getParam(numberBU, parameter)[0];
                    lastUpdateOptional = parameterTable.getParam(numberBU, parameter)[1];

                    parameterCondition = $"{parameterColumn1} > TO_TIMESTAMP('{lastUpdate}', 'DD-Mon-RR HH24:MI:SS.FF3') AND {parameterColumn2} != {lastUpdateOptional} ";

                    break;

                case "ID_NOMINAL":
                    lastUpdate = parameterTable.getParam(numberBU, parameter)[0];
                    lastUpdateOptional = parameterTable.getParam(numberBU, parameter)[1];

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

                        switch (configurationTable.getListDatatype(numberBU)[i])
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
                            parameterTable.updateParam(lastUpdate,"", numberBU, parameter);
                            break;

                        case "ID_ONLY":
                            lastUpdate = reader.GetString(getIndexFor(numberBU, "ID"));
                            parameterTable.updateParam(lastUpdate, "", numberBU, parameter);
                            break;

                        case "TIMESTAMP_ID":
                            lastUpdate = reader.GetDateTime(getIndexFor(numberBU, "CREATED_TIME")).ToString("yyyy-MM-dd HH:mm:ss.fff");
                            lastUpdateOptional = reader.GetString(getIndexFor(numberBU, "ID"));
                            parameterTable.updateParam(lastUpdate, lastUpdateOptional, numberBU, parameter);
                            break;

                        case "TIMESTAMP_NOMINAL":
                            lastUpdate = reader.GetDateTime(getIndexFor(numberBU, "CREATED_TIME")).ToString("yyyy-MM-dd HH:mm:ss.fff");
                            lastUpdateOptional = reader.GetDecimal(getIndexFor(numberBU, "NOMINAL")).ToString();
                            parameterTable.updateParam(lastUpdate, lastUpdateOptional, numberBU, parameter);
                            break;

                        case "ID_NOMINAL":
                            lastUpdate = reader.GetString(getIndexFor(numberBU, "ID"));
                            lastUpdateOptional = reader.GetDecimal(getIndexFor(numberBU, "NOMINAL")).ToString();
                            parameterTable.updateParam(lastUpdate, lastUpdateOptional, numberBU, parameter);
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
            for (int i = 0; i < configurationTable.getListSource(numberBU).ToArray().Length; i++)
            {
                if (configurationTable.getListSource(numberBU)[i] == columnParam)
                {
                    index = i;
                }

            };
            return index;
        }

    }
}
