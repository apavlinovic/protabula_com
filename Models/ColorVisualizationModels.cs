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

/// <summary>
/// Model for interactive color tile with specular highlights and undertone visualization.
/// </summary>
/// <param name="Hex">Main color hex value</param>
/// <param name="UndertoneHex">Undertone direction hex (optional, for undertone conic gradient)</param>
/// <param name="Strength">Undertone strength for intensity control</param>
/// <param name="Label">Optional label displayed below the tile</param>
/// <param name="OnClick">Optional JavaScript onclick handler (e.g., "openColorPreview()")</param>
/// <param name="ClickHint">Optional hint text shown on the tile (e.g., "Click to preview")</param>
/// <param name="NeedsDarkText">If true, use dark text for the hint</param>
public record ColorTileModel(
    string Hex,
    string? UndertoneHex = null,
    UndertoneStrength Strength = UndertoneStrength.None,
    string? Label = null,
    string? OnClick = null,
    string? ClickHint = null,
    bool NeedsDarkText = false
);
