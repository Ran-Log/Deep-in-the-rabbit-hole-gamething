using Godot;

/// <summary>
/// Una sala: un piso y un perímetro de bloques generado según Width y Depth.
/// También pone el gas, el cofre y la salida si la sala los tiene.
/// El origen es el centro de la primera capa de bloques; el piso queda media celda abajo.
/// </summary>
public partial class Room : Node3D
{
	[ExportCategory("Room")]
	[ExportGroup("Referencias")]
	[Export] public PackedScene BlockScene;
	[Export] public PackedScene GasZoneScene;
	[Export] public PackedScene ChestScene;
	[Export] public PackedScene ExitScene;
	[Export] public PackedScene NestScene;
	[Export] public StaticBody3D FloorBody;
	[Export] public MeshInstance3D FloorMesh;
	[Export] public CollisionShape3D FloorCollision;
	[Export] public Node3D BlockContainer;

	[ExportGroup("Tamaño")]
	// Ancho y profundidad totales en celdas, contando las dos paredes.
	[Export(PropertyHint.Range, "3,64,1")] public int Width = 10;
	[Export(PropertyHint.Range, "3,64,1")] public int Depth = 10;
	[Export(PropertyHint.Range, "1,16,1")] public int WallHeight = 3;

	[ExportGroup("Techo")]
	// Lado del agujero del tragaluz, en celdas.
	[Export(PropertyHint.Range, "1,8,1")] public int SkylightCells = 3;

	[ExportGroup("Paredes")]
	[Export] public MineralType WallMineral = MineralType.Stone;
	[Export] public int WallBlockHealth = 5;

	[ExportGroup("Contenido")]
	// Posición en la grilla de salas (no de bloques). La pone WorldStreamer.
	[Export] public Vector2I RoomGridCoord = Vector2I.Zero;
	[Export] public bool HasGas = false;

	[ExportGroup("Debug")]
	// Avisa en consola cuando dos salas piden la misma celda.
	[Export] public bool LogSharedWalls = false;

	// Los pone WorldStreamer desde el RoomSpec. Loot = None significa sin cofre.
	public ChestLoot Loot { get; set; } = ChestLoot.None;
	public string NoteText { get; set; } = "";
	public bool HasExit { get; set; }
	public bool HasNest { get; set; }
	// Agujero en el techo (GDD: de día entra luz). Sin él, la sala es oscura.
	public bool HasSkylight { get; set; }

	public int CeilingPieceCount { get; private set; }

	// Con tamaño par, el centro de la grilla queda corrido media celda (ver RoomMath.CenterOffset).
	private Vector3 InteriorCenter => new Vector3(-1.0f, FloorTopY, -1.0f);

	// Ej. para hacer aparecer enemigos adentro de la sala.
	public Vector3 InteriorCenterGlobal => ToGlobal(InteriorCenter);

	private float FloorTopY => -(BlockGrid.Instance?.CellSize ?? 2.0f) * 0.5f;

	private Vector3I originCell;
	private int spawnedBlocks;

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
		GenerateCeiling();
		SpawnGasZoneIfNeeded();
		SpawnChestIfNeeded();
		SpawnExitIfNeeded();
		SpawnNestIfNeeded();
	}

	// Una losa sobre las paredes (no son bloques de la grilla: así no suma cuerpos físicos).
	// Con tragaluz, son 4 franjas alrededor de un agujero en el medio.
	private void GenerateCeiling()
	{
		float cell = BlockGrid.Instance.CellSize;
		float centerX = (RoomMath.CenterOffset(0, Width) + RoomMath.CenterOffset(Width - 1, Width)) * cell * 0.5f;
		float centerZ = (RoomMath.CenterOffset(0, Depth) + RoomMath.CenterOffset(Depth - 1, Depth)) * cell * 0.5f;
		float halfWidth = Width * cell * 0.5f;
		float halfDepth = Depth * cell * 0.5f;
		float halfHole = SkylightCells * cell * 0.5f;

		var full = (centerX - halfWidth, centerZ - halfDepth, centerX + halfWidth, centerZ + halfDepth);
		var hole = (centerX - halfHole, centerZ - halfHole, centerX + halfHole, centerZ + halfHole);

		// Las paredes ocupan las celdas 0..WallHeight-1; la losa va justo encima.
		float y = WallHeight * cell;

		var ceiling = new StaticBody3D { Name = "Ceiling" };
		AddChild(ceiling);

		foreach (var piece in RoomMath.CeilingPieces(full, hole, HasSkylight))
		{
			AddCeilingPiece(ceiling, piece, y, cell);
		}
	}

	private void AddCeilingPiece(StaticBody3D ceiling, (float MinX, float MinZ, float MaxX, float MaxZ) piece, float y, float thickness)
	{
		var size = new Vector3(piece.MaxX - piece.MinX, thickness, piece.MaxZ - piece.MinZ);
		var center = new Vector3((piece.MinX + piece.MaxX) * 0.5f, y, (piece.MinZ + piece.MaxZ) * 0.5f);

		ceiling.AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = size },
			Position = center,
			MaterialOverride = MineralTextureFactory.GetFloorMaterial(),
		});

		ceiling.AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = size },
			Position = center,
		});

		CeilingPieceCount++;
	}

	private void SpawnNestIfNeeded()
	{
		if (!HasNest || NestScene == null)
		{
			return;
		}

		// Un nido destruido no vuelve (GDD).
		if (WorldState.Instance != null && WorldState.Instance.IsNestDestroyed(RoomGridCoord))
		{
			return;
		}

		var nest = NestScene.Instantiate<AntNest>();
		nest.RoomCoord = RoomGridCoord;
		nest.Position = InteriorCenter + new Vector3(-4.0f, 0.0f, -4.0f);
		AddChild(nest);
	}

	private void SnapOriginToGrid()
	{
		BlockGrid grid = BlockGrid.Instance;

		GlobalPosition = grid.GridToWorld(grid.WorldToGrid(GlobalPosition));
	}

	// Estira el piso a todo el tamaño de la sala, en la capa y = -1.
	private void ResizeFloor()
	{
		float cell = BlockGrid.Instance.CellSize;
		Vector3 size = new Vector3(Width * cell, cell, Depth * cell);
		Vector3 center = new Vector3(0.0f, -cell, 0.0f);

		if (FloorMesh?.Mesh is BoxMesh boxMesh)
		{
			boxMesh.Size = size;
			FloorMesh.Position = center;

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

		// La sala no puede estar rotada: las celdas se calculan sin rotación.
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

	private bool IsPerimeter(int x, int z)
	{
		return RoomMath.IsPerimeterCell(x, z, Width, Depth);
	}

	private void SpawnGasZoneIfNeeded()
	{
		// El gas que ya explotó no vuelve.
		if (!HasGas || (WorldState.Instance != null && WorldState.Instance.IsGasExploded(RoomGridCoord)))
		{
			return;
		}

		if (GasZoneScene == null)
		{
			GD.PushWarning($"{Name}: HasGas está activo pero falta asignar GasZoneScene; no se genera el gas.");
			return;
		}

		var gasZone = GasZoneScene.Instantiate<GasZone>();
		gasZone.RoomCoord = RoomGridCoord;

		// Un poco sobre el piso, para cubrir la altura del jugador.
		gasZone.Position = new Vector3(InteriorCenter.X, 0.5f, InteriorCenter.Z);

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

		// Con gas, el cofre va en el medio: el dilema "¿entro o no?" del GDD necesita premio.
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
			// Dos salas vecinas pueden pedir la misma celda: gana la primera.
			if (LogSharedWalls)
			{
				GD.Print($"{Name}: celda {originCell + localCell} ya ocupada por otra Room; se omite este bloque.");
			}

			return;
		}

		if (WorldState.Instance != null && WorldState.Instance.IsCellMined(RoomGridCoord, localCell))
		{
			// Ya se había minado antes: no se regenera al volver a cargar la sala.
			return;
		}

		var block = BlockScene.Instantiate<DestructibleBlock>();

		block.Mineral = WallMineral;
		block.MaxHealth = WallBlockHealth;
		block.RequiredTool = MineralRules.RequiredTool(WallMineral);

		// Antes de AddChild: en su _Ready el bloque se registra en la celda donde esté.
		block.Position = grid.GridToWorld(localCell);

		// Se marca minada la rompa quien la rompa (jugador o zombie).
		block.Destroyed += (_) => WorldState.Instance?.MarkCellMined(RoomGridCoord, localCell);

		(BlockContainer ?? (Node3D)this).AddChild(block);

		spawnedBlocks++;
	}
}
