using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;
using VitaWave.Data;
using VitaWave.Common;

namespace VitaWave.DataBase
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

        public EventController()
        {
            // Start in the user's vitawave folder
            var dir = new DirectoryInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "vitawave")
            );

            // Climb upward until we find the folder named "VitaWave.DataBase"
            while (dir != null && dir.Name != "VitaWave.DataBase")
            {
                dir = dir.Parent;
            }

            if (dir == null)
                throw new DirectoryNotFoundException("Could not locate 'VitaWave.DataBase' folder.");

            var projectRoot = dir.FullName;

            // Construct paths
            _dbPath = Path.Combine(projectRoot, "Database", "EventTable.db");
            _exportPath = Path.Combine(projectRoot, "Exports");

            // Create export folder if missing
            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);

            Console.WriteLine($"Database Path: {_dbPath}");
            Console.WriteLine($"Export Path: {_exportPath}");
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

        }

        public void HandleEventFromAlgo(object? s, ResultEvent e)
        {
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand(con);

            cmd.CommandText = "INSERT INTO EventTable(ModuleID, tid, event, criticality) VALUES(@m, @t, @e, @c)";
            cmd.Parameters.AddWithValue("@m", e.ModuleID);
            cmd.Parameters.AddWithValue("@t", e.TID);
            cmd.Parameters.AddWithValue("@e", e.ResultId);
            cmd.Parameters.AddWithValue("@c", 1);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
            }

            // Optional: print all stored events
            cmd.CommandText = "SELECT ModuleID, tid, event, criticality, timestamp FROM EventTable";
            using SQLiteDataReader rdr = cmd.ExecuteReader();
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
