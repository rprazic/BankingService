using Microsoft.Extensions.DependencyInjection;

namespace BankingService.Application.CQRS;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _provider;

    public CommandDispatcher(IServiceProvider provider) => _provider = provider;

    public Task<TResult> DispatchAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct, bool saveChanges = true)
        where TCommand : ICommand<TResult>
    {
        var handler = _provider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return handler.HandleAsync(command, ct, saveChanges);
    }
}