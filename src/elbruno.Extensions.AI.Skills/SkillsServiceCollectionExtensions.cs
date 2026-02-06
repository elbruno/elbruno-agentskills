using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace elbruno.Extensions.AI.Skills;

/// <summary>
/// Extension methods for registering Agent Skills services.
/// </summary>
public static class SkillsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Agent Skills services to the dependency injection container.
    /// </summary>
    public static ISkillsBuilder AddAgentSkills(this IServiceCollection services, Action<SkillsOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton<ISkillProvider, FileSystemSkillProvider>();

        return new SkillsBuilder(services);
    }
}

/// <summary>
/// Builder for configuring Agent Skills services.
/// </summary>
public interface ISkillsBuilder
{
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds skill directories to scan for skills.
    /// </summary>
    ISkillsBuilder WithSkillDirectories(params string[] directories);

    /// <summary>
    /// Wraps an existing <see cref="IChatClient"/> with skills injection.
    /// </summary>
    ISkillsBuilder WithChatClient();
}

internal class SkillsBuilder : ISkillsBuilder
{
    public IServiceCollection Services { get; }

    public SkillsBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public ISkillsBuilder WithSkillDirectories(params string[] directories)
    {
        Services.Configure<SkillsOptions>(o => o.SkillDirectories.AddRange(directories));
        return this;
    }

    public ISkillsBuilder WithChatClient()
    {
        Services.Decorate<IChatClient>((inner, sp) =>
        {
            var skillProvider = sp.GetRequiredService<ISkillProvider>();
            return new SkillsChatClient(inner, skillProvider);
        });
        return this;
    }
}

/// <summary>
/// Extension to support decorator pattern for IChatClient.
/// </summary>
internal static class ServiceCollectionDecoratorExtensions
{
    public static void Decorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator)
        where TService : class
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is null)
            throw new InvalidOperationException($"No service of type {typeof(TService).Name} has been registered.");

        services.Remove(descriptor);

        services.Add(ServiceDescriptor.Describe(
            typeof(TService),
            sp =>
            {
                var inner = descriptor.ImplementationFactory is not null
                    ? (TService)descriptor.ImplementationFactory(sp)
                    : descriptor.ImplementationInstance is not null
                        ? (TService)descriptor.ImplementationInstance
                        : (TService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
                return decorator(inner, sp);
            },
            descriptor.Lifetime));
    }
}
