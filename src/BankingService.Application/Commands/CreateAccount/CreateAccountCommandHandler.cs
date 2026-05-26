using BankingService.Application.Common;
using BankingService.Application.CQRS;
using BankingService.Domain.Entities;
using BankingService.Domain.ValueObjects;
using BankingService.Infrastructure.Persistence;
using BankingService.Infrastructure.Services;

namespace BankingService.Application.Commands.CreateAccount;

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Result<Guid>>
{
    private readonly BankingDbContext _context;
    private readonly IIbanGenerator _ibanGenerator;

    public CreateAccountCommandHandler(BankingDbContext context, IIbanGenerator ibanGenerator)
    {
        _context = context;
        _ibanGenerator = ibanGenerator;
    }

    public async Task<Result<Guid>> HandleAsync(CreateAccountCommand command, CancellationToken ct,
        bool saveChanges = true)
    {
        var now = DateTime.UtcNow;

        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            FirstName = command.FirstName,
            LastName = command.LastName,
            Iban = _ibanGenerator.Generate(),
            Currency = command.Currency,
            Balance = new Money(command.InitialDeposit, command.Currency),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Accounts.Add(account);

        if (saveChanges)
        {
            await _context.SaveChangesAsync(ct);
        }

        return Result<Guid>.Success(account.AccountId);
    }
}