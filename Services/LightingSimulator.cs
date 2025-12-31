using protabula_com.Helpers;

namespace protabula_com.Services;

/// <summary>
/// Simulates how colors appear under different lighting conditions throughout the day.
/// Uses color temperature (Kelvin) to calculate light source tints.
/// </summary>
public static class LightingSimulator
{
    /// <summary>
    /// Time-of-day lighting conditions with their color temperatures and intensities.
    /// Based on CIE daylight illuminants and typical indoor lighting.
    /// </summary>
    public static readonly LightingCondition[] Conditions =
    [
        new("9", "morning", 5000, 0.92f, 9),      // Morning - D50 horizon daylight
        new("12", "noon", 6500, 1.12f, 12),       // Noon - D65 standard daylight
        new("15", "afternoon", 5500, 0.98f, 15),  // Afternoon - D55 mid-afternoon
        new("18", "evening", 3200, 0.82f, 18),    // Evening - golden hour
        new("21", "dusk", 2700, 0.60f, 21),       // Dusk - warm tungsten-like
        new("24", "night", 2700, 0.45f, 24)       // Night - indoor incandescent
    ];

    /// <summary>
    /// Calculates how a color appears under a specific lighting condition.
    /// Uses linear RGB space for physically accurate light simulation.
    /// </summary>
    public static string SimulateLighting(string hexColor, int temperatureKelvin, float intensity)
    {
        var rgb = ColorMath.ParseHex(hexColor);

        // Convert to linear RGB for physically accurate calculations
        var (linR, linG, linB) = ColorMath.ToLinearRgb(rgb.R, rgb.G, rgb.B);

        // Get light color and reference in linear space
        var (lightR, lightG, lightB) = TemperatureToLinearRgb(temperatureKelvin);
        var (refR, refG, refB) = TemperatureToLinearRgb(6500); // D65 standard daylight

        // Calculate chromatic adaptation ratios
        float ratioR = lightR / Math.Max(refR, 0.001f);
        float ratioG = lightG / Math.Max(refG, 0.001f);
        float ratioB = lightB / Math.Max(refB, 0.001f);

        // Blend factor controls adaptation strength (0.3 = subtle ambient lighting)
        const float blendFactor = 0.3f;
        float adaptR = 1f + (ratioR - 1f) * blendFactor;
        float adaptG = 1f + (ratioG - 1f) * blendFactor;
        float adaptB = 1f + (ratioB - 1f) * blendFactor;

        // Apply chromatic adaptation and intensity in linear space
        float newR = linR * adaptR * intensity;
        float newG = linG * adaptG * intensity;
        float newB = linB * adaptB * intensity;

        // Convert back to sRGB
        var result = ColorMath.FromLinearRgb(newR, newG, newB);
        return $"#{result.R:X2}{result.G:X2}{result.B:X2}";
    }

    /// <summary>
    /// Generates all lighting variations for a given color.
    /// </summary>
    public static IReadOnlyList<LightingVariation> GenerateVariations(string hexColor)
    {
        return Conditions
            .Select(c => new LightingVariation(
                c.Key,
                c.CssClass,
                c.TemperatureKelvin,
                c.Hour,
                SimulateLighting(hexColor, c.TemperatureKelvin, c.Intensity)))
            .ToList();
    }

    /// <summary>
    /// Direct sunlight conditions - stronger color temperature effect.
    /// Based on measured daylight color temperatures at different sun angles.
    /// </summary>
    public static readonly DirectSunlightCondition[] DirectSunlightConditions =
    [
        new("Morning", "morning", 4000, 1.05f),       // Morning sun - warm yellow (low angle)
        new("Midday", "midday", 5500, 1.18f),         // Midday sun - neutral white (D55)
        new("GoldenHour", "golden-hour", 2800, 0.92f) // Golden hour - warm amber
    ];

    /// <summary>
    /// Simulates direct sunlight hitting a surface - stronger color shift than ambient.
    /// Uses linear RGB space for physically accurate light simulation.
    /// </summary>
    public static string SimulateDirectSunlight(string hexColor, int temperatureKelvin, float intensity)
    {
        var rgb = ColorMath.ParseHex(hexColor);

        // Convert to linear RGB for physically accurate calculations
        var (linR, linG, linB) = ColorMath.ToLinearRgb(rgb.R, rgb.G, rgb.B);

        // Get light color and reference in linear space
        var (lightR, lightG, lightB) = TemperatureToLinearRgb(temperatureKelvin);
        var (refR, refG, refB) = TemperatureToLinearRgb(6500); // D65 standard daylight

        // Calculate chromatic adaptation ratios
        float ratioR = lightR / Math.Max(refR, 0.001f);
        float ratioG = lightG / Math.Max(refG, 0.001f);
        float ratioB = lightB / Math.Max(refB, 0.001f);

        // Stronger blend factor for direct sunlight (0.55 = significant shift)
        const float blendFactor = 0.55f;
        float adaptR = 1f + (ratioR - 1f) * blendFactor;
        float adaptG = 1f + (ratioG - 1f) * blendFactor;
        float adaptB = 1f + (ratioB - 1f) * blendFactor;

        // Apply chromatic adaptation and intensity in linear space
        float newR = linR * adaptR * intensity;
        float newG = linG * adaptG * intensity;
        float newB = linB * adaptB * intensity;

        // Convert back to sRGB
        var result = ColorMath.FromLinearRgb(newR, newG, newB);
        return $"#{result.R:X2}{result.G:X2}{result.B:X2}";
    }

    /// <summary>
    /// Generates direct sunlight variations for a given color.
    /// </summary>
    public static IReadOnlyList<DirectSunlightVariation> GenerateDirectSunlightVariations(string hexColor)
    {
        return DirectSunlightConditions
            .Select(c => new DirectSunlightVariation(
                c.Key,
                c.CssClass,
                c.TemperatureKelvin,
                SimulateDirectSunlight(hexColor, c.TemperatureKelvin, c.Intensity)))
            .ToList();
    }

    /// <summary>
    /// Converts color temperature (Kelvin) to linear RGB values (0-1 range).
    /// Based on Tanner Helland's algorithm, converted to linear space for accurate blending.
    /// </summary>
    private static (float R, float G, float B) TemperatureToLinearRgb(int kelvin)
    {
        // Clamp temperature to reasonable range
        kelvin = Math.Clamp(kelvin, 1000, 40000);

        float temp = kelvin / 100f;
        float r, g, b;

        // Red (in sRGB 0-255)
        if (temp <= 66)
        {
            r = 255;
        }
        else
        {
            r = temp - 60;
            r = 329.698727446f * MathF.Pow(r, -0.1332047592f);
            r = Math.Clamp(r, 0, 255);
        }

        // Green (in sRGB 0-255)
        if (temp <= 66)
        {
            g = temp;
            g = 99.4708025861f * MathF.Log(g) - 161.1195681661f;
            g = Math.Clamp(g, 0, 255);
        }
        else
        {
            g = temp - 60;
            g = 288.1221695283f * MathF.Pow(g, -0.0755148492f);
            g = Math.Clamp(g, 0, 255);
        }

        // Blue (in sRGB 0-255)
        if (temp >= 66)
        {
            b = 255;
        }
        else if (temp <= 19)
        {
            b = 0;
        }
        else
        {
            b = temp - 10;
            b = 138.5177312231f * MathF.Log(b) - 305.0447927307f;
            b = Math.Clamp(b, 0, 255);
        }

        // Convert from sRGB to linear RGB for accurate blending
        return ColorMath.ToLinearRgb((byte)r, (byte)g, (byte)b);
    }
}

/// <summary>
/// Represents a lighting condition with its parameters.
/// </summary>
public sealed record LightingCondition(
    string Key,
    string CssClass,
    int TemperatureKelvin,
    float Intensity,
    int Hour);

/// <summary>
/// Represents a color under a specific lighting condition.
/// </summary>
public sealed record LightingVariation(
    string Key,
    string CssClass,
    int TemperatureKelvin,
    int Hour,
    string Hex);

/// <summary>
/// Represents a direct sunlight condition.
/// </summary>
public sealed record DirectSunlightCondition(
    string Key,
    string CssClass,
    int TemperatureKelvin,
    float Intensity);

/// <summary>
/// Represents a color under direct sunlight.
/// </summary>
public sealed record DirectSunlightVariation(
    string Key,
    string CssClass,
    int TemperatureKelvin,
    string Hex);
