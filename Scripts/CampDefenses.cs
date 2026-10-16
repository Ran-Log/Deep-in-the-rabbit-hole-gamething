using Godot;
using System.Linq;

/// <summary>
/// Barricadas que el jugador construye dentro del campamento (GDD 2.1.8).
/// Son DestructibleBlock normales — se registran en BlockGrid, los zombies
/// las rompen a golpes y el jugador las puede volver a minar — pero viven
/// acá, fuera del WorldStreamer, para que no se descarguen junto con la
/// sala del campamento al alejarse.
///
/// Las celdas construidas se guardan en WorldState, así que sobreviven a
/// reiniciar la escena (morir) y al guardado en disco.
///
/// BuildComponent (en el jugador) encuentra este nodo por el grupo
/// "camp_defenses": el jugador y el campamento son escenas distintas y no
/// se pueden referenciar por ruta en el editor.
/// </summary>
public partial class CampDefenses : Node3D, IStructurePlacer
{
	public const string Group = "camp_defenses";

	[Export]
	public PackedScene BlockScene;

	[Export]
	public int DefenseHealth = 12;

	/// <summary>Celda mínima (global, de BlockGrid) donde se puede construir. Por defecto, el interior de la sala (0,0).</summary>
	[Export]
	public Vector3I MinCell = new Vector3I(-5, 0, -5);

	[Export]
	public Vector3I MaxCell = new Vector3I(4, 1, 4);

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

		// Copia con ToList: SpawnDefense no modifica la lista, pero así el
		// recorrido no depende de eso.
		foreach (Vector3I cell in WorldState.Instance.GetPlacedDefenses().ToList())
		{
			SpawnDefense(cell);
		}
	}

	public bool IsInsideCamp(Vector3I cell)
	{
		return cell.X >= MinCell.X && cell.X <= MaxCell.X
			&& cell.Y >= MinCell.Y && cell.Y <= MaxCell.Y
			&& cell.Z >= MinCell.Z && cell.Z <= MaxCell.Z;
	}

	/// <summary>Si no se puede, <paramref name="reason"/> dice por qué (para el HUD). Las barricadas se apilan, así que no exige apoyarse arriba de algo.</summary>
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

		if (grid.IsOccupied(cell) || grid.HasBodyInCell(GetWorld3D(), cell))
		{
			reason = "Hay algo en el camino";
			return false;
		}

		if (WorldState.Instance == null || WorldState.Instance.GetItemCount("Stone") < UpgradeTable.DefenseStoneCost)
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

		WorldState.Instance.RemoveItem("Stone", UpgradeTable.DefenseStoneCost);
		WorldState.Instance.AddDefense(cell);
		SpawnDefense(cell);

		return true;
	}

	private void SpawnDefense(Vector3I cell)
	{
		var block = BlockScene.Instantiate<DestructibleBlock>();

		block.Mineral = MineralType.Stone;
		block.MaxHealth = DefenseHealth;
		block.RequiredTool = ToolTier.Basic;
		block.DisplayName = "Barricada";

		// Igual que en Room.SpawnBlock: la posición va ANTES de AddChild,
		// porque el bloque se alinea y registra en la grilla en su _Ready.
		block.Position = ToLocal(BlockGrid.Instance.GridToWorld(cell));
		block.Destroyed += (_) => WorldState.Instance?.RemoveDefense(cell);

		AddChild(block);
	}
}
