namespace protabula_com.Services;

/// <summary>
/// Simulates how colors appear under different lighting conditions throughout the day.
/// Uses color temperature (Kelvin) to calculate light source tints.
/// </summary>
public static class LightingSimulator
{
    /// <summary>
    /// Time-of-day lighting conditions with their color temperatures and intensities.
    /// </summary>
    public static readonly LightingCondition[] Conditions =
    [
        new("9", "morning", 4500, 0.95f, 9),      // Morning - cool daylight building
        new("12", "noon", 6000, 1.15f, 12),       // Noon - bright midday sun
        new("15", "afternoon", 5000, 0.98f, 15),  // Afternoon - slightly warm
        new("18", "evening", 3500, 0.85f, 18),    // Evening - golden hour
        new("21", "dusk", 2800, 0.65f, 21),       // Dusk - warm orange, dimming
        new("24", "night", 2700, 0.50f, 24)       // Night - indoor warm light
    ];

    /// <summary>
    /// Calculates how a color appears under a specific lighting condition.
    /// Uses a subtle blend to avoid overly dramatic color shifts.
    /// </summary>
    public static string SimulateLighting(string hexColor, int temperatureKelvin, float intensity)
    {
        var (r, g, b) = ParseHex(hexColor);
        var (lightR, lightG, lightB) = TemperatureToRgb(temperatureKelvin);

        // Normalize light color against D55 (~5500K daylight) reference
        var (refR, refG, refB) = TemperatureToRgb(5500);
        float normR = lightR / refR;
        float normG = lightG / refG;
        float normB = lightB / refB;

        // Blend factor controls how much the light affects the color (0.3 = subtle)
        const float blendFactor = 0.3f;
        float tintR = 1f + (normR - 1f) * blendFactor;
        float tintG = 1f + (normG - 1f) * blendFactor;
        float tintB = 1f + (normB - 1f) * blendFactor;

        // Apply lighting: tint and intensity
        float newR = r * tintR * intensity;
        float newG = g * tintG * intensity;
        float newB = b * tintB * intensity;

        // Clamp to valid range
        newR = Math.Clamp(newR, 0f, 255f);
        newG = Math.Clamp(newG, 0f, 255f);
        newB = Math.Clamp(newB, 0f, 255f);

        return $"#{(byte)newR:X2}{(byte)newG:X2}{(byte)newB:X2}";
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
    /// </summary>
    public static readonly DirectSunlightCondition[] DirectSunlightConditions =
    [
        new("Morning", "morning", 4000, 1.05f),      // Morning sun - warm yellow
        new("Midday", "midday", 5800, 1.20f),        // Midday sun - bright, slightly cool
        new("GoldenHour", "golden-hour", 2500, 0.95f) // Golden hour - strong orange/amber
    ];

    /// <summary>
    /// Simulates direct sunlight hitting a surface - stronger color shift than ambient.
    /// </summary>
    public static string SimulateDirectSunlight(string hexColor, int temperatureKelvin, float intensity)
    {
        var (r, g, b) = ParseHex(hexColor);
        var (lightR, lightG, lightB) = TemperatureToRgb(temperatureKelvin);

        // Normalize against D55 reference
        var (refR, refG, refB) = TemperatureToRgb(5500);
        float normR = lightR / refR;
        float normG = lightG / refG;
        float normB = lightB / refB;

        // Stronger blend factor for direct sunlight (0.6 = significant shift)
        const float blendFactor = 0.6f;
        float tintR = 1f + (normR - 1f) * blendFactor;
        float tintG = 1f + (normG - 1f) * blendFactor;
        float tintB = 1f + (normB - 1f) * blendFactor;

        // Apply lighting
        float newR = r * tintR * intensity;
        float newG = g * tintG * intensity;
        float newB = b * tintB * intensity;

        // Clamp
        newR = Math.Clamp(newR, 0f, 255f);
        newG = Math.Clamp(newG, 0f, 255f);
        newB = Math.Clamp(newB, 0f, 255f);

        return $"#{(byte)newR:X2}{(byte)newG:X2}{(byte)newB:X2}";
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
    /// Converts color temperature (Kelvin) to RGB values.
    /// Based on Tanner Helland's algorithm (attempt to approximate blackbody radiation).
    /// </summary>
    private static (float R, float G, float B) TemperatureToRgb(int kelvin)
    {
        // Clamp temperature to reasonable range
        kelvin = Math.Clamp(kelvin, 1000, 40000);

        float temp = kelvin / 100f;
        float r, g, b;

        // Red
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

        // Green
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

        // Blue
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

        return (r, g, b);
    }

    private static (float R, float G, float B) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 3)
        {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        var r = Convert.ToByte(hex[..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);

        return (r, g, b);
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
