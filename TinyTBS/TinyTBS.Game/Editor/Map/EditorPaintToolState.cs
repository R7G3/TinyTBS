using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Rules.Maps.Models;

namespace TinyTBS.Game.Editor.Map;

/// <summary>Selected content (left) + mode Place/Erase/Label + owner slot (right).</summary>
public sealed class EditorPaintToolState
{
    public const int PlayerSlotCount = 4;

    public EditorPaintMode Mode { get; set; } = EditorPaintMode.Place;

    public EditorPaintContentKind ContentKind { get; private set; } = EditorPaintContentKind.Terrain;

    public TerrainKind Terrain { get; private set; } = TerrainKind.Grass;

    public ContentId BuildingTypeId { get; private set; } = ContentId.Parse(VanillaContentIds.CastleId);

    public string? BuildingState { get; private set; } = MapSurfaceIds.IntactBuildingState;

    public ContentId UnitTypeId { get; private set; } = ContentId.Parse(VanillaContentIds.SwordsmanId);

    /// <summary>Null = Neutral (buildings). Units fall back to slot 0 when placing.</summary>
    public int? OwnerSlot { get; private set; }

    public string OwnerLabel => PlayerDisplayNames.EditorOwner(OwnerSlot);

    public string StatusLabel
    {
        get
        {
            if (Mode == EditorPaintMode.Erase)
                return "Mode: Erase (terrain→grass, clear objects)";

            if (Mode == EditorPaintMode.Label)
                return "Mode: Label → " + OwnerLabel + " (buildings + units)";

            return ContentKind switch
            {
                EditorPaintContentKind.Building =>
                    "Place building " + BuildingTypeId.Full
                    + (string.IsNullOrWhiteSpace(BuildingState) ? string.Empty : " [" + BuildingState + "]")
                    + " (" + OwnerLabel + ")",
                EditorPaintContentKind.Unit =>
                    "Place unit " + UnitTypeId.Full + " (" + UnitPlaceSlotLabel + ")",
                EditorPaintContentKind.Gravestone => "Place gravestone",
                _ => "Place terrain " + EditableMapDocument.FormatTerrain(Terrain),
            };
        }
    }

    private string UnitPlaceSlotLabel => PlayerDisplayNames.EditorUnitPlace(OwnerSlot);

    public void SelectOwner(int? slot)
    {
        if (slot is int playerSlot)
        {
            if (playerSlot < 0 || playerSlot >= PlayerSlotCount)
                throw new ArgumentOutOfRangeException(nameof(slot));
            OwnerSlot = playerSlot;
        }
        else
        {
            OwnerSlot = null;
        }

        // Owner buttons always stamp labels — never keep Place (that would drop another building/unit).
        Mode = EditorPaintMode.Label;
    }

    public void SelectTerrain(TerrainKind terrain)
    {
        ContentKind = EditorPaintContentKind.Terrain;
        Terrain = terrain;
        Mode = EditorPaintMode.Place;
    }

    public void SelectBuilding(ContentId typeId, string? state = MapSurfaceIds.IntactBuildingState)
    {
        ContentKind = EditorPaintContentKind.Building;
        BuildingTypeId = typeId;
        BuildingState = state;
        Mode = EditorPaintMode.Place;
    }

    public void SelectUnit(ContentId typeId)
    {
        ContentKind = EditorPaintContentKind.Unit;
        UnitTypeId = typeId;
        Mode = EditorPaintMode.Place;
    }

    public void SelectGravestone()
    {
        ContentKind = EditorPaintContentKind.Gravestone;
        Mode = EditorPaintMode.Place;
    }

    /// <summary>Returns true when the document changed.</summary>
    public bool Apply(EditableMapDocument document, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (Mode == EditorPaintMode.Erase)
            return document.EraseAt(x, y);

        if (Mode == EditorPaintMode.Label)
            return document.SetOwnerAt(x, y, OwnerSlot);

        return ContentKind switch
        {
            EditorPaintContentKind.Building =>
                document.PlaceBuilding(BuildingTypeId, x, y, slot: OwnerSlot, state: BuildingState),
            EditorPaintContentKind.Unit =>
                document.PlaceUnit(UnitTypeId, x, y, slot: OwnerSlot ?? 0),
            EditorPaintContentKind.Gravestone =>
                document.PlaceGravestone(x, y),
            _ => document.SetTerrain(x, y, Terrain),
        };
    }
}
