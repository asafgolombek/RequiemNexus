using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Registers all <see cref="ISeeder"/> implementations for startup database initialization.
/// </summary>
public static class RequiemDataSeedingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Requiem Nexus reference-data seeders to the service collection (singleton, stateless).
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddRequiemDataSeeders(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, ClanAndDisciplineSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, HuntingPoolSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, MeritSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, EquipmentSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, CovenantSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, CovenantDefinitionMeritSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, BloodlineSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, DisciplineAcquisitionMetadataSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, DevotionSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, SorceryRiteSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, BloodSorceryExtensionSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, CoilSeeder>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeeder, PrebuiltNpcSeeder>());
        return services;
    }
}
