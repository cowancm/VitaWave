using Microsoft.AspNetCore.Mvc;
using System.Data.SQLite;

public class UserData
{
    public string ModuleID { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

[ApiController]
[Route("[controller]")]
public class UserDatabaseController : ControllerBase
{
    private readonly string _dbPath;

    public UserDatabaseController()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var projectRoot = Path.Combine(userProfile, "VitaWave", "back", "src", "VitaWave.WebAPI");

        var dbFolder = Path.Combine(projectRoot, "Database");
        if (!Directory.Exists(dbFolder))
            Directory.CreateDirectory(dbFolder);

        _dbPath = Path.Combine(dbFolder, "UserData.db");

        EnsureTableExists();
    }

    private void EnsureTableExists()
    {
        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = new SQLiteCommand(con);
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS UserData (
                ModuleID TEXT PRIMARY KEY,
                Email TEXT NOT NULL,
                Password TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    // CREATE
    [HttpPost("create")]
    public IActionResult CreateUser([FromBody] UserData user)
    {
        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = new SQLiteCommand(con);
        cmd.CommandText = "INSERT INTO UserData(ModuleID, Email, Password) VALUES(@m, @e, @p)";
        cmd.Parameters.AddWithValue("@m", user.ModuleID);
        cmd.Parameters.AddWithValue("@e", user.Email);
        cmd.Parameters.AddWithValue("@p", user.Password);

        try
        {
            cmd.ExecuteNonQuery();
            return Ok(new { message = "User created successfully." });
        }
        catch (SQLiteException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // READ ALL
    [HttpGet("read")]
    public IActionResult ReadUsers()
    {
        var list = new List<UserData>();

        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = new SQLiteCommand("SELECT ModuleID, Email, Password FROM UserData", con);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new UserData
            {
                ModuleID = reader["ModuleID"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? "",
                Password = reader["Password"].ToString() ?? ""
            });
        }

        return Ok(list);
    }

    // UPDATE
    [HttpPut("update")]
    public IActionResult UpdateUser([FromBody] UserData user)
    {
        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = new SQLiteCommand(con);
        cmd.CommandText = "UPDATE UserData SET Email=@e, Password=@p WHERE ModuleID=@m";
        cmd.Parameters.AddWithValue("@m", user.ModuleID);
        cmd.Parameters.AddWithValue("@e", user.Email);
        cmd.Parameters.AddWithValue("@p", user.Password);

        int rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound(new { error = "User not found." });
        return Ok(new { message = "User updated successfully." });
    }

    // DELETE
    [HttpDelete("delete")]
    public IActionResult DeleteUser([FromQuery] string moduleID)
    {
        using var con = new SQLiteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = new SQLiteCommand(con);
        cmd.CommandText = "DELETE FROM UserData WHERE ModuleID=@m";
        cmd.Parameters.AddWithValue("@m", moduleID);

        int rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound(new { error = "User not found." });
        return Ok(new { message = "User deleted successfully." });
    }
}
