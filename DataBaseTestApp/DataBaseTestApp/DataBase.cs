using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;
using static AlgoTestClass.Algo;

namespace DataBaseTestApp
{
    public class eventData
    {
        public int Id { get; set; }
        public string ModuleID { get; set; }
        public string Tid { get; set; }
        public string Event { get; set; }
        public int Criticality { get; set; }
    }

    public class DataBase
    {
        private readonly string _dbPath;

        public DataBase(AlgoTestClass.Algo algo, string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));

            CreateTable();

            // Subscribe to Algo's event
            algo.OnEventDetected += HandleEventFromAlgo;
        }

        private string GetConnectionString()
        {
            Console.WriteLine("[DEBUG] Using DB Path: " + _dbPath);
            return $"Data Source={_dbPath};Version=3;";
        }

        private void CreateTable()
        {
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS EventTable (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ModuleID TEXT NOT NULL,
                    tid TEXT NOT NULL,
                    event TEXT NOT NULL,
                    criticality INTEGER CHECK(criticality BETWEEN 1 AND 10),
                    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                );";
            cmd.ExecuteNonQuery();
        }

        private void HandleEventFromAlgo(object sender, EventArgsWithData e)
        {
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand(con);

            // Insert event data
            cmd.CommandText = "INSERT INTO EventTable(ModuleID, tid, event, criticality) VALUES(@m, @t, @e, @c)";
            cmd.Parameters.AddWithValue("@m", e.ModuleID);
            cmd.Parameters.AddWithValue("@t", e.Tid);
            cmd.Parameters.AddWithValue("@e", e.Event);
            cmd.Parameters.AddWithValue("@c", e.Criticality);

            try
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine($"Inserted: {e.Event} from {e.ModuleID}");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"DB Insert failed: {ex.Message}");
            }

            // Optional: show all events in console
            cmd.CommandText = "SELECT Id, ModuleID, tid, event, criticality, timestamp FROM EventTable";
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            Console.WriteLine("Events in DB:");
            while (rdr.Read())
            {
                Console.WriteLine($"{rdr["Id"]}: {rdr["event"]} (Module: {rdr["ModuleID"]}, TID: {rdr["tid"]}, Criticality: {rdr["criticality"]})");
            }
        }

        public List<eventData> GetAllEvents()
        {
            var events = new List<eventData>();
            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand("SELECT Id, ModuleID, tid, event, criticality FROM EventTable", con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                events.Add(new eventData
                {
                    Id = Convert.ToInt32(rdr["Id"]),
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
