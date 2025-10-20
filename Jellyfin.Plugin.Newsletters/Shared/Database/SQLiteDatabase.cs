#pragma warning disable 1591, CA1304
using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.Newsletters.Configuration;
using Jellyfin.Plugin.Newsletters.LOGGER;
using Jellyfin.Plugin.Newsletters.Scripts.ENTITIES;
using MediaBrowser.Common.Configuration;
using SQLitePCL;
using SQLitePCL.pretty;

namespace Jellyfin.Plugin.Newsletters.Shared.DATA;

public class SQLiteDatabase
    {
        private readonly PluginConfiguration config;
        private string dbFilePath;
        private string dbLockPath;
        private Logger logger;
        private SQLiteDatabaseConnection? _db;
        // private bool writeLock;

        public SQLiteDatabase()
        {
            logger = new Logger();
            config = Plugin.Instance!.Configuration;
            SQLite3.EnableSharedCache = false;

            _ = raw.sqlite3_config(raw.SQLITE_CONFIG_MEMSTATUS, 0);

            _ = raw.sqlite3_config(raw.SQLITE_CONFIG_MULTITHREAD, 1);

            _ = raw.sqlite3_enable_shared_cache(1);

            ThreadSafeMode = raw.sqlite3_threadsafe();
            dbFilePath = config.DataPath + "/newsletters.db"; // get directory from config
            dbLockPath = dbFilePath + ".lock";
        }

        internal static int ThreadSafeMode { get; set; }

        public void CreateConnection()
        {
            if (!File.Exists(dbLockPath)) // Database is not locked
            {
                logger.Debug("Opening Database: " + dbFilePath);
                _db = SQLite3.Open(dbFilePath);
                File.WriteAllText(dbLockPath, string.Empty);
                InitDatabaase();
                // writeLock = true;
            }
            else
            {
                logger.Debug("Database lock file shows database is in use: " + dbLockPath);
            }

            // Example of looping through query, creating
            // foreach (var row in Query("SELECT * FROM CurrRunData"))
            // {
            //     if (row is not null)
            //     {
            //         JsonFileObj helper = new JsonFileObj();
            //         JsonFileObj newObj = helper.ConvertToObj(row);
            //         logger.Debug("NewObj: " + newObj.Filename);
            //         logger.Debug("Title: " + newObj.Title);
            //         // logger.Debug(row[0]);
            //     }
            // }
        }

        private void InitDatabaase()
        {
            // Filename = string.Empty;
            // Title = string.Empty;
            // Season = 0;
            // Episode = 0;
            // SeriesOverview = string.Empty;
            // ImageURL = string.Empty;
            // ItemID = string.Empty;
            // PosterPath = string.Empty;

            logger.Debug("Creating Tables...");
            string[] tableNames = { "CurrRunData", "CurrNewsletterData", "ArchiveData" };
            CreateTables(tableNames);
            logger.Debug("Done Init of tables");
        }

        private void CreateTables(string[] tables)
        {
            foreach (string table in tables)
            {
                ExecuteSQL("create table if not exists " + table + " (" +
                                "Filename TEXT NOT NULL," +
                                "Title TEXT," +
                                "Season INT," +
                                "Episode INT," +
                                "SeriesOverview TEXT," +
                                "ImageURL TEXT," +
                                "ItemID TEXT," +
                                "PosterPath TEXT," +
                                "Type TEXT," +
                                // "PremiereYear TEXT" +
                                // "RunTime INT" +
                                // "OfficialRating TEXT" +
                                // "CommunityRating REAL" +
                                "PRIMARY KEY (Filename)" +
                            ");");

                // ExecuteSQL("ALTER TABLE " + table + " ADD COLUMN Type TEXT;");
                // logger.Debug("Altering Table not needed since V0.6.2.0");
                // continue;
                logger.Info($"Altering DB table: {table}");
                // <TABLE_NAME, DATA_TYPE>
                Dictionary<string, string> new_cols = new Dictionary<string, string>();
                new_cols.Add("PremiereYear", "TEXT");
                new_cols.Add("RunTime", "INT");
                new_cols.Add("OfficialRating", "TEXT");
                new_cols.Add("CommunityRating", "REAL");

                foreach (KeyValuePair<string, string> col in new_cols)
                {
                    try
                    {
                        logger.Debug($"Adding Table Columns for DB updates...");
                        ExecuteSQL($"ALTER TABLE {table} ADD COLUMN {col.Key} {col.Value};");
                    }
                    catch (SQLiteException sle)
                    {
                        // logger.Warn(sle);
                        logger.Debug(sle);
                    }
                }
            }
        }

        public IEnumerable<IReadOnlyList<ResultSetValue>> Query(string query)
        {
            logger.Debug("Running Query: " + query);
            return _db.Query(query);
        }

        // private IStatement PrepareStatement(string query)
        // {
        //     return _db.PrepareStatement(query);
        // }

        public void ExecuteSQL(string query)
        {
            logger.Debug("Executing SQL Statement: " + query);
            _db.Execute(query);
        }

        public void CloseConnection()
        {
            if (File.Exists(dbLockPath)) // Database is locked
            {
                logger.Debug("Closing Database: " + dbFilePath);
                // _db.Close();
                File.Delete(dbLockPath);
                // logger.Debug("TYPE: " + conn.GetType());
                // writeLock = true;
            }
            else
            {
                logger.Debug("Database lock file does not exist. Database is not use: " + dbLockPath);
            }
        }
    }