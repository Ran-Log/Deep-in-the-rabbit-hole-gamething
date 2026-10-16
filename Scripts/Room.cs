using Godot;

/// <summary>
/// Una habitación: un piso, y un perímetro de bloques generado automáticamente
/// alrededor según <see cref="Width"/> y <see cref="Depth"/>.
///
/// El origen de la Room coincide con el CENTRO de la primera capa de bloques
/// (celda y = 0), así que la superficie del piso queda media celda más abajo.
/// Conviene ubicar las Rooms en múltiplos de BlockGrid.CellSize para que las
/// celdas de habitaciones vecinas calcen entre sí.
///
/// Orden de generación: las Rooms generan sus paredes en <see cref="_Ready"/>,
/// en el mismo orden en que Godot recorre el árbol de la escena (o sea, el
/// orden de los nodos hijos bajo "Rooms" en main.tscn). Si dos Rooms llegaran
/// a pedir la MISMA celda de grilla, gana la que se registró primero — ver
/// <see cref="SpawnBlock"/>. Con el espaciado actual (Room separadas
/// Width/Depth * CellSize en el mundo) las paredes de Rooms vecinas quedan
/// una al lado de la otra, no superpuestas, así que este caso hoy no ocurre
/// en main.tscn; queda documentado para quien agregue Rooms más juntas o
/// cambie el espaciado.
/// </summary>
public partial class Room : Node3D
{
	[Export]
	public PackedScene BlockScene;

	/// <summary>Ancho total en celdas, incluyendo las dos paredes.</summary>
	[Export(PropertyHint.Range, "3,64,1")]
	public int Width = 10;

	/// <summary>Profundidad total en celdas, incluyendo las dos paredes.</summary>
	[Export(PropertyHint.Range, "3,64,1")]
	public int Depth = 10;

	/// <summary>Altura del muro perimetral, en celdas.</summary>
	[Export(PropertyHint.Range, "1,16,1")]
	public int WallHeight = 3;

	[Export]
	public MineralType WallMineral = MineralType.Stone;

	[Export]
	public int WallBlockHealth = 5;

	[Export]
	public StaticBody3D FloorBody;

	[Export]
	public MeshInstance3D FloorMesh;

	[Export]
	public CollisionShape3D FloorCollision;

	[Export]
	public Node3D BlockContainer;

	/// <summary>
	/// Si está activo, imprime en consola cada vez que esta Room se topa con
	/// una celda ya ocupada por otra Room (ver <see cref="SpawnBlock"/>).
	/// Apagado por defecto para no ensuciar la consola en juego normal.
	/// </summary>
	[Export]
	public bool LogSharedWalls = false;

	/// <summary>
	/// Coordenada de esta sala en la grilla infinita de salas (no en celdas
	/// de bloque). La setea WorldStreamer al generarla; queda en (0,0) para
	/// una Room puesta a mano en una escena (comportamiento de antes: sin
	/// mundo infinito, todo pasa en la sala "(0,0)").
	/// </summary>
	[Export]
	public Vector2I RoomGridCoord = Vector2I.Zero;

	/// <summary>Si es true, esta Room genera una GasZone en su centro al entrar al árbol (ver RoomGenerator).</summary>
	[Export]
	public bool HasGas = false;

	[Export]
	public PackedScene GasZoneScene;

	/// <summary>Contenido del cofre de la sala; None = sin cofre. Lo setea WorldStreamer desde el RoomSpec.</summary>
	public ChestLoot Loot { get; set; } = ChestLoot.None;

	public string NoteText { get; set; } = "";

	public bool HasExit { get; set; }

	[Export]
	public PackedScene ChestScene;

	[Export]
	public PackedScene ExitScene;

	/// <summary>
	/// Centro aproximado del interior caminable, en coordenadas locales. Con
	/// tamaño par el centro de la grilla queda corrido media celda (ver
	/// RoomMath.CenterOffset), así que el interior no está centrado en el origen.
	/// </summary>
	private Vector3 InteriorCenter => new Vector3(-1.0f, FloorTopY, -1.0f);

	/// <summary>Centro del interior en coordenadas globales (ej. para hacer aparecer enemigos adentro).</summary>
	public Vector3 InteriorCenterGlobal => ToGlobal(InteriorCenter);

	/// <summary>Altura local de la superficie del piso (media celda bajo el origen).</summary>
	private float FloorTopY => -(BlockGrid.Instance?.CellSize ?? 2.0f) * 0.5f;

	private Vector3I originCell;
	private int spawnedBlocks;

	/// <summary>Cuántos bloques generó esta habitación. Útil para el paso de medición.</summary>
	public int SpawnedBlocks => spawnedBlocks;

	public override void _Ready()
	{
		if (BlockGrid.Instance == null)
		{
			GD.PushError($"{Name}: no hay BlockGrid autoload; la habitación no se puede generar.");
			return;
		}

		SnapOriginToGrid();
		ResizeFloor();
		GenerateWalls();
		SpawnGasZoneIfNeeded();
		SpawnChestIfNeeded();
		SpawnExitIfNeeded();
	}

	private void SnapOriginToGrid()
	{
		BlockGrid grid = BlockGrid.Instance;

		GlobalPosition = grid.GridToWorld(grid.WorldToGrid(GlobalPosition));
	}

	/// <summary>
	/// Estira el piso para que cubra exactamente la huella de la habitación.
	/// Ocupa la capa de celdas y = -1, o sea justo debajo del primer nivel de muro.
	/// </summary>
	private void ResizeFloor()
	{
		float cell = BlockGrid.Instance.CellSize;
		Vector3 size = new Vector3(Width * cell, cell, Depth * cell);
		Vector3 center = new Vector3(0.0f, -cell, 0.0f);

		if (FloorMesh?.Mesh is BoxMesh boxMesh)
		{
			boxMesh.Size = size;
			FloorMesh.Position = center;

			// Textura de piso generada por código (ver MineralTextureFactory):
			// reemplaza cualquier material puesto a mano en Room.tscn.
			FloorMesh.MaterialOverride = MineralTextureFactory.GetFloorMaterial();
		}

		if (FloorCollision?.Shape is BoxShape3D boxShape)
		{
			boxShape.Size = size;
			FloorCollision.Position = center;
		}
	}

	private void GenerateWalls()
	{
		if (BlockScene == null)
		{
			GD.PushWarning($"{Name}: falta asignar BlockScene; no se generan paredes.");
			return;
		}

		// La habitación tiene que estar alineada a los ejes: las celdas se
		// calculan como offsets locales sin rotar.
		originCell = BlockGrid.Instance.WorldToGrid(GlobalPosition);

		for (int x = 0; x < Width; x++)
		{
			for (int z = 0; z < Depth; z++)
			{
				if (!IsPerimeter(x, z))
				{
					continue;
				}

				for (int y = 0; y < WallHeight; y++)
				{
					SpawnBlock(new Vector3I(RoomMath.CenterOffset(x, Width), y, RoomMath.CenterOffset(z, Depth)));
				}
			}
		}
	}

	/// <summary>La lógica real vive en RoomMath (testeada aparte, ver Tests/GameLogic.Tests).</summary>
	private bool IsPerimeter(int x, int z)
	{
		return RoomMath.IsPerimeterCell(x, z, Width, Depth);
	}

	private void SpawnGasZoneIfNeeded()
	{
		if (!HasGas)
		{
			return;
		}

		if (GasZoneScene == null)
		{
			GD.PushWarning($"{Name}: HasGas está activo pero falta asignar GasZoneScene; no se genera el gas.");
			return;
		}

		var gasZone = GasZoneScene.Instantiate<GasZone>();

		// Centro de la sala, un poco levantado del piso — cubre bien la
		// altura del jugador sin meterse en el techo bajo (WallHeight suele
		// ser 3 celdas = 6m).
		gasZone.Position = new Vector3(0.0f, 0.5f, 0.0f);

		AddChild(gasZone);
	}

	private void SpawnChestIfNeeded()
	{
		if (Loot == ChestLoot.None)
		{
			return;
		}

		if (ChestScene == null)
		{
			GD.PushWarning($"{Name}: la sala tiene cofre pero falta asignar ChestScene.");
			return;
		}

		var chest = ChestScene.Instantiate<Chest>();

		chest.Loot = Loot;
		chest.NoteText = NoteText;
		chest.RoomCoord = RoomGridCoord;

		// En una sala con gas, el cofre va justo en el medio del gas: el
		// dilema "¿entro o no?" del GDD tiene que tener premio.
		chest.Position = HasGas
			? InteriorCenter
			: InteriorCenter + new Vector3(4.0f, 0.0f, -4.0f);

		AddChild(chest);
	}

	private void SpawnExitIfNeeded()
	{
		if (!HasExit)
		{
			return;
		}

		if (ExitScene == null)
		{
			GD.PushWarning($"{Name}: la sala tiene salida pero falta asignar ExitScene.");
			return;
		}

		var exit = ExitScene.Instantiate<Node3D>();
		exit.Position = InteriorCenter;
		AddChild(exit);
	}

	private void SpawnBlock(Vector3I localCell)
	{
		BlockGrid grid = BlockGrid.Instance;

		if (grid.IsOccupied(originCell + localCell))
		{
			// Habitaciones vecinas pueden compartir pared: la primera en
			// registrarse gana (ver el comentario de orden de generación en
			// el doc-comment de la clase).
			if (LogSharedWalls)
			{
				GD.Print($"{Name}: celda {originCell + localCell} ya ocupada por otra Room; se omite este bloque.");
			}

			return;
		}

		if (WorldState.Instance != null && WorldState.Instance.IsCellMined(RoomGridCoord, localCell))
		{
			// Ya lo habían minado en una visita anterior (mundo infinito
			// descargado y vuelto a cargar) o en una partida guardada: no se
			// regenera, para que el streaming no "repare" agujeros que hizo
			// el jugador.
			return;
		}

		var block = BlockScene.Instantiate<DestructibleBlock>();

		block.Mineral = WallMineral;
		block.MaxHealth = WallBlockHealth;
		block.RequiredTool = MineralRules.RequiredTool(WallMineral);

		// La posición se fija ANTES de AddChild: al entrar al árbol el bloque
		// corre su _Ready, y ahí ya necesita estar donde corresponde para
		// registrarse en la celda correcta.
		block.Position = grid.GridToWorld(localCell);

		// "localCell" es un parámetro propio de ESTA llamada a SpawnBlock (no
		// una variable de loop compartida), así que el lambda lo captura
		// correctamente sin necesidad de copiarlo aparte.
		// Se marca minada sin importar quién la rompió (jugador o zombie);
		// el mineral lo suma MiningComponent solo cuando mina el jugador.
		block.Destroyed += (_) => WorldState.Instance?.MarkCellMined(RoomGridCoord, localCell);

		(BlockContainer ?? (Node3D)this).AddChild(block);

		spawnedBlocks++;
	}
}
