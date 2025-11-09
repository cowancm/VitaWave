using Microsoft.AspNetCore.Mvc;
using System.Data.SQLite;
using System.Text;
using VitaWave.Data;
using VitaWave.Common;
using VitaWave.DataBase;

[ApiController]
[Route("[controller]")]
public class DatabaseController : ControllerBase
{
    private static bool _loggingEnabled = false;
    private readonly string _dbPath;
    private readonly string _exportPath;

    public DatabaseController()
    {
        try
        {
            // Get user profile path: C:\Users\Ashto
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Build the path: C:\Users\Ashto\VitaWave\back\src\VitaWave.WebAPI
            var projectRoot = Path.Combine(userProfile, "VitaWave", "back", "src", "VitaWave.WebAPI");

            // Construct paths
            _dbPath = Path.Combine(projectRoot, "Database", "EventTable.db");
            _exportPath = Path.Combine(projectRoot, "Exports");

            // Create database folder if missing
            var dbFolder = Path.Combine(projectRoot, "Database");
            if (!Directory.Exists(dbFolder))
                Directory.CreateDirectory(dbFolder);

            // Create export folder if missing
            if (!Directory.Exists(_exportPath))
                Directory.CreateDirectory(_exportPath);

            Console.WriteLine($"[DatabaseController] Database Path: {_dbPath}");
            Console.WriteLine($"[DatabaseController] Export Path: {_exportPath}");

            // Ensure table exists (in case DataBase.cs hasn't run yet)
            EnsureTableExists();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatabaseController] ERROR in constructor: {ex.Message}");
            throw;
        }
    }

    private void EnsureTableExists()
    {
        // This creates the table if it doesn't exist
        // Safe to call multiple times - won't overwrite existing data
        using var con = new SQLiteConnection($"Data Source={_dbPath}");
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
    public IActionResult InsertEvent([FromBody] ResultEvent ev)
    {
        if (!_loggingEnabled)
            return BadRequest(new { error = "Logging is disabled. Start logging first." });

        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = new SQLiteCommand(con);
        cmd.CommandText = @"INSERT INTO EventTable(ModuleID, tid, event, criticality) 
                        VALUES(@m, @t, @e, @c)";
        cmd.Parameters.AddWithValue("@m", ev.ModuleID);
        cmd.Parameters.AddWithValue("@t", ev.TID);
        cmd.Parameters.AddWithValue("@e", ev.ResultId.ToString());
        cmd.Parameters.AddWithValue("@c", 1);

        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (SQLiteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok(new { message = "Event inserted." });
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
    public IActionResult ReadEvents(
        [FromQuery] string? tid = null,
        [FromQuery] string? moduleId = null,
        [FromQuery] string? evt = null,
        [FromQuery] int? minCriticality = null,
        [FromQuery] int? maxCriticality = null)
    {
        try
        {
            var events = GetFilteredEvents(tid, moduleId, evt, minCriticality, maxCriticality);
            if (events.Count == 0)
                return Ok(events);

            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error reading events: {ex.Message}");
        }
    }

    [HttpPost("save-csv")]
    [HttpGet("save-csv")]
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
        sb.AppendLine("ModuleID,tid,event,criticality");
        foreach (var row in filteredRows)
        {
            sb.AppendLine($"{row.ModuleID},{row.Tid},{row.Event.Replace(",", ";")},{row.Criticality}");
        }

        string filterTag = $"{(tid ?? "AllTids")}_{(moduleId ?? "AllModules")}_{DateTime.Now:yyyyMMdd_HHmmss}";
        string fileName = $"EventTable_{filterTag}.csv";
        string filePath = Path.Combine(_exportPath, fileName);
        System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        var whereClause = BuildWhereClause(tid, moduleId, evt, minCriticality, maxCriticality, out var parameters);
        var deleteCmd = new SQLiteCommand($"DELETE FROM EventTable {whereClause}", con);
        foreach (var p in parameters) deleteCmd.Parameters.Add(p);
        deleteCmd.ExecuteNonQuery();

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", fileName);
    }

    private List<eventData> GetFilteredEvents(
        string? tid, string? moduleId, string? evt, int? minCriticality, int? maxCriticality)
    {
        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        var whereClause = BuildWhereClause(tid, moduleId, evt, minCriticality, maxCriticality, out var parameters);
        var cmd = new SQLiteCommand($"SELECT ModuleID, tid, event, criticality FROM EventTable {whereClause}", con);
        foreach (var p in parameters) cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();
        var list = new List<eventData>();
        while (reader.Read())
        {
            list.Add(new eventData
            {
                ModuleID = reader["ModuleID"].ToString() ?? "",
                Tid = reader["tid"].ToString() ?? "",
                Event = reader["event"].ToString() ?? "",
                Criticality = Convert.ToInt32(reader["criticality"])
            });
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