using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
                string sql = SafeSql("SELECT LastModified, Size, Hash FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.Path, f.Name);
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

        internal List<FileInfo> GetFilesByPath(string path)
        {
            try
            {
                string sql = GetFilesByPathSql(path);
                List<FileInfo> res = [];
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmd = new(sql, conn);
                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    FileInfo fi = new(reader[0] as string, reader[1] as string)
                    {
                        hash = reader[2] as string,
                        size = (long) reader[3]
                    };
                    res.Add(fi);
                }
                return res;
            }
            catch (Exception ex)
            {
                Config.WriteException("", ex);
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
                Config.LogDebugging("Creating record for " + filename);
                string sqlInsert = SafeSql("INSERT INTO [dbo].[FileDetail] (Path, Name, Hash, Size, Age, LastModified, FileScanned) VALUES ('{0}', '{1}', '{2}', {3}, {4}, '{5}', getdate())", f.Path, f.Name, hashfile, filesize.ToString(), LastModified.Ticks.ToString(), LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                ExecuteNonQuery(sqlInsert);
            }
            else
            {
                Config.LogDebugging("Updating record for " + filename);
                string sqlUpdate = SafeSql("UPDATE [dbo].[FileDetail] SET Hash = '{2}', Size = '{3}', Age = '{4}', LastModified = '{5}', FileScanned = getdate(), Fire = NULL, Corruption = NULL, DiskFailure = NULL, Theft = NULL WHERE Path = '{0}' AND Name = '{1}'", f.Path, f.Name, hashfile, filesize.ToString(), LastModified.Ticks.ToString(), LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
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
            string sqlDelete = SafeSql("DELETE FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.Path, f.Name);
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

        internal FileInfoDetailed GetFileInfoDetailed(PathFormatted f, bool populateBackupLocations)
        {
            try
            {
                string sqlHeader = SafeSql("SELECT Path, Name, Size, Hash, LastModified FROM [dbo].[FileDetail] WHERE Path = '{0}' AND Name = '{1}'", f.Path, f.Name);
                using SqlConnection conn = new(_connectionString);
                using SqlCommand cmdHeader = new(sqlHeader, conn);
                conn.Open();
                using SqlDataReader readerHeader = cmdHeader.ExecuteReader();
                if (!readerHeader.Read())
                    return null;
                FileInfoDetailed info = new(readerHeader[0] as string, readerHeader[1] as string)
                {
                    size = (long)readerHeader[2],
                    hash = readerHeader[3] as string,
                    lastModified = (DateTime)readerHeader[4],
                    backupLocations = []
                };
                readerHeader.Close();
                if (populateBackupLocations && info.size > 0)
                {
                    string sqlBackups = SafeSql("SELECT Path, Name FROM [dbo].[FileDetail] WHERE Hash = '{0}' AND NOT (Path = '{1}' AND Name = '{2}') ORDER BY Path, Name", info.hash, f.Path, f.Name);
                    using SqlCommand cmdBackups = new(sqlBackups, conn);
                    using SqlDataReader readerBackups = cmdBackups.ExecuteReader();
                    while (readerBackups.Read())
                        info.backupLocations.Add(new PathFormatted(readerBackups[0] as string, readerBackups[1] as string));
                }
                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        internal void SaveRiskAssessment(RiskAssessment res)
        {
            try
            {
                string sqlUpdate = SafeSql("UPDATE FileDetail SET Fire = {0}, Theft = {1}, Corruption = {2}, DiskFailure = {3}, RiskAssessmentUpdate = getdate() WHERE Path = '{4}' AND Name = '{5}'",
                    res.Fire ? "1" : "0",
                    res.Theft ? "1" : "0",
                    res.Corruption ? "1" : "0",
                    res.DiskFailure ? "1" : "0",
                    res.FileInfoDetailed.Path,
                    res.FileInfoDetailed.Name);
                ExecuteNonQuery(sqlUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}
