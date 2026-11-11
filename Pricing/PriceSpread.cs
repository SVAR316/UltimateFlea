namespace UltimateFlea.Pricing;

/// <summary>
/// Per-item offer band. Low/High are RATIOS of the reference price (low24h / ref, high24h / ref), so
/// the shape follows the simulation as it moves the reference price. AbsLow/AbsHigh are the live
/// feed's real 24h low/high in roubles; they are hard bounds no offer may leave (0 = no bound).
/// </summary>
public readonly record struct PriceSpread(double Low, double High, double AbsLow = 0, double AbsHigh = 0)
{
    public const double MinRatio = 0.5;
    public const double MaxRatio = 3.0;

    /// <summary>
    /// Builds a band from the feed's 24h low/high relative to the chosen reference price. Returns
    /// false when low/high are missing, zero, non-finite or inverted (low &gt; high), so the item
    /// falls back to the global behaviour. Each ratio is clamped to MinRatio..MaxRatio.
    /// </summary>
    public static bool TryCreate(double reference, double? low, double? high, out PriceSpread spread)
    {
        spread = default;
        if (!(reference > 0) || !double.IsFinite(reference) ||
            low is not > 0 || high is not > 0 ||
            !double.IsFinite(low.Value) || !double.IsFinite(high.Value) ||
            low.Value > high.Value)
        {
            return false;
        }

        spread = new PriceSpread(
            Math.Clamp(low.Value / reference, MinRatio, MaxRatio),
            Math.Clamp(high.Value / reference, MinRatio, MaxRatio),
            low.Value,
            high.Value);
        return true;
    }

    /// <summary>Clamps a rouble price into the absolute 24h [low, high]; unchanged when no bounds are set.</summary>
    public double ClampAbsolute(double price)
    {
        if (!(AbsLow > 0) || !(AbsHigh >= AbsLow))
        {
            return price;
        }

        return Math.Clamp(price, AbsLow, AbsHigh);
    }

    /// <summary>Validates the cached values: ratio low/high plus absolute low/high (roubles).</summary>
    public static bool TryFromCache(double low, double high, double absLow, double absHigh, out PriceSpread spread)
    {
        spread = default;
        if (!double.IsFinite(low) || !double.IsFinite(high) || low <= 0 || high < low ||
            low < MinRatio || high > MaxRatio ||
            !double.IsFinite(absLow) || !double.IsFinite(absHigh) || absLow <= 0 || absHigh < absLow)
        {
            return false;
        }

        spread = new PriceSpread(low, high, absLow, absHigh);
        return true;
    }

    /// <summary>Lower bound on the skew exponent: 1.0 is a flat, uniform band.</summary>
    public const double MinSkew = 1.0;

    /// <summary>
    /// Upper bound on the skew exponent. Without it, a band whose low sits just under the reference
    /// while the high runs far above it (the usual shape: most live offers near the floor, rare
    /// outliers at the top) needs an exponent in the tens, which collapses nearly every offer onto
    /// the floor and brings back the "every offer costs the same" problem.
    /// </summary>
    public const double MaxSkew = 6.0;

    /// <summary>
    /// Draws a multiplier over [Low, High] as Low + (High - Low) * u^k, where the exponent k leans
    /// the draws toward the floor. k is chosen so the mean lands on the reference price (1.0),
    /// because the feed's reference IS the 24h average of real offers; it is clamped to
    /// MinSkew..MaxSkew, so on very wide bands the mean stays above 1.0 rather than degenerating
    /// into a single price. <paramref name="u"/> is a uniform sample in [0, 1).
    /// </summary>
    public double Sample(double u)
    {
        var range = High - Low;
        if (range <= 0)
        {
            return Low;
        }

        // Mean of Low + range * u^k is Low + range / (k + 1); solving it for 1.0 gives this k.
        // Only meaningful when the band straddles the reference, which is the common case.
        var k = Low < 1.0 && High > 1.0
            ? Math.Clamp(range / (1.0 - Low) - 1.0, MinSkew, MaxSkew)
            : MinSkew;

        return Low + range * Math.Pow(u, k);
    }
}
