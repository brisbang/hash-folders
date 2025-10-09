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
using Microsoft.Extensions.Logging;

namespace HashLib7
{
    internal class Database
    {
        private string _connectionString;
        private readonly ILogger<Database> _logger;

        internal Database(ILogger<Database> logger, string connString)
        {
            _logger = logger;
            _connectionString = connString;
        }

        public int NumFilesInLibrary()
        {
            return ExecuteScalar("SELECT COUNT(*) FROM [dbo].[FileDetail]");
        }

        private int ExecuteNonQuery(string sql)
        {
            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                int res = cmd.ExecuteNonQuery();
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        private int ExecuteScalar(string sql)
        {
            try
            {
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        private static string SafeSql(string formatString, params string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                args[i] = args[i].Replace("'", "''");
            return String.Format(formatString, args);
        }

        internal FileHash ReadHash(string filename)
        {
            try
            {
                PathFormatted f = new(filename);
                string sql = SafeSql("SELECT LastModified, Size, Hash FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.path, f.name);
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                    return new(filename, DateTime.Parse(reader[0].ToString()), (long)reader[1], (string)reader[2]);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        internal SortedList<string, string> GetFilesByPathBrief(string path)
        {
            try
            {
                string sql = GetFilesByPathSql(path);
                SortedList<string, string> res = [];
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string value = String.Format("{0}\\{1}", reader[0] as string, reader[1] as string);
                    res.Add(value.ToUpper(), value);
                }
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }
/*
        internal List<char> GetDrives()
        {
            try
            {
                string sql = "SELECT DISTINCT LEFT(Path, 1) FROM [dbo].[FileDetail] ORDER BY LEFT(Path, 1)";
                List<char> res = [];
                using SqlConnection conn = new(_connectionString);
                using (SqlCommand cmd = new(SafeSql(sql), conn))
                {
                    conn.Open();
                    using SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add((reader[0] as string)[0]);
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }

        }
*/
        private static string GetFilesByPathSql(string path)
        {
            return SafeSql("SELECT Path, Name, Hash, Size FROM [dbo].[FileDetail] WHERE (Path LIKE '{0}%') ORDER BY Name", path);
        }

        internal Queue<FileInfo> GetFilesByPath(string path)
        {
            try
            {
                string sql = GetFilesByPathSql(path);
                Queue<FileInfo> res = new();
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    FileInfo fi = new()
                    {
                        filePath = String.Format("{0}\\{1}", reader[0] as string, reader[1] as string),
                        hash = reader[2] as string,
                        size = (long) reader[3]
                    };
                    res.Enqueue(fi);
                }
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }



        private void WriteHash(string filename, string hashfile, long filesize, DateTime LastModified, bool newRecord)
        {
            PathFormatted f = new(filename);
            //Won't transact because the worst that can happen is that the previous record is lost. It was always incorrect so would require a re-run anyway.
            if (newRecord)
            //Technically for multiuser then you would place this all into a transaction, including the check for existence.
            //However this path (having the caller advise) produces less data growth
            //if (FileExists(f))
            {
                Config.LogInfo("Creating record for " + filename);
                string sqlInsert = SafeSql("INSERT INTO [dbo].[FileDetail] (Path, Name, Hash, Size, Age, LastModified) VALUES ('{0}', '{1}', '{2}', {3}, {4}, '{5}')", f.path, f.name, hashfile, filesize.ToString(), LastModified.Ticks.ToString(), LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                ExecuteNonQuery(sqlInsert);
            }
            else
            {
                Config.LogInfo("Updating record for " + filename);
                string sqlUpdate = SafeSql("UPDATE [dbo].[FileDetail] SET Hash = '{2}', Size = '{3}', Age = '{4}', LastModified = '{5}' WHERE Path = '{0}' AND Name = '{1}'", f.path, f.name, hashfile, filesize.ToString(), LastModified.Ticks.ToString(), LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                ExecuteNonQuery(sqlUpdate);
            }
        }

        internal void WriteHash(FileHash hash, bool newRecord)
        {
            WriteHash(hash.FilePath, hash.Hash, hash.Length, hash.LastModified, newRecord);
        }
/*
        //Used where the fingerprint no longer matches
        internal bool FileExists(PathFormatted f)
        {
            string sqlDelete = SafeSql("SELECT COUNT(*) FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.path, f.name);
            return 1 == ExecuteScalar(sqlDelete);
        }
*/
        //Used where the fingerprint no longer matches
        internal void DeleteFile(PathFormatted f)
        {
            Config.LogInfo("Deleting record for " + f.fullName + " as it is no longer found");
            string sqlDelete = SafeSql("DELETE FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.path, f.name);
            ExecuteNonQuery(sqlDelete);
        }

        internal List<PathFormatted> GetFilesByHash(string hash)
        {
            try
            {
                string sql = SafeSql("SELECT Path, Name FROM [FileDetail] WHERE Hash = '{0}' ORDER BY Path, Name", hash);
                List<PathFormatted> res = [];
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    res.Add(new PathFormatted(reader[0] as string, reader[1] as string));
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}
