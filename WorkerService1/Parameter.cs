using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace WorkerService1
{
    class Parameter
    {
        private SqlConnection sqlReaderCon = new SqlConnection();

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
                FROM returnConfigTableName() WHERE BU = 'returnBU(numberBU)'";

            SqlDataReader configReader = configCommand.ExecuteReader();

            while (configReader.Read())
            {
                sources.Add(configReader.GetString(1));
            }
            sqlReaderCon.Close();
            return sources;
        }
    }
}
