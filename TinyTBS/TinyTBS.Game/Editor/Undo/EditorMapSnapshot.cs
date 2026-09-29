using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Editor.Undo;

/// <summary>Immutable copy of map layers for Undo/Redo.</summary>
public sealed class EditorMapSnapshot
{
    public EditorMapSnapshot(
        string[,] surface,
        IReadOnlyList<MapBuildingPlacement> buildings,
        IReadOnlyList<MapUnitPlacement> units,
        IReadOnlyList<MapGravestonePlacement> gravestones,
        bool isDirty)
    {
        Surface = (string[,])surface.Clone();
        Buildings = buildings.Select(CloneBuilding).ToList();
        Units = units.Select(CloneUnit).ToList();
        Gravestones = gravestones.Select(CloneGrave).ToList();
        IsDirty = isDirty;
    }

    public string[,] Surface { get; }

    public IReadOnlyList<MapBuildingPlacement> Buildings { get; }

    public IReadOnlyList<MapUnitPlacement> Units { get; }

    public IReadOnlyList<MapGravestonePlacement> Gravestones { get; }

    public bool IsDirty { get; }

    private static MapBuildingPlacement CloneBuilding(MapBuildingPlacement building) =>
        new()
        {
            Type = building.Type,
            X = building.X,
            Y = building.Y,
            Slot = building.Slot,
            State = building.State,
        };

    private static MapUnitPlacement CloneUnit(MapUnitPlacement unit) =>
        new()
        {
            Type = unit.Type,
            X = unit.X,
            Y = unit.Y,
            Slot = unit.Slot,
            Hp = unit.Hp,
            Xp = unit.Xp,
        };

    private static MapGravestonePlacement CloneGrave(MapGravestonePlacement grave) =>
        new() { X = grave.X, Y = grave.Y };
}
