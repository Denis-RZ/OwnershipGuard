using Microsoft.Extensions.DependencyInjection;

namespace OwnershipGuard;

/// <summary>
/// DI extensions for registering OwnershipGuard services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OwnershipGuard services (options, access guard, and descriptor registry).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional configuration of options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOwnershipGuard(
        this IServiceCollection services,
        Action<OwnershipGuardOptions>? configure = null)
    {
        services.AddOptions<OwnershipGuardOptions>();
        if (configure != null)
            services.Configure(configure);
        services.AddScoped<IAccessGuard, AccessGuard>();
        services.AddSingleton<IOwnershipDescriptorRegistry, OwnershipDescriptorRegistry>();
        return services;
    }
}
