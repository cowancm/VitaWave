using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;
using VitaWave.Data;
using VitaWave.Common;

namespace DataBaseTestApp
{
    public class eventData
    {
        public string ModuleID { get; set; }
        public string Tid { get; set; }
        public string Event { get; set; }
        public int Criticality { get; set; }
    }

    public class DataBase
    {
        private readonly string _dbPath;

        public DataBase()
        {
            // Build a path for the database inside the project’s Database folder
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // /Database/database/ folder
            string dbFolder = Path.Combine(baseDir, "Database", "database");
            Directory.CreateDirectory(dbFolder);

            _dbPath = Path.Combine(dbFolder, "EventTable.db");

            Console.WriteLine($"[INFO] Database path: {_dbPath}");

            CreateTable();
        }

        private string GetConnectionString()
        {
            return $"Data Source={_dbPath};Version=3;";
        }

        private void CreateTable()
        {
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS EventTable (
                    ModuleID TEXT NOT NULL,
                    tid TEXT NOT NULL,
                    event TEXT NOT NULL,
                    criticality INTEGER CHECK(criticality BETWEEN 1 AND 10),
                    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            cmd.ExecuteNonQuery();

            Console.WriteLine("[INFO] EventTable ready.");
        }

        // ✅ Public so DataFacilitator can subscribe
        public void HandleEventFromAlgo(object? s, ResultEvent e)
        {
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand(con);

            cmd.CommandText = "INSERT INTO EventTable(ModuleID, tid, event, criticality) VALUES(@m, @t, @e, @c)";
            cmd.Parameters.AddWithValue("@m", e.ModuleID);
            cmd.Parameters.AddWithValue("@t", e.TID);
            cmd.Parameters.AddWithValue("@e", e.GetTypeResult(e.ResultId));
            cmd.Parameters.AddWithValue("@c", 1);

            try
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine($"[INFO] Inserted: {e.GetTypeResult(e.ResultId)} from {e.ModuleID}");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"[ERROR] DB Insert failed: {ex.Message}");
            }

            // Optional: print all stored events
            cmd.CommandText = "SELECT ModuleID, tid, event, criticality, timestamp FROM EventTable";
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            Console.WriteLine("\n[INFO] Current Events in DB:");
            while (rdr.Read())
            {
                Console.WriteLine($"→ {rdr["event"]} (Module: {rdr["ModuleID"]}, TID: {rdr["tid"]}, Criticality: {rdr["criticality"]})");
            }
        }

        public List<eventData> GetAllEvents()
        {
            var events = new List<eventData>();
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand("SELECT ModuleID, tid, event, criticality FROM EventTable", con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                events.Add(new eventData
                {
                    ModuleID = rdr["ModuleID"].ToString(),
                    Tid = rdr["tid"].ToString(),
                    Event = rdr["event"].ToString(),
                    Criticality = Convert.ToInt32(rdr["criticality"])
                });
            }

            return events;
        }
    }
}
