using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using elbruno.Extensions.AI.Skills;

// === Sample: Creating an Agent with Skills using Ollama ===

// Configuration — change these to match your Ollama setup
var ollamaEndpoint = "http://localhost:11434";
var ollamaModel = "llama3.2";

Console.WriteLine("=== AI Agent with Skills Integration (Ollama) ===\n");
Console.WriteLine($"Ollama endpoint: {ollamaEndpoint}");
Console.WriteLine($"Model: {ollamaModel}\n");

// 1. Set up dependency injection container
var services = new ServiceCollection();

// 2. Add logging
services.AddLogging(builder => builder
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));

// 3. Register OllamaSharp as the IChatClient
services.AddSingleton<IChatClient>(sp =>
    new OllamaApiClient(ollamaEndpoint, ollamaModel));

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

// 8. Show what will be injected into the prompt
Console.WriteLine("=== Skills Prompt (injected automatically) ===\n");
var skillsPrompt = skillProvider.GetAvailableSkillsPrompt();
Console.WriteLine(skillsPrompt);
Console.WriteLine();

// 9. Load sample code files for the agent to review
var sampleCodeDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "sample-code"));
var sampleFiles = Directory.GetFiles(sampleCodeDir, "*.cs").OrderBy(f => f).ToArray();

Console.WriteLine($"=== Reviewing {sampleFiles.Length} sample files with Ollama ===\n");

foreach (var file in sampleFiles)
{
    var fileName = Path.GetFileName(file);
    var sourceCode = File.ReadAllText(file);

    Console.WriteLine(new string('=', 60));
    Console.WriteLine($"File: {fileName} ({sourceCode.Split('\n').Length} lines)");
    Console.WriteLine(new string('=', 60));

    // 10. Build conversation with the sample code
    var conversationHistory = new List<ChatMessage>
    {
        new(ChatRole.System, "You are a helpful coding assistant. Use the available skills to help the user."),
        new(ChatRole.User, $"Review the following C# code for bugs, security issues, and best practices. Be concise.\n\n```csharp\n{sourceCode}\n```")
    };

    // 11. Get agent response (the SkillsChatClient automatically injects skills into the prompt)
    Console.WriteLine("\nAgent is thinking...\n");
    var response = await chatClient.GetResponseAsync(conversationHistory);

    Console.WriteLine($"Agent:\n{response.Text}");
    Console.WriteLine();
}

Console.WriteLine("\nDone! The agent reviewed all sample files using skills injected by SkillsChatClient middleware.");
