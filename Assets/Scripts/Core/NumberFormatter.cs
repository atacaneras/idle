using System;
using System.Globalization;

// Tum sayi gosterimi buradan gecer. Ileride BigDouble'a gecerken sadece burasi ve GameManager degisir.
public static class NumberFormatter
{
    static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

    public static string Format(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";
        if (value < 1000) return value < 10
            ? value.ToString("0.##", CultureInfo.InvariantCulture)
            : value.ToString("0", CultureInfo.InvariantCulture);

        int tier = (int)Math.Floor(Math.Log10(value) / 3);
        if (tier >= Suffixes.Length) return value.ToString("0.00e0", CultureInfo.InvariantCulture);
        double scaled = value / Math.Pow(1000, tier);
        return scaled.ToString("0.##", CultureInfo.InvariantCulture) + Suffixes[tier];
    }
}
