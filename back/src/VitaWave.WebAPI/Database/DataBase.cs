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
        public string ModuleID { get; set; } = "";
        public string Tid { get; set; } = "";
        public string Event { get; set; } = "";
        public int Criticality { get; set; }
    }

    public class DataBase
    {
        private readonly string _dbPath;

        public DataBase(DataFacilitator data)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var projectRoot = Path.Combine(userProfile, "VitaWave", "back", "src", "VitaWave.WebAPI");

            data.EventRaise += HandleEventFromAlgo;

            // Database folder
            string dbFolder = Path.Combine(projectRoot, "Database");
            Directory.CreateDirectory(dbFolder);

            // Same database file as the controller
            _dbPath = Path.Combine(dbFolder, "EventTable.db");

            Console.WriteLine($"[DataBase.cs] Database Path: {_dbPath}");
            Console.WriteLine($"[DataBase.cs] Database exists: {File.Exists(_dbPath)}");

            CreateTable();
        }

        private string GetConnectionString()
        {
            return $"Data Source={_dbPath};Version=3;";
        }

        private void CreateTable()
        {
            try
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

                Console.WriteLine("[DataBase.cs] EventTable created/verified successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataBase.cs] ERROR creating table: {ex.Message}");
                throw;
            }
        }

        public void HandleEventFromAlgo(object? s, ResultEvent e)
        {
            try
            {
                // Validate input
                if (e == null)
                {
                    Console.WriteLine("[DataBase.cs] WARNING: Received null ResultEvent");
                    return;
                }

                Console.WriteLine($"[DataBase.cs] Inserting event - ModuleID: {e.ModuleID}, TID: {e.TID}, ResultId: {e.ResultId}");

                using var con = new SQLiteConnection(GetConnectionString());
                con.Open();

                using var cmd = new SQLiteCommand(con);
                cmd.CommandText = "INSERT INTO EventTable(ModuleID, tid, event, criticality) VALUES(@m, @t, @e, @c)";

                // Add parameters with proper values
                cmd.Parameters.AddWithValue("@m", e.ModuleID ?? "Unknown");
                cmd.Parameters.AddWithValue("@t", e.TID ?? "Unknown");
                cmd.Parameters.AddWithValue("@e", e.ResultId ?? "Unknown");
                cmd.Parameters.AddWithValue("@c", 1);

                int rowsAffected = cmd.ExecuteNonQuery();
                Console.WriteLine($"[DataBase.cs] Event inserted successfully. Rows affected: {rowsAffected}");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine($"[DataBase.cs] SQLite ERROR inserting event: {ex.Message}");
                Console.WriteLine($"[DataBase.cs] SQLite Error Code: {ex.ErrorCode}");
                Console.WriteLine($"[DataBase.cs] Stack Trace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataBase.cs] GENERAL ERROR inserting event: {ex.Message}");
                Console.WriteLine($"[DataBase.cs] Stack Trace: {ex.StackTrace}");
            }
        }

        public List<eventData> GetAllEvents()
        {
            var events = new List<eventData>();

            try
            {
                using var con = new SQLiteConnection(GetConnectionString());
                con.Open();

                using var cmd = new SQLiteCommand("SELECT ModuleID, tid, event, criticality FROM EventTable", con);
                using SQLiteDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    events.Add(new eventData
                    {
                        ModuleID = rdr["ModuleID"].ToString() ?? "",
                        Tid = rdr["tid"].ToString() ?? "",
                        Event = rdr["event"].ToString() ?? "",
                        Criticality = Convert.ToInt32(rdr["criticality"])
                    });
                }

                Console.WriteLine($"[DataBase.cs] Retrieved {events.Count} events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataBase.cs] ERROR retrieving events: {ex.Message}");
            }

            return events;
        }
    }
}