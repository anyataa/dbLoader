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

namespace WorkerService1.DbConfig
{
    class DatabaseConnection
    {
        private readonly IConfiguration _configuration;
       
        public DatabaseConnection(IConfiguration configuration) {
            _configuration = configuration;
        }

       
    }
}
