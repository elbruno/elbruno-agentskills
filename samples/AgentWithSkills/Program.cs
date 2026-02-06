using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using elbruno.Extensions.AI.Skills;

// === Sample: Creating an Agent with Skills ===

Console.WriteLine("=== AI Agent with Skills Integration ===\n");

// 1. Set up dependency injection container
var services = new ServiceCollection();

// 2. Add logging (to see what's happening behind the scenes)
services.AddLogging(builder => builder
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));

// 3. Register a mock chat client (in production, use real LLM client like OpenAI, Anthropic, etc.)
services.AddSingleton<IChatClient>(sp => new MockAgentChatClient());

// 4. Register Agent Skills and configure skill directories
var skillsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "skills"));
services.AddAgentSkills(options =>
{
    options.AutoDiscover = true;
})
.WithSkillDirectories(skillsDir)
.WithChatClient(); // Wrap the IChatClient with skills injection middleware

// 5. Build the service provider
var serviceProvider = services.BuildServiceProvider();

// 6. Get the skill-enabled chat client
var chatClient = serviceProvider.GetRequiredService<IChatClient>();
var skillProvider = serviceProvider.GetRequiredService<ISkillProvider>();

// 7. Display available skills
Console.WriteLine("Available Skills:");
var skills = skillProvider.GetSkillMetadata();
foreach (var skill in skills)
{
    Console.WriteLine($"  - {skill.Name}: {skill.Description}");
    if (skill.AllowedTools is not null && skill.AllowedTools.Length > 0)
        Console.WriteLine($"    Allowed Tools: {skill.AllowedTools}");
}
Console.WriteLine();

// 8. Run an agent conversation
Console.WriteLine("=== Agent Conversation ===\n");

var conversationHistory = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful coding assistant. Use the available skills to help the user."),
    new(ChatRole.User, "Can you review the following C# code for security issues?\n\n" +
                       "```csharp\n" +
                       "public void Login(string username, string password)\n" +
                       "{\n" +
                       "    var sql = \"SELECT * FROM Users WHERE Username='\" + username + \"' AND Password='\" + password + \"'\";\n" +
                       "    var result = ExecuteQuery(sql).Result;\n" +
                       "}\n" +
                       "```")
};

Console.WriteLine("User: Can you review the following C# code for security issues?");
Console.WriteLine();

// 9. Get agent response (the SkillsChatClient automatically injects skills into the prompt)
var response = await chatClient.GetResponseAsync(conversationHistory);

Console.WriteLine($"Agent: {response.Text}");
Console.WriteLine();

// 10. Continue the conversation with a follow-up
conversationHistory.Add(response.Messages.Last());
conversationHistory.Add(new ChatMessage(ChatRole.User, "What specific changes should I make?"));

Console.WriteLine("User: What specific changes should I make?");
Console.WriteLine();

var followUpResponse = await chatClient.GetResponseAsync(conversationHistory);
Console.WriteLine($"Agent: {followUpResponse.Text}");
Console.WriteLine();

// 11. Show what was injected into the prompt
Console.WriteLine("=== Behind the Scenes ===");
Console.WriteLine("The SkillsChatClient automatically injected the following skills prompt:\n");
var skillsPrompt = skillProvider.GetAvailableSkillsPrompt();
Console.WriteLine(skillsPrompt);

Console.WriteLine("\nAgent successfully used skills from the skills directory!");

/// <summary>
/// Mock chat client that simulates an AI agent using skills.
/// In production, replace this with a real LLM client (e.g., OpenAI, Anthropic).
/// </summary>
internal class MockAgentChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Check if skills were injected
        var systemMessage = messages.FirstOrDefault(m => m.Role == ChatRole.System);
        var hasSkills = systemMessage?.Text?.Contains("available_skills") ?? false;

        var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "";

        // Simulate agent reasoning based on available skills
        var response = lastUserMessage switch
        {
            var msg when msg.Contains("review") && msg.Contains("code") && hasSkills =>
                "Based on the code-review skill, I've identified several critical issues:\n\n" +
                "1. **SQL Injection Vulnerability**: The code concatenates user input directly into SQL queries. " +
                "Use parameterized queries instead.\n\n" +
                "2. **Blocking Async Call**: Using `.Result` on an async method can cause deadlocks. " +
                "Use `await` instead.\n\n" +
                "3. **Plaintext Password**: The password should be hashed before comparison.\n\n" +
                "These issues are flagged by the code-review skill's security checklist.",

            var msg when msg.Contains("What specific changes") =>
                "Here's the corrected version:\n\n" +
                "```csharp\n" +
                "public async Task<bool> LoginAsync(string username, string passwordHash)\n" +
                "{\n" +
                "    var sql = \"SELECT * FROM Users WHERE Username=@username AND PasswordHash=@passwordHash\";\n" +
                "    using var command = new SqlCommand(sql, connection);\n" +
                "    command.Parameters.AddWithValue(\"@username\", username);\n" +
                "    command.Parameters.AddWithValue(\"@passwordHash\", passwordHash);\n" +
                "    var result = await command.ExecuteReaderAsync();\n" +
                "    return result.HasRows;\n" +
                "}\n" +
                "```\n\n" +
                "Key changes:\n" +
                "- Used parameterized queries to prevent SQL injection\n" +
                "- Made the method async and used await properly\n" +
                "- Accept a password hash instead of plaintext password",

            _ when hasSkills =>
                "I have access to several skills including code review. How can I help you today?",

            _ =>
                "Hello! I'm a helpful assistant. What can I do for you?"
        };

        var responseMessage = new ChatMessage(ChatRole.Assistant, response);
        return Task.FromResult(new ChatResponse(responseMessage));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Streaming not implemented in mock client");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
