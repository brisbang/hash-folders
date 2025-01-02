using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    internal class Database
    {
        private OleDbConnection _connection;

        public void Open(string database)
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = String.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Persist Security Info=True", database);
            conn.Open();
            Close();
            _connection = conn;
        }

        public int NumFilesInLibrary()
        {
            return ExecuteScalar("SELECT COUNT(*) FROM [File]");
        }

        internal int ExecuteNonQuery(string sql)
        {
            if (_connection == null) throw new InvalidOperationException("Connection not set");
            OleDbCommand cmd = new OleDbCommand(sql, _connection);
            return cmd.ExecuteNonQuery();
        }

        internal int ExecuteScalar(string sql)
        {
            if (_connection == null) throw new InvalidOperationException("Connection not set");
            OleDbCommand cmd = new OleDbCommand(sql, _connection);
            return (int)cmd.ExecuteScalar();
        }

        public void Close()
        {
            try
            {
                _connection.Close();
            }
            catch { }
            _connection = null;
        }

        //Remove Destination and write to the database
        //Write out the FilenameHash
        //Search on FilenameHash
    }
}
