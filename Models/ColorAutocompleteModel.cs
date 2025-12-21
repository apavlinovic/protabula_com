using protabula_com.Models;

namespace protabula_com.Models;

/// <summary>
/// Model for the reusable color autocomplete partial view.
/// </summary>
public record ColorAutocompleteModel(
    string ContainerId,
    string Placeholder,
    IReadOnlyList<RalColor> Colors,
    string? SelectedNumber = null
);
