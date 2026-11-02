using Godot;
using System.Linq;

/// <summary>
/// Barricadas del campamento (GDD 2.1.8). Son bloques normales que los zombies
/// rompen a golpes. Viven acá y no en la sala, para que no se descarguen al alejarse.
/// Se guardan en WorldState. BuildComponent encuentra este nodo por su grupo.
/// </summary>
public partial class CampDefenses : Node3D, IStructurePlacer
{
	public const string Group = "camp_defenses";

	private static readonly string StoneId = ItemCatalog.IdOf(MineralType.Stone);

	[ExportCategory("CampDefenses")]
	[ExportGroup("Referencias")]
	[Export] public PackedScene BlockScene;

	[ExportGroup("Barricada")]
	// Solo si faltan los datos del campamento; si no, sale de CampDefinition.BarricadeHealth.
	[Export] public int DefenseHealth = 12;

	[ExportGroup("Zona del campamento")]
	// Celdas (de BlockGrid) donde se puede construir. Por defecto, el interior de la sala (0,0).
	[Export] public Vector3I MinCell = new Vector3I(-5, 0, -5);
	[Export] public Vector3I MaxCell = new Vector3I(4, 1, 4);

	public override void _EnterTree()
	{
		AddToGroup(Group);
	}

	public override void _Ready()
	{
		if (BlockScene == null)
		{
			GD.PushError($"{Name}: falta asignar BlockScene; no se pueden construir defensas.");
			return;
		}

		if (WorldState.Instance == null)
		{
			return;
		}

		// Se recorre una copia, por si la lista cambia mientras se itera.
		foreach (Vector3I cell in WorldState.Instance.GetPlacedDefenses().ToList())
		{
			SpawnDefense(cell);
		}
	}

	// Un campamento mejorado hace barricadas más fuertes (las ya puestas no cambian).
	public int BarricadeHealth => GameData.Camp(WorldState.Instance?.CampLevel ?? 1)?.BarricadeHealth ?? DefenseHealth;

	public bool IsInsideCamp(Vector3I cell)
	{
		return cell.X >= MinCell.X && cell.X <= MaxCell.X
			&& cell.Y >= MinCell.Y && cell.Y <= MaxCell.Y
			&& cell.Z >= MinCell.Z && cell.Z <= MaxCell.Z;
	}

	// reason explica por qué no (para el HUD). Las barricadas se apilan, no necesitan suelo.
	public bool CanPlace(Vector3I cell, bool onTopOfSurface, out string reason)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null || BlockScene == null)
		{
			reason = "No se puede construir";
			return false;
		}

		if (!IsInsideCamp(cell))
		{
			reason = "Solo se construye dentro del campamento";
			return false;
		}

		if (grid.IsOccupied(cell) || grid.HasBodyInCell(GetWorld3D(), cell) || WorldState.Instance?.HasTrap(cell) == true)
		{
			reason = "Hay algo en el camino";
			return false;
		}

		if (WorldState.Instance == null || WorldState.Instance.GetItemCount(StoneId) < UpgradeTable.DefenseStoneCost)
		{
			reason = $"Necesitas {UpgradeTable.DefenseStoneCost} Piedra";
			return false;
		}

		reason = "";
		return true;
	}

	public bool TryPlace(Vector3I cell, bool onTopOfSurface)
	{
		if (!CanPlace(cell, onTopOfSurface, out string reason))
		{
			GameEvents.PostMessage(reason);
			return false;
		}

		WorldState.Instance.RemoveItem(StoneId, UpgradeTable.DefenseStoneCost);
		WorldState.Instance.AddDefense(cell);
		SpawnDefense(cell);

		return true;
	}

	private void SpawnDefense(Vector3I cell)
	{
		var block = BlockScene.Instantiate<DestructibleBlock>();

		block.Mineral = MineralType.Stone;
		block.MaxHealth = BarricadeHealth;
		block.RequiredTool = ToolTier.Basic;
		block.DisplayName = "Barricada";

		// Antes de AddChild: en su _Ready el bloque se registra en la grilla.
		block.Position = ToLocal(BlockGrid.Instance.GridToWorld(cell));
		block.Destroyed += (_) => WorldState.Instance?.RemoveDefense(cell);

		AddChild(block);
	}
}
