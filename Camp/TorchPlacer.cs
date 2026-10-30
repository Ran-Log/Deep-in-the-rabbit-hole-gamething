using Godot;
using System.Linq;

/// <summary>
/// Antorchas que construye el jugador (B, luego 2). Van en cualquier suelo, para
/// iluminar los túneles. Igual que CampDefenses: no se descargan y se guardan en WorldState.
/// </summary>
public partial class TorchPlacer : Node3D, IStructurePlacer
{
	public const string Group = "torch_placer";

	[ExportCategory("TorchPlacer")]
	[ExportGroup("Referencias")]
	[Export] public PackedScene TorchScene;

	public override void _EnterTree()
	{
		AddToGroup(Group);
	}

	public override void _Ready()
	{
		if (TorchScene == null)
		{
			GD.PushError($"{Name}: falta asignar TorchScene; no se pueden poner antorchas.");
			return;
		}

		if (WorldState.Instance == null)
		{
			return;
		}

		foreach (Vector3I cell in WorldState.Instance.GetPlacedTorches().ToList())
		{
			SpawnTorch(cell);
		}
	}

	public bool CanPlace(Vector3I cell, bool onTopOfSurface, out string reason)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid == null || TorchScene == null)
		{
			reason = "No se puede construir";
			return false;
		}

		if (!onTopOfSurface)
		{
			reason = "Apunta al suelo para poner la antorcha";
			return false;
		}

		if (grid.IsOccupied(cell) || grid.HasBodyInCell(GetWorld3D(), cell))
		{
			reason = "Hay algo en el camino";
			return false;
		}

		if (WorldState.Instance == null || WorldState.Instance.GetItemCount("Coal") < UpgradeTable.TorchCoalCost)
		{
			reason = $"Necesitas {UpgradeTable.TorchCoalCost} Carbón";
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

		WorldState.Instance.RemoveItem("Coal", UpgradeTable.TorchCoalCost);
		WorldState.Instance.AddTorch(cell);
		SpawnTorch(cell);

		return true;
	}

	private void SpawnTorch(Vector3I cell)
	{
		var torch = TorchScene.Instantiate<Torch>();
		BlockGrid grid = BlockGrid.Instance;

		// El origen de la antorcha está en su base: va al fondo de la celda.
		Vector3 floor = grid.GridToWorld(cell) - new Vector3(0.0f, grid.CellSize * 0.5f, 0.0f);

		torch.StartLit = true;
		torch.Position = ToLocal(floor);

		AddChild(torch);
	}
}
