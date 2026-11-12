using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using D2ApiCreator.Application.Common.Behaviours;

namespace D2ApiCreator.Application;

/// <summary>
/// Dependency Injection configuration for Application layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register Pipeline Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}

