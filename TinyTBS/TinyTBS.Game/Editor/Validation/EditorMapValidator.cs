using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Editor.Validation;

/// <summary>Map-only checks for the editor (does not block Save).</summary>
public static class EditorMapValidator
{
    public static EditorMapValidationResult Validate(EditableMapDocument document, MatchContentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalog);

        var issues = new List<EditorValidationIssue>();

        foreach (var grave in document.Gravestones)
        {
            if (document.Buildings.Any(building => building.X == grave.X && building.Y == grave.Y))
            {
                issues.Add(new EditorValidationIssue(
                    EditorValidationSeverity.Error,
                    $"Gravestone on building at ({grave.X},{grave.Y})."));
            }
        }

        foreach (var building in document.Buildings)
        {
            if (!catalog.TryGetBuilding(building.Type, out _))
            {
                issues.Add(new EditorValidationIssue(
                    EditorValidationSeverity.Warning,
                    $"Unknown building type '{building.Type.Full}' at ({building.X},{building.Y})."));
            }
        }

        foreach (var unit in document.Units)
        {
            if (!catalog.TryGetUnit(unit.Type, out _))
            {
                issues.Add(new EditorValidationIssue(
                    EditorValidationSeverity.Warning,
                    $"Unknown unit type '{unit.Type.Full}' at ({unit.X},{unit.Y})."));
            }
        }

        var unitCells = new HashSet<(int X, int Y)>();
        foreach (var unit in document.Units)
        {
            if (!unitCells.Add((unit.X, unit.Y)))
            {
                issues.Add(new EditorValidationIssue(
                    EditorValidationSeverity.Error,
                    $"Multiple units on cell ({unit.X},{unit.Y})."));
            }
        }

        var buildingCells = new HashSet<(int X, int Y)>();
        foreach (var building in document.Buildings)
        {
            if (!buildingCells.Add((building.X, building.Y)))
            {
                issues.Add(new EditorValidationIssue(
                    EditorValidationSeverity.Error,
                    $"Multiple buildings on cell ({building.X},{building.Y})."));
            }
        }

        return new EditorMapValidationResult(issues);
    }
}
