using BankingService.Application.Queries.GetAccountDetails;
using FluentAssertions;

namespace BankingService.Unit.Tests.Queries;

public class GetAccountDetailsQueryHandlerTests : BankingDbContextTestBase
{
    private readonly GetAccountDetailsQueryHandler _sut;

    public GetAccountDetailsQueryHandlerTests()
    {
        _sut = new GetAccountDetailsQueryHandler(Context);
    }

    [Fact]
    public async Task HandleAsync_AccountExists_ReturnsAccountDto()
    {
        var account = CreateAccount();
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountDetailsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.AccountId.Should().Be(account.AccountId);
        result.Value?.FirstName.Should().Be(account.FirstName);
        result.Value?.LastName.Should().Be(account.LastName);
        result.Value?.Iban.Should().Be(account.Iban);
        result.Value?.Balance.Amount.Should().Be(account.Balance.Amount);
        result.Value?.Balance.Currency.Should().Be(account.Balance.Currency);
        result.Value?.CreatedAt.Should().Be(account.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new GetAccountDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == "Account not found.");
    }

    [Fact]
    public async Task HandleAsync_InactiveAccount_ReturnsAccountDto()
    {
        var account = CreateAccount(isActive: false);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountDetailsQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.AccountId.Should().Be(account.AccountId);
    }
}
