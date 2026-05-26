using BankingService.Domain.Enums;

namespace BankingService.Domain.Exceptions;

public class CurrencyMismatchException(Currency left, Currency right)
    : Exception($"Currency mismatch: cannot mix {left} and {right}.");