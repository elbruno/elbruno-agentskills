namespace SampleCode;

using System.Data.SqlClient;

public class admin_panel
{
    public static string DB_PASSWORD = "SuperSecret123!"; // Critical: hardcoded password
    static string connStr = "Server=prod-db;Database=users;User=sa;Password=SuperSecret123!;"; // Critical: hardcoded connection string

    // Critical: SQL injection vulnerability
    public string get_user(string userName)
    {
        var conn = new SqlConnection(connStr); // Critical: connection never disposed
        conn.Open();
        var cmd = new SqlCommand("SELECT * FROM Users WHERE Name = '" + userName + "'", conn); // Critical: SQL injection
        var reader = cmd.ExecuteReader(); // Critical: reader never disposed
        if (reader.Read())
            return reader["Name"].ToString();
        return null; // Warning: nullable not annotated
    }

    // Critical: path traversal vulnerability
    public string ReadFile(string fileName)
    {
        return File.ReadAllText("C:\\data\\" + fileName); // Critical: path traversal, no validation
    }

    // Multiple issues: naming, async misuse, no error handling
    public void do_everything(string DATA)
    {
        var client = new HttpClient();
        // Critical: fire-and-forget async, no await, no disposal
        client.PostAsync("http://api.example.com/log", // Warning: HTTP instead of HTTPS
            new StringContent(DATA));

        // Warning: thread sleep in production code
        Thread.Sleep(5000);

        // Bug: using the password field as generic storage
        DB_PASSWORD = DATA;
    }

    // Warning: finalizer without IDisposable
    ~admin_panel()
    {
        Console.WriteLine("Destroyed");
    }
}
