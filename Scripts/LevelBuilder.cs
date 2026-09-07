using Godot;

/// <summary>
/// Genera las Rooms del nivel a partir de <see cref="LevelLayout"/> en vez
/// de tenerlas como nodos individuales configurados a mano en la escena.
/// Agregar, mover o rebalancear una sala es editar LevelLayout, no abrir
/// main.tscn y tocar propiedades exportadas una por una.
///
/// BlockScene, FloorMesh, etc. de cada Room ya vienen resueltos desde
/// Room.tscn (son parte de la escena empaquetada); acá solo se setean los
/// datos que antes se configuraban por instancia en main.tscn.
/// </summary>
public partial class LevelBuilder : Node3D
{
	[Export]
	public PackedScene RoomScene;

	public override void _Ready()
	{
		if (RoomScene == null)
		{
			GD.PushError($"{Name}: falta asignar RoomScene; no se genera ninguna sala.");
			return;
		}

		foreach (RoomSpec spec in LevelLayout.Default)
		{
			SpawnRoom(spec);
		}
	}

	private void SpawnRoom(RoomSpec spec)
	{
		var room = RoomScene.Instantiate<Room>();

		room.Name = spec.Name;
		room.Width = spec.Width;
		room.Depth = spec.Depth;
		room.WallHeight = spec.WallHeight;
		room.WallMineral = spec.WallMineral;
		room.WallBlockHealth = spec.WallBlockHealth;

		// La posición se fija ANTES de AddChild, mismo criterio que
		// Room.SpawnBlock con sus propios bloques: al entrar al árbol la
		// Room corre su _Ready y ahí ya necesita estar en su lugar.
		room.Position = spec.Position;

		AddChild(room);
	}
}
