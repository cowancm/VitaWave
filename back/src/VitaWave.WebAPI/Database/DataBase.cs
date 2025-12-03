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
        public string Timestamp { get; set; } = "";
    }


    public class DataBase
    {
        private readonly string _dbPath;

        public DataBase(DataFacilitator data)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var projectRoot = Path.Combine(userProfile, "vitawave");

            data.EventRaise += HandleEventFromAlgo;

            string dbFolder = Path.Combine(projectRoot, "Database");
            Directory.CreateDirectory(dbFolder);

            _dbPath = Path.Combine(dbFolder, "EventTable.db");

            Console.WriteLine("[DataBase.cs] Database Path: " + _dbPath);

            CreateTable();
        }

        private string GetConnectionString()
        {
            return "Data Source=" + _dbPath + ";Version=3;";
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
                timestamp TEXT NOT NULL
            );";
            cmd.ExecuteNonQuery();

            Console.WriteLine("[DataBase.cs] Table created successfully.");
        }

        public void HandleEventFromAlgo(object? s, ResultEvent e)
        {
            if (e == null)
            {
                Console.WriteLine("[DataBase.cs] Received null event");
                return;
            }

            Console.WriteLine("[DataBase.cs] Inserting - ModuleID: " + e.ModuleID + ", TID: " + e.TID);

            try
            {
                using var con = new SQLiteConnection(GetConnectionString());
                con.Open();

                using var cmd = new SQLiteCommand(con);
                cmd.CommandText = "INSERT INTO EventTable(ModuleID, tid, event, criticality, timestamp) VALUES(@m, @t, @e, @c, @ts)";

                cmd.Parameters.AddWithValue("@m", e.ModuleID ?? "Unknown");
                cmd.Parameters.AddWithValue("@t", e.TID.ToString());
                cmd.Parameters.AddWithValue("@e", e.ResultId.ToString());
                cmd.Parameters.AddWithValue("@c", 1);
                cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
                Console.WriteLine("[DataBase.cs] Event inserted successfully");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("[DataBase.cs] SQLite ERROR: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DataBase.cs] ERROR: " + ex.Message);
            }
        }

        public List<eventData> GetAllEvents()
        {
            var events = new List<eventData>();

            using var con = new SQLiteConnection(GetConnectionString());
            con.Open();

            using var cmd = new SQLiteCommand("SELECT ModuleID, tid, event, criticality, timestamp FROM EventTable", con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                events.Add(new eventData
                {
                    ModuleID = rdr["ModuleID"].ToString() ?? "",
                    Tid = rdr["tid"].ToString() ?? "",
                    Event = rdr["event"].ToString() ?? "",
                    Criticality = Convert.ToInt32(rdr["criticality"]),
                    Timestamp = rdr["timestamp"].ToString() ?? ""
                });
            }

            Console.WriteLine("[DataBase.cs] Retrieved " + events.Count + " events.");
            return events;
        }

    }
}