using DataBaseTestApp;
using Microsoft.AspNetCore.Mvc;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Text;

namespace SQLiteTestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private static bool _loggingEnabled = false;
        private readonly string _dbPath;
        private readonly string _exportPath;
        private readonly DataBase _db;

        public EventController()
        {
            // Locate project root
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.Name != "DataBaseTestApp")
                dir = dir.Parent;

            if (dir == null)
                throw new DirectoryNotFoundException("Could not locate project root folder.");

            var projectRoot = dir.FullName;
            _dbPath = Path.Combine(projectRoot, "Database", "EventTable.db");
            _exportPath = Path.Combine(projectRoot, "Exports");

            Console.WriteLine($"[DEBUG] Using database at: {_dbPath}");
            Console.WriteLine($"[DEBUG] Export folder at: {_exportPath}");

            _db = new DataBase();
        }

        [HttpPost("start")]
        public IActionResult StartLogging()
        {
            _loggingEnabled = true;
            return Ok("Logging started.");
        }

        [HttpPost("stop")]
        public IActionResult StopLogging()
        {
            _loggingEnabled = false;
            return Ok("Logging stopped.");
        }

        //Manually insert values into database for testing purposes
        [HttpPost("insert")]
        public IActionResult InsertEvent(string moduleId, string tid, string evt, int criticality)
        {
            if (!_loggingEnabled)
                return BadRequest("Logging is disabled. Start logging first.");

            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = @"INSERT INTO EventTable(ModuleID, tid, event, criticality) 
                                VALUES(@m, @t, @e, @c)";
            cmd.Parameters.AddWithValue("@m", moduleId);
            cmd.Parameters.AddWithValue("@t", tid);
            cmd.Parameters.AddWithValue("@e", evt);
            cmd.Parameters.AddWithValue("@c", criticality);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                return BadRequest($"Insert failed: {ex.Message}");
            }

            return Ok("Event inserted.");
        }

        [HttpPost("clear")]
        public IActionResult ClearDatabase()
        {
            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            using var cmd = new SQLiteCommand("DELETE FROM EventTable", con);
            cmd.ExecuteNonQuery();

            return Ok("Database cleared.");
        }

        //Read the database
        [HttpGet("read")]
        public IActionResult ReadEvents(
            [FromQuery] string? tid = null,
            [FromQuery] string? moduleId = null,
            [FromQuery] string? evt = null,
            [FromQuery] int? minCriticality = null,
            [FromQuery] int? maxCriticality = null)
        {
            var events = GetFilteredEvents(tid, moduleId, evt, minCriticality, maxCriticality);
            if (events.Count == 0)
                return Ok("No rows match the given filters.");

            return Ok(events);
        }

       //save the current database even if filtered
        [HttpPost("save-csv")]
        public IActionResult SaveCsvAndDownload(
            [FromQuery] string? tid = null,
            [FromQuery] string? moduleId = null,
            [FromQuery] string? evt = null,
            [FromQuery] int? minCriticality = null,
            [FromQuery] int? maxCriticality = null)
        {
            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);

            var filteredRows = GetFilteredEvents(tid, moduleId, evt, minCriticality, maxCriticality);
            if (filteredRows.Count == 0)
                return BadRequest("No data found for the given filters.");

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("ModuleID,tid,event,criticality");

            // Rows
            foreach (var row in filteredRows)
            {
                sb.AppendLine($"{row.ModuleID},{row.Tid},{row.Event.Replace(",", ";")},{row.Criticality}");
            }

            // Save file
            string filterTag = $"{(tid ?? "AllTids")}_{(moduleId ?? "AllModules")}_{DateTime.Now:yyyyMMdd_HHmmss}";
            string fileName = $"EventTable_{filterTag}.csv";
            string filePath = Path.Combine(_exportPath, fileName);
            System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            // Delete filtered rows
            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            var whereClause = BuildWhereClause(tid, moduleId, evt, minCriticality, maxCriticality, out var parameters);
            var deleteCmd = new SQLiteCommand($"DELETE FROM EventTable {whereClause}", con);
            foreach (var p in parameters) deleteCmd.Parameters.Add(p);
            deleteCmd.ExecuteNonQuery();

            // Return CSV as download
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", fileName);
        }

        // Helper to build dynamic WHERE clause + fetch data
        private List<(string ModuleID, string Tid, string Event, int Criticality)> GetFilteredEvents(
            string? tid, string? moduleId, string? evt, int? minCriticality, int? maxCriticality)
        {
            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            var whereClause = BuildWhereClause(tid, moduleId, evt, minCriticality, maxCriticality, out var parameters);
            var cmd = new SQLiteCommand($"SELECT ModuleID, tid, event, criticality FROM EventTable {whereClause}", con);
            foreach (var p in parameters) cmd.Parameters.Add(p);

            using var reader = cmd.ExecuteReader();
            var list = new List<(string ModuleID, string Tid, string Event, int Criticality)>();

            while (reader.Read())
            {
                list.Add((
                    reader["ModuleID"].ToString() ?? "",
                    reader["tid"].ToString() ?? "",
                    reader["event"].ToString() ?? "",
                    Convert.ToInt32(reader["criticality"])
                ));
            }

            return list;
        }

        private static string BuildWhereClause(
            string? tid, string? moduleId, string? evt, int? minCriticality, int? maxCriticality,
            out List<SQLiteParameter> parameters)
        {
            var clauses = new List<string>();
            parameters = new List<SQLiteParameter>();

            if (!string.IsNullOrEmpty(tid))
            {
                clauses.Add("tid LIKE '%' || @tid || '%'");
                parameters.Add(new SQLiteParameter("@tid", tid));
            }

            if (!string.IsNullOrEmpty(moduleId))
            {
                clauses.Add("ModuleID LIKE '%' || @moduleId || '%'");
                parameters.Add(new SQLiteParameter("@moduleId", moduleId));
            }

            if (!string.IsNullOrEmpty(evt))
            {
                clauses.Add("event LIKE '%' || @evt || '%'");
                parameters.Add(new SQLiteParameter("@evt", evt));
            }

            if (minCriticality.HasValue)
            {
                clauses.Add("criticality >= @min");
                parameters.Add(new SQLiteParameter("@min", minCriticality));
            }

            if (maxCriticality.HasValue)
            {
                clauses.Add("criticality <= @max");
                parameters.Add(new SQLiteParameter("@max", maxCriticality));
            }

            return clauses.Count > 0 ? "WHERE " + string.Join(" AND ", clauses) : "";
        }
    }
}
