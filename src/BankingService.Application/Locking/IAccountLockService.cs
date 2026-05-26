namespace BankingService.Application.Locking;

public interface IAccountLockService
{
    Task<IAsyncDisposable> AcquireAsync(Guid accountId, CancellationToken ct);
}