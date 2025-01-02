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

        internal OleDbDataReader OpenReader(string sql)
        {
            if (_connection == null) throw new InvalidOperationException("Connection not set");
            OleDbCommand cmd = new OleDbCommand(sql, _connection);
            return cmd.ExecuteReader();
        }

        internal OleDbDataReader GetLibrary()
        {
            const string sql = "SELECT FileName, HashFile, FileSize, Ticks, LastModified FROM [File]";
            return OpenReader(sql);
        }

        internal void WriteHash(string filename, int hashFileName, string hashfile, long filesize, DateTime LastModified)
        {
            filename = filename.Replace("'", "''");
            //Won't transact because the worst that can happen is that the previous record is lost. It was always incorrect so would require a re-run anyway.
            string sqlDelete = String.Format("DELETE FROM [File] WHERE hashFileName = {0} AND FileName = '{1}'", hashFileName, filename);
            string sqlInsert = String.Format("INSERT INTO [File] (FileName, HashFile, FileSize, Ticks, LastModified) VALUES ('{0}', {1}, {2}, {3}, '{4}')", filename, hashFileName, filesize, LastModified.Ticks, LastModified.ToString("yyyy-MM-dd hh:mm:ss"));
            ExecuteNonQuery(sqlDelete);
            ExecuteNonQuery(sqlInsert);
        }

        //Used where the fingerprint no longer matches
        internal void DeleteHash(string filename, int hashFileName)
        {
            filename = filename.Replace("'", "''");
            string sqlDelete = String.Format("DELETE FROM [File] WHERE hashFileName = {0} AND FileName = '{1}'", hashFileName, filename);
            ExecuteNonQuery(sqlDelete);
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

        //Write out the FilenameHash to all records
        //Search on FilenameHash for updates
        //Testing of indexing 
        //  New files
        //  Updated fingerprint
        //Compression
        //  Scan all files to see if the fingerprint still matches
        //  Delete if it doesn't
        //Match folders
        //  Validate all fingerprints in both folders
        //  Compare the hashes
    }
}
