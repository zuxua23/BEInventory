namespace InventoryControl.Utility;

using System.Reflection;
// Extension method for registering all services automatically
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var implementations = assembly.GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                !typeof(IHostedService).IsAssignableFrom(type));

        foreach (var implementation in implementations)
        {
            var interfaces = implementation.GetInterfaces()
                .Where(i => i.Name.EndsWith("Service"));

            foreach (var service in interfaces)
            {
                services.AddScoped(service, implementation);
            }
        }

        return services;
    }
}