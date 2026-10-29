using Godot;
using System.Linq;

/// <summary>
/// Antorchas que el jugador construye desde el menú de construcción (B →
/// 2). A diferencia de las barricadas, se pueden poner en CUALQUIER suelo
/// (no solo en el campamento): la idea es iluminar los túneles que uno
/// abre, porque de noche la mina queda casi a oscuras.
///
/// Igual que CampDefenses, vive fuera del WorldStreamer (no se descarga) y
/// guarda las celdas en WorldState. BuildComponent lo encuentra por el
/// grupo "torch_placer".
/// </summary>
public partial class TorchPlacer : Node3D, IStructurePlacer
{
	public const string Group = "torch_placer";

	[Export]
	public PackedScene TorchScene;

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

		// La antorcha tiene su origen en la base: va al fondo de la celda
		// (= la superficie del piso o del bloque de abajo).
		Vector3 floor = grid.GridToWorld(cell) - new Vector3(0.0f, grid.CellSize * 0.5f, 0.0f);

		torch.StartLit = true;
		torch.Position = ToLocal(floor);

		AddChild(torch);
	}
}
