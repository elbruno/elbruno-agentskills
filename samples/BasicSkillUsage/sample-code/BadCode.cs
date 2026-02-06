namespace SampleCode;

/// <summary>
/// A user service with some issues.
/// </summary>
public class DataProcessor
{
    private string connectionString;

    public DataProcessor()
    {
        // Warning: no null check, no readonly
        connectionString = "";
    }

    // Warning: synchronous method blocks the thread
    public string GetData(string query)
    {
        var client = new HttpClient(); // Warning: HttpClient not disposed, should be injected
        var result = client.GetStringAsync($"https://api.example.com/data?q={query}").Result; // Warning: .Result blocks async
        return result;
    }

    // Warning: returns mutable list instead of IReadOnlyList
    public List<string> ProcessItems(List<string> items)
    {
        var results = new List<string>();
        // Warning: no null check on items parameter
        for (int i = 0; i <= items.Count; i++) // Bug: off-by-one error (should be < not <=)
        {
            try
            {
                results.Add(items[i].ToUpper());
            }
            catch (Exception) // Warning: swallowed exception
            {
            }
        }
        return results;
    }
}
