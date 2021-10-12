using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using log4net;
using System.Reflection;
using log4net.Config;
using System.IO;

namespace WorkerService1
{
    class Parameter
    {
        private SqlConnection sqlReaderCon = new SqlConnection();
        private SqlConnection sqlInsertCon = new SqlConnection();
        private Database dataBase;
        private IConfiguration _configuration ;
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void log4NetConfiguration()
        {

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        }

        public Parameter(IConfiguration configuration) 
        {
            log4NetConfiguration();
            dataBase = new Database(configuration);
            _configuration = configuration;
        }

        public string returnParameterType(string BU)
        {

            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            string getParameterSP = $"exec sp_GetParamType {BU}";
            SqlCommand cmd = new SqlCommand(getParameterSP, sqlReaderCon);
            StringBuilder errorMessages = new StringBuilder();
            try 
            {
                sqlReaderCon.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                string parameterType = "";
                while (reader.Read()) {
                    parameterType = reader.GetString(0);
                }
                sqlReaderCon.Close();
                return parameterType;
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
                sqlReaderCon.Close();
                return "Error";
            }
        }

        public List<string> getParameterColumnName(string parameter, string BU) 
        {
            List<string> columnNameList = new List<string>();
            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            string getParameterSP = $"";
            if (parameter == "ID_ONLY" || parameter == "TIMESTAMP_ONLY")
            {
                getParameterSP = $"exec sp_GetColumnOneParam '{BU}'";
            }
            else 
            {
                getParameterSP = $"exec sp_GetColumnTwoParam '{BU}'";
            }
            
            SqlCommand cmd = new SqlCommand(getParameterSP, sqlReaderCon);
            StringBuilder errorMessages = new StringBuilder();
            try
            {
                sqlReaderCon.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                string parameterColumn1 = "";
                string parameterColumn2 = "";
                while (reader.Read())
                {
                    if (parameter == "ID_ONLY" || parameter == "TIMESTAMP_ONLY")
                    {
                        parameterColumn1 = reader.GetString(0);
                        columnNameList.Add(parameterColumn1);
                    }
                    else
                    {
                        parameterColumn1 = reader.GetString(0);
                        parameterColumn2 = reader.GetString(1);

                        columnNameList.Add(parameterColumn1);
                        columnNameList.Add(parameterColumn2);
                        
                    }
                   
                }
                sqlReaderCon.Close();
                return columnNameList;
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
                sqlReaderCon.Close();
                return null;
            }

        }

        public void updateParam(string paramValue, string paramValueOptional, int numberBU, string parameter)
        {
            sqlInsertCon.ConnectionString = dataBase.returnSqlDB();
            string parameter_table = _configuration["DataConfig:ParamTableName"];

            string setParameter = "";

            switch (parameter)
            {
                case "TIMESTAMP_ONLY":
                    setParameter = $"SET VAL_PARAM01 = CAST('{paramValue}' AS Datetime2)";
                    //setParameter = $"sp_UpdateParamTimestamp '{paramValue}', '{dataBase.returnBU(numberBU)}'";
                    break;

                case "ID_ONLY":
                    setParameter = $"SET VAL_PARAM02 = {paramValue})";
                    //setParameter = $"sp_UpdateParamTimestamp {paramValue}";
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
             {setParameter} WHERE BU = '{dataBase.returnBU(numberBU)}'";

            SqlCommand cmd = new SqlCommand(updateCmdSql, sqlInsertCon);
            StringBuilder errorMessages = new StringBuilder();
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


        public List<string> getParam(int numberBU, string parameter)
        {
            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            //string parameter_table = _configuration["DataConfig:ParamTableName"];
            StringBuilder errorMessages = new StringBuilder();
            //string setParameter = "";
            string getCmdSql = "";
            List<string> lastUpdate = new List<string>();

            switch (parameter)
            {
                case "TIMESTAMP_ONLY":
                    getCmdSql = $"sp_GetParamTimestampOnly  '{dataBase.returnBU(numberBU)}'";
                    break;

                case "ID_ONLY":
                    getCmdSql = $"sp_GetParamIdOnly  '{dataBase.returnBU(numberBU)} ";
                    break;

                case "TIMESTAMP_ID":
                    getCmdSql = $"sp_GetParamTimestampId  '{dataBase.returnBU(numberBU)}'";
                    break;
                case "TIMESTAMP_NOMINAL":
                    getCmdSql = $"sp_GetParamTimestampNominal  '{dataBase.returnBU(numberBU)}'";
                    break;

                case "ID_NOMINAL":
                    getCmdSql = $"sp_GetParamIdNominal  '{dataBase.returnBU(numberBU)}'";
                    break;
            }

           

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
