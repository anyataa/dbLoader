using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
namespace WorkerService1
{
    class Parameter
    {
        private SqlConnection sqlReaderCon = new SqlConnection();
        private SqlConnection sqlInsertCon = new SqlConnection();
        private Database dataBase;
        private IConfiguration _configuration ;

        public Parameter(IConfiguration configuration) 
        {
            dataBase = new Database(configuration);
            _configuration = configuration;
        }

        public string returnParameterType(int parameterNumber)
        {
            return _configuration[$"DataConfig:Parameter{parameterNumber}"];
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
            WHERE BU = '{dataBase.returnBU(numberBU)}'";

            SqlCommand cmd = new SqlCommand(updateCmdSql, sqlInsertCon);
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
                //_logger.Warn(errorMessages.ToString());
            }
        }


        public List<string> getParam(int numberBU, string parameter)
        {
            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            string parameter_table = _configuration["DataConfig:ParamTableName"];
            StringBuilder errorMessages = new StringBuilder();
            //string lastUpdate = "";
            string setParameter = "";
            List<string> lastUpdate = new List<string>();

            switch (parameter)
            {
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
                               WHERE BU = '{dataBase.returnBU(numberBU)}'";

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
                //_logger.Error(errorMessages.ToString());
                return null;
            }

        }

    }
}
