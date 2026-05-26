using BankingService.Application.Behaviours;
using BankingService.Application.CQRS;
using BankingService.Infrastructure;
using BankingService.Infrastructure.Locking;
using BankingService.Infrastructure.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);

        services.AddSingleton<IAccountLockService, AccountLockService>();
        services.AddSingleton<IIbanGenerator, IbanGenerator>();

        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.Scan(selector => selector
            .FromAssemblyOf<ICommandDispatcher>()
            .AddClasses(f => f.AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        if (services.Any(s => s.ServiceType.IsGenericType &&
                              s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) &&
                              s.ImplementationType != null &&
                              !s.ImplementationType.IsGenericTypeDefinition))
        {
            services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
        }

        services.AddValidatorsFromAssemblyContaining<ICommandDispatcher>();

        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}