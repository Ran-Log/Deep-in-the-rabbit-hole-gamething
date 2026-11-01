using Godot;
using System.Linq;

/// <summary>
/// Trampas que construye el jugador (B, luego 3). Solo en el suelo del campamento: la zona
/// la decide CampDefenses. Igual que las barricadas: no se descargan y se guardan en WorldState.
/// </summary>
public partial class TrapPlacer : Node3D, IStructurePlacer
{
	public const string Group = "trap_placer";

	[ExportCategory("TrapPlacer")]
	[ExportGroup("Referencias")]
	[Export] public PackedScene TrapScene;
	// De acá sale la zona donde se puede construir.
	[Export] public CampDefenses Camp;

	public override void _EnterTree()
	{
		AddToGroup(Group);
	}

	public override void _Ready()
	{
		if (TrapScene == null || Camp == null)
		{
			GD.PushError($"{Name}: falta asignar TrapScene o Camp; no se pueden poner trampas.");
			return;
		}

		if (WorldState.Instance == null)
		{
			return;
		}

		foreach (var (cell, usesLeft) in WorldState.Instance.GetPlacedTraps().ToList())
		{
			SpawnTrap(cell, usesLeft);
		}
	}

	public bool CanPlace(Vector3I cell, bool onTopOfSurface, out string reason)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null || TrapScene == null || Camp == null || WorldState.Instance == null)
		{
			reason = "No se puede construir";
			return false;
		}

		if (!onTopOfSurface || !Camp.IsInsideCamp(cell) || cell.Y != Camp.MinCell.Y)
		{
			reason = "La trampa va en el suelo del campamento";
			return false;
		}

		if (grid.IsOccupied(cell) || grid.HasBodyInCell(GetWorld3D(), cell) || WorldState.Instance.HasTrap(cell))
		{
			reason = "Hay algo en el camino";
			return false;
		}

		if (!WorldState.Instance.HasItems(UpgradeTable.TrapCost))
		{
			reason = $"Necesitas {UpgradeTable.FormatCost(UpgradeTable.TrapCost)}";
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

		WorldState.Instance.TrySpend(UpgradeTable.TrapCost);
		SpikeTrap trap = SpawnTrap(cell, 0);
		WorldState.Instance.SetTrap(cell, trap.UsesLeft);

		return true;
	}

	private SpikeTrap SpawnTrap(Vector3I cell, int usesLeft)
	{
		var trap = TrapScene.Instantiate<SpikeTrap>();
		BlockGrid grid = BlockGrid.Instance;

		// El origen de la trampa está en su base: va al fondo de la celda.
		Vector3 floor = grid.GridToWorld(cell) - new Vector3(0.0f, grid.CellSize * 0.5f, 0.0f);

		trap.Cell = cell;
		trap.UsesLeft = usesLeft;
		trap.Position = ToLocal(floor);

		AddChild(trap);
		return trap;
	}
}
