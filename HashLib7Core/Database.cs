using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HashLib7
{
    internal class Database
    {
        private string _connectionString;

        internal Database(string connString)
        {
            _connectionString = connString;
        }

        public int NumFilesInLibrary()
        {
            return ExecuteScalar("SELECT COUNT(*) FROM [dbo].[FileDetail]");
        }

        private int ExecuteNonQuery(string sql)
        {
            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new(sql, conn);
            conn.Open();
            int res = cmd.ExecuteNonQuery();
            return res;
        }

        private int ExecuteScalar(string sql)
        {
            using SqlConnection conn = new(_connectionString);
            using SqlCommand cmd = new(sql, conn);
            conn.Open();
            int res = (int)cmd.ExecuteScalar();
            return res;
        }

        internal FileHash ReadHash(string filename)
        {
            PathFormatted f = new(filename);
            string sql = string.Format("SELECT Hash FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{0}'", f.path, f.name);
            FileHash res = null;
            using (SqlConnection conn = new(_connectionString))
            {
                SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                    res = new(reader[0] as string);
            }
            return res;
        }

        internal SortedList<string, short> GetFilesByPathBrief(string path)
        {
            string sql = GetFilesByPathSql(path);
            SortedList<string, short> res = [];
            using (SqlConnection conn = new(_connectionString))
            {
                SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    res.Add((reader[0] as string).ToUpper(), 0);
                }
            }
            return res;
        }

        internal List<char> GetDrives()
        {
            string sql = "SELECT DISTINCT LEFT(Path, 1) FROM [dbo].[FileDetail] ORDER BY LEFT(Path, 1)";
            List<char> res = [];
            using (SqlConnection conn = new(_connectionString))
            {
                SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    res.Add((reader[0] as string)[0]);
                }
            }
            return res;
        }

        private static string GetFilesByPathSql(string path)
        {
            PathFormatted p = new(path);
            string sql = "SELECT Name, Hash, Size FROM [dbo].[FileDetail]";
            sql = string.Format("{0} WHERE (Path = '{1}%')", sql, p.path);
            sql += " ORDER BY Name";
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
                    FileInfo fi = new()
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
            bool pass = false;
            short numAttempts = 0;
            const short maxAttempts = 5;
            const short timeout = 500;
            PathFormatted f = new(filename);
            //Won't transact because the worst that can happen is that the previous record is lost. It was always incorrect so would require a re-run anyway.
            numAttempts = 0; pass = false;
            while (!pass && numAttempts < maxAttempts)
            {
                try {
                    DeleteFile(f);
                    pass = true;
                }
                catch {
                    if (++numAttempts < maxAttempts)
                        System.Threading.Thread.Sleep(Convert.ToInt32(System.Random.Shared.NextDouble() * timeout));
                }
            }
            numAttempts = 0; pass = false;
            string sqlInsert = String.Format("INSERT INTO [dbo].[FileDetail] (Path, Name, Hash, Size, Age, LastModified) VALUES ('{0}', '{1}', '{2}', {3}, {4}, '{5}')", f.path, f.name, hashfile, filesize, LastModified.Ticks, LastModified.ToString("yyyy-MM-dd hh:mm:ss"));
            while (!pass && numAttempts < maxAttempts)
            {
                try {
                    ExecuteNonQuery(sqlInsert);
                    pass = true;
                }
                catch {
                    if (++numAttempts < maxAttempts)
                        System.Threading.Thread.Sleep(Convert.ToInt32(System.Random.Shared.NextDouble() * timeout));
                }
            }
        }

        internal void WriteHash(FileHash hash)
        {
            WriteHash(hash.FilePath, hash.Hash, hash.Length, hash.LastModified);
        }

        //Used where the fingerprint no longer matches
        internal void DeleteFile(PathFormatted f)
        {
            string sqlDelete = String.Format("DELETE FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.path, f.name);
            ExecuteNonQuery(sqlDelete);
        }

        internal List<PathFormatted> GetFilesByHash(string hash)
        {
            string sql = String.Format("SELECT Path, Name FROM [FileDetail] WHERE Hash = '{0}' ORDER BY Path, Name", hash);
            List<PathFormatted> res = [];
            using (OdbcConnection conn = new(_connectionString))
            {
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    res.Add(new PathFormatted(reader[0] as string, reader[1] as string));
                reader.Close();
            }
            return res;
        }
    }
}
