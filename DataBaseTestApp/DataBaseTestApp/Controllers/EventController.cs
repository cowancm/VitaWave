using AlgoTestClass;
using DataBaseTestApp;
using Microsoft.AspNetCore.Mvc;
using System.Data.SQLite;
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
        private readonly Algo _algo;
        private readonly DataBase _db;

        public EventController()
        {
            // ?? Start from bin folder and walk up until project folder is found
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir != null && dir.Name != "DataBaseTestApp") // <-- change if your project folder is named differently
            {
                dir = dir.Parent;
            }

            if (dir == null)
                throw new DirectoryNotFoundException("Could not locate project root folder.");

            var projectRoot = dir.FullName;

            // ?? Build paths relative to project root
            _dbPath = Path.Combine(projectRoot, "Database", "EventTable.db");
            _exportPath = Path.Combine(projectRoot, "Exports");

            // Optional: log resolved path for debugging
            Console.WriteLine($"[DEBUG] Using database at: {_dbPath}");
            Console.WriteLine($"[DEBUG] Export folder at: {_exportPath}");

            _algo = new Algo();
            _db = new DataBase(_algo,_dbPath); // subscribe DB to algo events
        }

        [HttpPost("run")]
        public IActionResult RunAlgo()
        {
            _algo.RunAlgo(); // just runs the algorithm
            return Ok("Algo.RunAlgo() executed and events (if any) have been handled.");
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

        [HttpPost("insert")]
        public IActionResult InsertEvent(int Id, string moduleId, string tid, string evt, int criticality)
        {
            if (!_loggingEnabled)
                return BadRequest("Logging is disabled. Start logging first.");

            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = @"INSERT INTO EventTable(Id,ModuleID, tid, event, criticality) 
                                VALUES(@i,@m, @t, @e, @c)";
            cmd.Parameters.AddWithValue("@i", Id);
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

        [HttpGet("read")]
        public IActionResult ReadEvents()
        {
            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            string sql = "SELECT * FROM EventTable";
            using var cmd = new SQLiteCommand(sql, con);
            using var reader = cmd.ExecuteReader();

            var events = new List<object>();

            while (reader.Read())
            {
                events.Add(new
                {
                    Id = reader["Id"],
                    ModuleID = reader["ModuleID"],
                    Tid = reader["tid"],
                    Event = reader["event"],
                    Criticality = reader["criticality"]
                });
            }

            if (events.Count == 0)
                return Ok("No rows in EventTable.");

            return Ok(events);
        }

        [HttpPost("save-csv")]
        public IActionResult SaveCsv()
        {
            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);

            var sb = new StringBuilder();

            using var con = new SQLiteConnection($"Data Source={_dbPath}");
            con.Open();

            string sql = "SELECT * FROM EventTable";
            using var cmd = new SQLiteCommand(sql, con);
            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows)
                return BadRequest("No data in table to save.");

            // Header
            for (int i = 0; i < reader.FieldCount; i++)
            {
                sb.Append(reader.GetName(i));
                if (i < reader.FieldCount - 1) sb.Append(",");
            }
            sb.AppendLine();

            // Rows
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    sb.Append(reader[i]?.ToString()?.Replace(",", ";"));
                    if (i < reader.FieldCount - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            // Save file
            string fileName = $"EventTable_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(_exportPath, fileName);
            System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            // Clear the table after saving
            reader.Close();
            using var clearCmd = new SQLiteCommand("DELETE FROM EventTable", con);
            clearCmd.ExecuteNonQuery();

            return Ok(new { message = "CSV saved on server and table cleared", file = fileName });
        }

        [HttpGet("list-csvs")]
        public IActionResult ListCsvFiles()
        {
            if (!Directory.Exists(_exportPath))
                return Ok(Array.Empty<string>());

            var files = Directory.GetFiles(_exportPath, "*.csv")
                                 .Select(Path.GetFileName)
                                 .ToArray();
            return Ok(files);
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadCsv(string fileName)
        {
            string filePath = Path.Combine(_exportPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found.");

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "text/csv", fileName);
        }
    }
}
