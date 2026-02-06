using Microsoft.Extensions.AI;

namespace elbruno.Extensions.AI.Skills.Tests;

public class SkillsChatClientTests : IDisposable
{
    private readonly string _tempDir;

    public SkillsChatClientTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skills-chat-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void CreateSkillDir(string name, string description)
    {
        var skillDir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"),
            $"---\nname: {name}\ndescription: {description}\n---\nInstructions.");
    }

    private FileSystemSkillProvider CreateProvider()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SkillsOptions
        {
            SkillDirectories = [_tempDir],
            AutoDiscover = true
        });
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSystemSkillProvider>();
        return new FileSystemSkillProvider(options, logger);
    }

    [Fact]
    public async Task GetResponseAsync_InjectsSkillsIntoSystemPrompt()
    {
        CreateSkillDir("test-skill", "A test skill");
        var provider = CreateProvider();

        List<ChatMessage>? capturedMessages = null;
        var innerClient = new TestChatClient((messages, options, ct) =>
        {
            capturedMessages = messages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Response")));
        });

        var client = new SkillsChatClient(innerClient, provider);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        await client.GetResponseAsync(messages);

        Assert.NotNull(capturedMessages);
        // Should have injected a system message with available skills
        var systemMsg = capturedMessages!.FirstOrDefault(m => m.Role == ChatRole.System);
        Assert.NotNull(systemMsg);
        Assert.Contains("available_skills", systemMsg.Text);
        Assert.Contains("test-skill", systemMsg.Text);
    }

    [Fact]
    public async Task GetResponseAsync_AppendsToExistingSystemPrompt()
    {
        CreateSkillDir("my-skill", "My skill");
        var provider = CreateProvider();

        List<ChatMessage>? capturedMessages = null;
        var innerClient = new TestChatClient((messages, options, ct) =>
        {
            capturedMessages = messages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Response")));
        });

        var client = new SkillsChatClient(innerClient, provider);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "Hello")
        };

        await client.GetResponseAsync(messages);

        var systemMsg = capturedMessages!.First(m => m.Role == ChatRole.System);
        Assert.Contains("You are a helpful assistant.", systemMsg.Text);
        Assert.Contains("available_skills", systemMsg.Text);
    }

    [Fact]
    public async Task GetResponseAsync_NoSkills_DoesNotInjectEmptyBlock()
    {
        var provider = CreateProvider(); // No skills

        List<ChatMessage>? capturedMessages = null;
        var innerClient = new TestChatClient((messages, options, ct) =>
        {
            capturedMessages = messages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Response")));
        });

        var client = new SkillsChatClient(innerClient, provider);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        await client.GetResponseAsync(messages);

        // No system message should be injected for empty skills
        Assert.DoesNotContain(capturedMessages!, m => m.Role == ChatRole.System);
    }

    /// <summary>
    /// Simple test IChatClient implementation.
    /// </summary>
    private class TestChatClient : IChatClient
    {
        private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> _handler;

        public TestChatClient(Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> handler)
        {
            _handler = handler;
        }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => _handler(messages, options, cancellationToken);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
