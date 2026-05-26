using System.Numerics;

namespace BankingService.Infrastructure.Services;

public class IbanGenerator : IIbanGenerator
{
    private const string CountryCode = "RS";
    // R=27, S=28
    private const string CountryCodeNumeric = "2728";

    public string Generate()
    {
        var bban = GenerateBban();
        var checkDigits = CalculateCheckDigits(bban);
        return $"{CountryCode}{checkDigits}{bban}";
    }

    private static string GenerateBban()
    {
        var random = Random.Shared;
        return string.Concat(Enumerable.Range(0, 18).Select(_ => random.Next(0, 10).ToString()));
    }

    private static string CalculateCheckDigits(string bban)
    {
        // MOD97: rearrange as BBAN + CountryCode + "00", replace letters with digits
        var numericString = bban + CountryCodeNumeric + "00";
        var remainder = BigInteger.Parse(numericString) % 97;
        var checkDigits = 98 - (int)remainder;
        return checkDigits.ToString("D2");
    }
}