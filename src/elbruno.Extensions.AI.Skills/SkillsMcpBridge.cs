using elbruno.Extensions.AI.Skills.Core;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace elbruno.Extensions.AI.Skills;

/// <summary>
/// Bridge that exposes discovered Agent Skills as MCP resources.
/// Skills are exposed as resources with their SKILL.md content, making them
/// accessible to MCP clients.
/// </summary>
public class SkillsMcpBridge
{
    private readonly ISkillProvider _skillProvider;
    private readonly ILogger<SkillsMcpBridge> _logger;

    public SkillsMcpBridge(ISkillProvider skillProvider, ILogger<SkillsMcpBridge> logger)
    {
        _skillProvider = skillProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets MCP resource definitions for all discovered skills.
    /// Each skill is exposed as a resource with URI format: skill://{name}/SKILL.md
    /// </summary>
    public IReadOnlyList<Resource> GetSkillResources()
    {
        return _skillProvider.GetSkillMetadata()
            .Select(props => new Resource
            {
                Name = props.Name,
                Uri = $"skill://{props.Name}/SKILL.md",
                Description = props.Description,
                MimeType = "text/markdown"
            })
            .ToList();
    }

    /// <summary>
    /// Reads a skill resource by name, returning the full SKILL.md content.
    /// </summary>
    public ResourceContents? ReadSkillResource(string skillName)
    {
        var skill = _skillProvider.GetSkill(skillName);
        if (skill is null)
        {
            _logger.LogWarning("Skill '{SkillName}' not found", skillName);
            return null;
        }

        var fullContent = $"---\nname: {skill.Properties.Name}\ndescription: {skill.Properties.Description}\n---\n{skill.Body}";

        return new TextResourceContents
        {
            Uri = $"skill://{skillName}/SKILL.md",
            MimeType = "text/markdown",
            Text = fullContent
        };
    }

    /// <summary>
    /// Configures MCP server handlers to expose skills as resources.
    /// </summary>
    public void ConfigureHandlers(McpServerHandlers handlers)
    {
        handlers.ListResourcesHandler = (request, cancellationToken) =>
        {
            var resources = GetSkillResources();
            return ValueTask.FromResult(new ListResourcesResult { Resources = [.. resources] });
        };

        handlers.ReadResourceHandler = (request, cancellationToken) =>
        {
            var uri = request.Params?.Uri ?? throw new McpProtocolException("Missing resource URI", McpErrorCode.InvalidParams);
            var skillName = ExtractSkillName(uri);
            var content = ReadSkillResource(skillName)
                ?? throw new McpProtocolException($"Skill not found: {skillName}", McpErrorCode.InvalidRequest);

            return ValueTask.FromResult(new ReadResourceResult { Contents = [content] });
        };
    }

    private static string ExtractSkillName(string uri)
    {
        // Expected format: skill://{name}/SKILL.md
        if (uri.StartsWith("skill://"))
        {
            var parts = uri["skill://".Length..].Split('/');
            if (parts.Length > 0)
                return parts[0];
        }
        return uri;
    }
}
