namespace BankingService.Application.CQRS;

public interface ICommandDispatcher
{
    Task<TResult> DispatchAsync<TCommand, TResult>(
        TCommand command, CancellationToken ct, bool saveChanges = true)
        where TCommand : ICommand<TResult>;
}