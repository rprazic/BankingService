using System.ComponentModel;

namespace BankingService.Domain.Enums;

[Description("Supported currency, serialized as ISO 4217 numeric code. Allowed values: 978 = EUR (Euro), 840 = USD (US Dollar), 941 = RSD (Serbian Dinar).")]
public enum Currency
{
    EUR = 978,
    USD = 840,
    RSD = 941
}