using System.Collections.Concurrent;

namespace BankingService.Infrastructure.Locking;

public class AccountLockService : IAccountLockService
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IAsyncDisposable> AcquireAsync(Guid accountId, CancellationToken ct)
    {
        var semaphore = _locks.GetOrAdd(accountId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        return new LockReleaser(semaphore);
    }

    private sealed class LockReleaser : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public LockReleaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}