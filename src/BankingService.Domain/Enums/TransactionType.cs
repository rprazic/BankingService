using System.ComponentModel;

namespace BankingService.Domain.Enums;

[Description("Direction of money flow, serialized as integer. Allowed values: 0 = Credit (money arriving: deposit or incoming transfer), 1 = Debit (money leaving: withdrawal or outgoing transfer).")]
public enum TransactionType
{
    Credit,
    Debit
}