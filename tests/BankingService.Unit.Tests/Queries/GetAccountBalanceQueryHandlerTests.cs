using BankingService.Application.Queries.GetAccountBalance;
using BankingService.Domain.Enums;
using FluentAssertions;

namespace BankingService.Unit.Tests.Queries;

public class GetAccountBalanceQueryHandlerTests : BankingDbContextTestBase
{
    private readonly GetAccountBalanceQueryHandler _sut;

    public GetAccountBalanceQueryHandlerTests()
    {
        _sut = new GetAccountBalanceQueryHandler(Context);
    }

    [Fact]
    public async Task HandleAsync_AccountExists_ReturnsBalanceDto()
    {
        var account = CreateAccount(balance: 2500m);
        Context.Accounts.Add(account);
        await Context.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetAccountBalanceQuery(account.AccountId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value?.AccountId.Should().Be(account.AccountId);
        result.Value?.Balance.Amount.Should().Be(2500m);
        result.Value?.Balance.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public async Task HandleAsync_AccountNotFound_ReturnsFailure()
    {
        var result = await _sut.HandleAsync(new GetAccountBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == "Account not found.");
    }
}
