namespace e_commerce_web_customer.Infrastructure.Integrations.SePay;

internal static class SePayVndAmount
{
    public static decimal Normalize(decimal amount) =>
        decimal.Round(amount, 0, MidpointRounding.AwayFromZero);

    public static long ToWholeUnits(decimal amount) =>
        decimal.ToInt64(Normalize(amount));
}
