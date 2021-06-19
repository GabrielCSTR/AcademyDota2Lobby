using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2LDatabase
{
    public class DBConnection
    {
        public DBConnection(String dateConnection)
        {
            this._connection = new SqlConnection();
            this._stringConnection = dateConnection;
            this._connection.ConnectionString = dateConnection;
        }

        private String _stringConnection;
        public String StringConnection
        {
            get { return this._stringConnection; }
            set { this._stringConnection = value; }
        }

        private SqlConnection _connection;
        public SqlConnection ObjectConnection
        {
            get { return this._connection; }
            set { this._connection = value; }
        }

        public void Connection()
        {
            this._connection.Open();
        }

        public void Disconnect()
        {
            this._connection.Close();
        }
    }
}
