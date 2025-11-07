using VitaWave.Common;
using VitaWave.Data;
using System.Data.SQLite;

namespace VitaWave.WebAPI.Database
{
    public class DataBase
    {

        static DataBase()
            {
                
            }

        public void EventTable()
        {
            // 1. Connect to SQLite database (creates file if it doesn’t exist)
            string cs = "Data Source=EventTable.db;Version=3;";
            using var con = new SQLiteConnection(cs);
            con.Open();

            // 2. Create a table
            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS eventTable (
                        ModuleID TEXT NOT NULL,
                        tid TEXT NOT NULL,
                        event TEXT UNIQUE NOT NULL,
                        criticality INTEGER CHECK(criticality BETWEEN 1 AND 10)
                    )";
            cmd.ExecuteNonQuery();

            // 3. Insert data
            cmd.CommandText = "INSERT INTO users(name, email, age) VALUES(@name, @email, @age)";
            cmd.Parameters.AddWithValue("@name", "Alice");
            cmd.Parameters.AddWithValue("@email", "alice@example.com");
            cmd.Parameters.AddWithValue("@age", 25);
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
            cmd.CommandText = "INSERT INTO users(name, email, age) VALUES(@name, @email, @age)";
            cmd.Parameters.AddWithValue("@name", "Bob");
            cmd.Parameters.AddWithValue("@email", "bob@example.com");
            cmd.Parameters.AddWithValue("@age", 30);
            cmd.ExecuteNonQuery();

            // 4. Query data
            cmd.CommandText = "SELECT id, name, email, age FROM users";
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            Console.WriteLine("Users:");
            while (rdr.Read())
            {
                Console.WriteLine($"{rdr["id"]}: {rdr["name"]} ({rdr["email"]}), Age {rdr["age"]}");
            }
        }
    }
}
