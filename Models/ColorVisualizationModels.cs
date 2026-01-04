namespace protabula_com.Models;

/// <summary>
/// Model for saturation bar visualization.
/// </summary>
public record SaturationBarModel(int Hue, int Saturation);

/// <summary>
/// Model for HSL lightness bar visualization.
/// </summary>
public record LightnessBarModel(int Hue, int Lightness);

/// <summary>
/// Model for LRV (Light Reflectance Value) bar visualization.
/// </summary>
public record LrvBarModel(double Lrv);

/// <summary>
/// Model for color temperature badge visualization.
/// </summary>
public record TemperatureBadgeModel(int Kelvin, string Classification);

/// <summary>
/// Model for CIE Lab component visualization (L*, a*, or b*).
/// </summary>
/// <param name="Component">The component type: "L" (0-100), "a" (-128 to +128), or "b" (-128 to +128)</param>
/// <param name="Value">The component value</param>
public record CieLabComponentModel(string Component, double Value);
