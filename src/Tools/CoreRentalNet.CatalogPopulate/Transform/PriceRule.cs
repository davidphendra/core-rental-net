namespace CoreRentalNet.CatalogPopulate.Transform;

/// <summary>The one price rule the catalogue was built with.</summary>
/// <remarks>
/// The source price is USD per item; the catalogue is IDR per month. The recorded rule converts at a fixed
/// rate and spreads the price over twelve months, rounding to the nearest thousand IDR. Verified against the
/// committed catalogue: 205 of 205 rows agree with it.
/// </remarks>
internal static class PriceRule
{
    /// <summary>The fixed rate the catalogue was converted at.</summary>
    public const decimal IdrPerUsd = 16_000m;

    /// <summary>The number of months a source price is spread over.</summary>
    public const decimal Months = 12m;

    /// <summary>The amount one product costs per month, in IDR, rounded to the nearest thousand.</summary>
    public static int MonthlyIdr(double priceUsd)
    {
        var monthly = (decimal)priceUsd * IdrPerUsd / Months;
        var thousands = Math.Round(monthly / 1_000m, 0, MidpointRounding.AwayFromZero);

        return (int)(thousands * 1_000m);
    }
}
