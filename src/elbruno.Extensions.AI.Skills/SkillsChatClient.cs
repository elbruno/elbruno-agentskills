using Microsoft.Extensions.AI;

namespace elbruno.Extensions.AI.Skills;

/// <summary>
/// A delegating chat client that injects &lt;available_skills&gt; into system prompts.
/// </summary>
public class SkillsChatClient : DelegatingChatClient
{
    private readonly ISkillProvider _skillProvider;

    public SkillsChatClient(IChatClient innerClient, ISkillProvider skillProvider)
        : base(innerClient)
    {
        _skillProvider = skillProvider;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = InjectSkillsPrompt(messages);
        return await base.GetResponseAsync(messageList, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = InjectSkillsPrompt(messages);
        return base.GetStreamingResponseAsync(messageList, options, cancellationToken);
    }

    private List<ChatMessage> InjectSkillsPrompt(IEnumerable<ChatMessage> messages)
    {
        var skillsPrompt = _skillProvider.GetAvailableSkillsPrompt();
        var messageList = messages.ToList();

        if (string.IsNullOrWhiteSpace(skillsPrompt) || skillsPrompt == "<available_skills>\n</available_skills>")
            return messageList;

        // Find or create system message and append skills
        var systemMessage = messageList.FirstOrDefault(m => m.Role == ChatRole.System);
        if (systemMessage is not null)
        {
            var existingText = systemMessage.Text ?? "";
            systemMessage.Contents = [new TextContent($"{existingText}\n\n{skillsPrompt}")];
        }
        else
        {
            messageList.Insert(0, new ChatMessage(ChatRole.System, skillsPrompt));
        }

        return messageList;
    }
}
