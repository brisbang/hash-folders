using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib3
{
    internal class Database
    {
        private OleDbConnection _connection;
        private HashAlgorithm _hashAlgorithm;

        internal Database()
        {
            _hashAlgorithm = new HashAlgorithm();
        }

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

        private int ExecuteNonQuery(string sql)
        {
            if (_connection == null) throw new InvalidOperationException("Connection not set");
            OleDbCommand cmd = new OleDbCommand(sql, _connection);
            using (cmd)
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private int ExecuteScalar(string sql)
        {
            if (_connection == null) throw new InvalidOperationException("Connection not set");
            OleDbCommand cmd = new OleDbCommand(sql, _connection);
            using (cmd)
            {
                return (int)cmd.ExecuteScalar();
            }
        }

        private OleDbDataReader OpenReader(string sql)
        {
            if (_connection == null) throw new InvalidOperationException("Connection not set");
            OleDbCommand cmd = new OleDbCommand(sql, _connection);
            return cmd.ExecuteReader();
        }

        internal FileHash ReadHash(string filename)
        {
            string f1, f2;
            DeconstructFilename(filename, out f1, out f2);
            string sql = String.Format("SELECT FileName1 + FileName2, HashFile, FileSize, Ticks, LastModified FROM [File] WHERE FileName1 = '{0}' AND FileName2 = '{1}'", f1, f2);
            OleDbDataReader reader = OpenReader(sql);
            using (reader)
            {
                if (reader.Read())
                    return new FileHash(reader);
                else
                    return null;
            }
        }

        internal SortedList<string, int> GetFilesByPath(string path)
        {
            string f1, f2;
            DeconstructFilename(path, out f1, out f2);
            string sql = "SELECT FileName1 + FileName2 FROM [File] ";
            if (f2.Length == 0)
                sql = String.Format("{0} WHERE (FileName1 LIKE '{1}%')", sql, f1);
            else
                sql = String.Format("{0} WHERE (FileName1 = '{1}' AND FileName2 LIKE '{2}%')", sql, f1, f2);
            SortedList<string, int> res = new SortedList<string, int>();
            OleDbDataReader reader = OpenReader(sql);
            using (reader)
            {
                while (reader.Read())
                    res.Add((reader[0] as string).ToUpper(), 0);
            }
            return res;
        }

        private void WriteHash(string filename, string hashfile, long filesize, DateTime LastModified)
        {
            //Won't transact because the worst that can happen is that the previous record is lost. It was always incorrect so would require a re-run anyway.
            DeleteHash(filename);
            string f1, f2;
            DeconstructFilename(filename, out f1, out f2);
            string sqlInsert = String.Format("INSERT INTO [File] (FileName1, FileName2, HashFile, FileSize, Ticks, LastModified) VALUES ('{0}', '{1}', '{2}', {3}, {4}, '{5}')", f1, f2, hashfile, filesize, LastModified.Ticks, LastModified.ToString("yyyy-MM-dd hh:mm:ss"));
            ExecuteNonQuery(sqlInsert);
        }

        private static void DeconstructFilename(string filename, out string f1, out string f2)
        {
            if (filename.Length > 510)
                throw new Exception(String.Format("File path is too long: {0}", filename));
            if (filename.Length > 255)
            {
                f1 = filename.Substring(0, 255);
                f2 = filename.Substring(255, filename.Length - 255);
                f2 = f2.Replace("'", "''");
            }
            else
            {
                f1 = filename;
                f2 = "";
            }
            f1 = f1.Replace("'", "''");
        }

        internal void WriteHash(FileHash hash)
        {
            WriteHash(hash.FilePath, hash.Hash, hash.Length, hash.LastModified);
        }

        //Used where the fingerprint no longer matches
        internal void DeleteHash(string filename)
        {
            string f1, f2;
            DeconstructFilename(filename, out f1, out f2);
            string sqlDelete = String.Format("DELETE FROM [File] WHERE FileName1 = '{0}' AND FileName2 = '{1}'", f1, f2);
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


        //Compression
        //  Scan all files to see if the fingerprint still matches
        //  Delete if it doesn't
    }
}
