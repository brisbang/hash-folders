using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HashLib7
{
    internal class Database
    {
//        private OdbcConnection _connection;
        private string _connectionString;

        internal Database(string database)
        {
            _connectionString = String.Format("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={0}", database);
        }

/*
   public OdbcConnection GetConnection()
        {
            return new OdbcConnection(_connectionString);
        }
*/
/*        public void Open()
        {
            if (_connection != null) Close();
            OdbcConnection conn = new()
            {
                ConnectionString = _connectionString
            };
            conn.Open();
            _connection = conn;
        }
*/

        public int NumFilesInLibrary()
        {
            return ExecuteScalar("SELECT COUNT(*) FROM [File]");
        }

        private int ExecuteNonQuery(string sql)
        {
            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            {
                //            if (_connection == null) throw new InvalidOperationException("Connection not set");
                OdbcCommand cmd = new OdbcCommand(sql, conn);
                conn.Open();
                int res = cmd.ExecuteNonQuery();
                conn.Close();
                return res;
            }
        }

        private int ExecuteScalar(string sql)
        {
            //            if (_connection == null) throw new InvalidOperationException("Connection not set");
            using (OdbcConnection conn = new OdbcConnection(_connectionString))
            {
                OdbcCommand cmd = new OdbcCommand(sql, conn);
                conn.Open();
                int res = (int) cmd.ExecuteScalar();
                conn.Close();
                return res;
            }
        }

        internal FileHash ReadHash(string filename)
        {
            PathFormatted f = new(filename);
            string sql = String.Format("SELECT FileName1 + FileName2, HashFile, FileSize, Ticks, LastModified FROM [File] WHERE FileName1 = '{0}' AND FileName2 = '{1}'", f.part1, f.part2);
            FileHash res = null;
            using (OdbcConnection conn = new(_connectionString))
            {
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                    res = new FileHash(reader);
                reader.Close();
            }
            return res;
        }

        internal SortedList<string, short> GetFilesByPathBrief(string path)
        {
            string sql = GetFilesByPathSql(path);
            SortedList<string, short> res = new();
            using (OdbcConnection conn = new(_connectionString))
            {
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    res.Add((reader[0] as string).ToUpper(), 0);
                }
                reader.Close();
            }
            return res;
        }

        internal List<char> GetDrives()
        {
            string sql = "SELECT DISTINCT LEFT(FileName1, 1) FROM [File] ORDER BY LEFT(FileName1, 1)";
            List<char> res = new();
            using (OdbcConnection conn = new(_connectionString))
            {
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    res.Add((reader[0] as string)[0]);
                }
            }
            return res;
        }

        private static string GetFilesByPathSql(string path)
        {
            PathFormatted p = new PathFormatted(path);
            string sql = "SELECT FileName1 + FileName2, HashFile, FileSize FROM [File] ";
            if (p.part2.Length == 0)
                sql = String.Format("{0} WHERE (FileName1 LIKE '{1}%')", sql, p.part1);
            else
                sql = String.Format("{0} WHERE (FileName1 = '{1}' AND FileName2 LIKE '{2}%')", sql, p.part1, p.part2);
            sql += " ORDER BY FileName1, FileName2";
            return sql;
        }

        internal Queue<FileInfo> GetFilesByPath(string path)
        {
            string sql = GetFilesByPathSql(path);
            Queue<FileInfo> res = new();
            using (OdbcConnection conn = new(_connectionString))
            {
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    FileInfo fi = new FileInfo
                    {
                        filename = reader[0] as string,
                        hash = reader[1] as string,
                        size = long.Parse(reader[2] as string)
                    };
                    res.Enqueue(fi);
                }
                reader.Close();
            }
            return res;
        }

        private void WriteHash(string filename, string hashfile, long filesize, DateTime LastModified)
        {
            PathFormatted f = new(filename);
            //Won't transact because the worst that can happen is that the previous record is lost. It was always incorrect so would require a re-run anyway.
            DeleteFile(f);
            string sqlInsert = String.Format("INSERT INTO [File] (FileName1, FileName2, HashFile, FileSize, Ticks, LastModified) VALUES ('{0}', '{1}', '{2}', {3}, {4}, '{5}')", f.part1, f.part2, hashfile, filesize, LastModified.Ticks, LastModified.ToString("yyyy-MM-dd hh:mm:ss"));
            ExecuteNonQuery(sqlInsert);
        }

        internal void WriteHash(FileHash hash)
        {
            WriteHash(hash.FilePath, hash.Hash, hash.Length, hash.LastModified);
        }

        //Used where the fingerprint no longer matches
        internal void DeleteFile(PathFormatted f)
        {
            string sqlDelete = String.Format("DELETE FROM [File] WHERE FileName1 = '{0}' AND FileName2 = '{1}'", f.part1, f.part2);
            ExecuteNonQuery(sqlDelete);
        }

        internal List<string> GetFilesByHash(string hash)
        {
            string sql = String.Format("SELECT FileName1 + FileName2 FROM [File] WHERE HashFile = '{0}' ORDER BY FileName1, FileName2", hash);
            List<string> res = new();
            using (OdbcConnection conn = new(_connectionString))
            {
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    res.Add(reader[0] as string);
                reader.Close();
            }
            return res;
        }

/*        public void Close()
        {
            try
            {
                _connection.Close();
            }
            catch { }
            _connection = null;
        }
*/

        //Compression
        //  Scan all files to see if the fingerprint still matches
        //  Delete if it doesn't

    }
}
