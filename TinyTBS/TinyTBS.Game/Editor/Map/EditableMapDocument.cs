using TinyTBS.Game.Editor.Undo;
using TinyTBS.Game.Maps;
using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Match;

namespace TinyTBS.Game.Editor.Map;

/// <summary>Mutable map being edited in the Editor (in-memory).</summary>
public sealed class EditableMapDocument
{
    public EditableMapDocument(
        string id,
        string title,
        int width,
        int height,
        string[,] surface,
        List<MapBuildingPlacement> buildings,
        List<MapUnitPlacement> units,
        List<MapGravestonePlacement> gravestones)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException(nameof(width), "Map size must be at least 1×1.");
        if (surface.GetLength(0) != width || surface.GetLength(1) != height)
            throw new ArgumentException("Surface dimensions must match width/height.");

        Id = id.Trim();
        Title = title.Trim();
        Width = width;
        Height = height;
        Surface = surface;
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Units = units ?? throw new ArgumentNullException(nameof(units));
        Gravestones = gravestones ?? throw new ArgumentNullException(nameof(gravestones));
    }

    public string Id { get; set; }

    public string Title { get; set; }

    public int Width { get; }

    public int Height { get; }

    public string[,] Surface { get; private set; }

    public List<MapBuildingPlacement> Buildings { get; private set; }

    public List<MapUnitPlacement> Units { get; private set; }

    public List<MapGravestonePlacement> Gravestones { get; private set; }

    public bool IsDirty { get; set; }

    public static EditableMapDocument CreateFilled(string id, string title, int width, int height, string terrainType)
    {
        var surface = new string[width, height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                surface[x, y] = terrainType;
        }

        return new EditableMapDocument(id, title, width, height, surface, [], [], []);
    }

    public static EditableMapDocument FromDefinition(MapDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var surface = (string[,])definition.Surface.Clone();
        return new EditableMapDocument(
            definition.Id,
            definition.Title,
            definition.Width,
            definition.Height,
            surface,
            definition.Buildings.ToList(),
            definition.Units.ToList(),
            definition.Gravestones.ToList());
    }

    public EditorMapSnapshot CaptureSnapshot() =>
        new(Surface, Buildings, Units, Gravestones, IsDirty);

    public void RestoreSnapshot(EditorMapSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Surface.GetLength(0) != Width || snapshot.Surface.GetLength(1) != Height)
            throw new ArgumentException("Snapshot size does not match document.");

        Surface = (string[,])snapshot.Surface.Clone();
        Buildings = snapshot.Buildings.ToList();
        Units = snapshot.Units.ToList();
        Gravestones = snapshot.Gravestones.ToList();
        IsDirty = snapshot.IsDirty;
    }

    public bool SetTerrain(int x, int y, string terrainType)
    {
        EnsureInBounds(x, y);
        MapSurfaceIds.ParseTerrain(terrainType);
        if (string.Equals(Surface[x, y], terrainType, StringComparison.OrdinalIgnoreCase))
            return false;
        Surface[x, y] = terrainType;
        IsDirty = true;
        return true;
    }

    public bool SetTerrain(int x, int y, TerrainKind kind) =>
        SetTerrain(x, y, FormatTerrain(kind));

    public TerrainKind GetTerrain(int x, int y)
    {
        EnsureInBounds(x, y);
        return MapSurfaceIds.ParseTerrain(Surface[x, y]);
    }

    public bool PlaceBuilding(ContentId typeId, int x, int y, int? slot, string? state = "intact")
    {
        EnsureInBounds(x, y);
        RemoveGravestoneAt(x, y);
        Buildings.RemoveAll(building => building.X == x && building.Y == y);
        Buildings.Add(new MapBuildingPlacement
        {
            Type = typeId,
            X = x,
            Y = y,
            Slot = slot,
            State = state,
        });
        IsDirty = true;
        return true;
    }

    public bool PlaceUnit(ContentId typeId, int x, int y, int slot, int hitPoints = 100, int experience = 0)
    {
        EnsureInBounds(x, y);
        Units.RemoveAll(unit => unit.X == x && unit.Y == y);
        Units.Add(new MapUnitPlacement
        {
            Type = typeId,
            X = x,
            Y = y,
            Slot = slot,
            Hp = hitPoints,
            Xp = experience,
        });
        IsDirty = true;
        return true;
    }

    public bool PlaceGravestone(int x, int y)
    {
        EnsureInBounds(x, y);
        if (Buildings.Any(building => building.X == x && building.Y == y))
            return false;

        if (Gravestones.Any(grave => grave.X == x && grave.Y == y))
            return false;

        Gravestones.Add(new MapGravestonePlacement { X = x, Y = y });
        IsDirty = true;
        return true;
    }

    /// <summary>
    /// Stamps owner on building (any slot including null/Neutral) and unit (player slots only).
    /// </summary>
    public bool SetOwnerAt(int x, int y, int? slot)
    {
        EnsureInBounds(x, y);
        var changed = false;

        var buildingIndex = Buildings.FindIndex(building => building.X == x && building.Y == y);
        if (buildingIndex >= 0)
        {
            var building = Buildings[buildingIndex];
            if (building.Slot != slot)
            {
                Buildings[buildingIndex] = new MapBuildingPlacement
                {
                    Type = building.Type,
                    X = building.X,
                    Y = building.Y,
                    Slot = slot,
                    State = building.State,
                };
                changed = true;
            }
        }

        if (slot is int playerSlot)
        {
            var unitIndex = Units.FindIndex(unit => unit.X == x && unit.Y == y);
            if (unitIndex >= 0)
            {
                var unit = Units[unitIndex];
                if (unit.Slot != playerSlot)
                {
                    Units[unitIndex] = new MapUnitPlacement
                    {
                        Type = unit.Type,
                        X = unit.X,
                        Y = unit.Y,
                        Slot = playerSlot,
                        Hp = unit.Hp,
                        Xp = unit.Xp,
                    };
                    changed = true;
                }
            }
        }

        if (changed)
            IsDirty = true;

        return changed;
    }

    public bool EraseAt(int x, int y)
    {
        EnsureInBounds(x, y);
        var removed = Buildings.RemoveAll(building => building.X == x && building.Y == y);
        removed += Units.RemoveAll(unit => unit.X == x && unit.Y == y);
        removed += Gravestones.RemoveAll(grave => grave.X == x && grave.Y == y);
        if (!string.Equals(Surface[x, y], "grass", StringComparison.OrdinalIgnoreCase))
        {
            Surface[x, y] = "grass";
            removed++;
        }

        if (removed > 0)
        {
            IsDirty = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Skirmish slot count from placed owners: <c>max(slot)+1</c>, at least 2.
    /// Neutral-only maps stay at 2 (default hotseat).
    /// </summary>
    public static int InferPlayerSlotCount(EditableMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var maxSlot = -1;
        foreach (var building in document.Buildings)
        {
            if (building.Slot is int slot)
                maxSlot = Math.Max(maxSlot, slot);
        }

        foreach (var unit in document.Units)
            maxSlot = Math.Max(maxSlot, unit.Slot);

        if (maxSlot < 0)
            return 2;

        return Math.Max(2, maxSlot + 1);
    }

    public static string FormatTerrain(TerrainKind kind) => kind switch
    {
        TerrainKind.Water => "water",
        TerrainKind.Road => "road",
        TerrainKind.Mountain => "mountain",
        TerrainKind.Bridge => "bridge",
        TerrainKind.Forest => "forest",
        _ => "grass",
    };

    private void RemoveGravestoneAt(int x, int y) =>
        Gravestones.RemoveAll(grave => grave.X == x && grave.Y == y);

    private void EnsureInBounds(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            throw new ArgumentOutOfRangeException($"Cell ({x},{y}) outside {Width}×{Height}.");
    }
}
