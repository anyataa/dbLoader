using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace WorkerService1
{
    class Configuration
    {
        private SqlConnection sqlReaderCon = new SqlConnection();
        private Database dataBase;

        public Configuration(IConfiguration configuration)
        {
            dataBase = new Database(configuration);
        }
        public List<string> getListSource(int numberBU)
        {

            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            List<string> sources = new List<string>();
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"sp_GetParamSource '{dataBase.returnBU(numberBU)}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                sources.Add(configReader.GetString(0));
            }
            sqlReaderCon.Close();
            return sources;
        }

        public List<string> getListDestination(int numberBU)
        {
            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            List<string> destination = new List<string>();
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"sp_GetParamDestination '{dataBase.returnBU(numberBU)}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                destination.Add(configReader.GetString(0));
            }
            sqlReaderCon.Close();
            return destination;
        }

        public List<string> getListDatatype(int numberBU)
        {
            sqlReaderCon.ConnectionString = dataBase.returnSqlDB();
            List<string> dataType = new List<string>();
            sqlReaderCon.Open();
            SqlCommand configCommand = sqlReaderCon.CreateCommand();
            configCommand.CommandText = $@"sp_GetParamDatatype '{dataBase.returnBU(numberBU)}'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                dataType.Add(configReader.GetString(0));
            }
            sqlReaderCon.Close();
            return dataType;
        }
    }
}
