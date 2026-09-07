using System;
using System.Linq;
using System.Text.Json;

// Sin framework de testing: solo asserts a mano que imprimen PASS/FAIL y
// devuelven código de salida != 0 si algo falla, para que CI lo detecte.
// Cubre la lógica de Scripts/ que NO depende de GodotSharp: RoomMath (dónde
// van las paredes de una Room), DeterministicHash + RoomMineralTable (la
// generación procedural del mundo infinito) y WorldSaveState (el guardado).
// Nada de esto se pudo probar dentro del proyecto de Godot en sí — eso
// necesita abrirlo en el editor.

int total = 0;
int failures = 0;

void Check(string name, bool condition)
{
	total++;

	if (condition)
	{
		Console.WriteLine($"PASS  {name}");
	}
	else
	{
		failures++;
		Console.WriteLine($"FAIL  {name}");
	}
}

// --- RoomMath.IsPerimeterCell ---

Check("perimetro: esquina (0,0) de una sala 12x12 es pared", RoomMath.IsPerimeterCell(0, 0, 12, 12));
Check("perimetro: esquina opuesta (11,11) es pared", RoomMath.IsPerimeterCell(11, 11, 12, 12));
Check("perimetro: celda interior (5,5) NO es pared", !RoomMath.IsPerimeterCell(5, 5, 12, 12));
Check("perimetro: borde x=0 con z interior es pared", RoomMath.IsPerimeterCell(0, 5, 12, 12));
Check("perimetro: borde z=depth-1 con x interior es pared", RoomMath.IsPerimeterCell(5, 9, 10, 10));
Check("perimetro: celda interior cerca del borde (1,1) NO es pared", !RoomMath.IsPerimeterCell(1, 1, 12, 12));

// --- RoomMath.CenterOffset ---

Check("centro: offset(0, 12) es -6", RoomMath.CenterOffset(0, 12) == -6);
Check("centro: offset(11, 12) es 5", RoomMath.CenterOffset(11, 12) == 5);
Check("centro: el ancho total (max-min) es width-1", RoomMath.CenterOffset(11, 12) - RoomMath.CenterOffset(0, 12) == 11);

// --- Regresión: dos Rooms de 12x12 (CellSize=2m) separadas 24m en el mundo
// quedan ADYACENTES, sin hueco entre ellas (ver game_architecture_review.md
// en la memoria del proyecto).
{
	const float cellSize = 2.0f;
	const int width = 12;

	int startFarWallCell = 0 + RoomMath.CenterOffset(width - 1, width);
	int roomNorthOriginCellZ = (int)(24.0f / cellSize);
	int northNearWallCell = roomNorthOriginCellZ + RoomMath.CenterOffset(0, width);

	Check(
		"regresion: la pared lejana de RoomStart y la cercana de RoomNorth son celdas consecutivas (adyacentes, sin hueco ni superposición)",
		northNearWallCell - startFarWallCell == 1
	);
}

// --- DeterministicHash ---

Check(
	"hash: misma semilla y coordenada siempre dan el mismo resultado",
	DeterministicHash.Combine(42, 3, -7) == DeterministicHash.Combine(42, 3, -7)
);
Check(
	"hash: coordenadas distintas casi seguro dan resultados distintos",
	DeterministicHash.Combine(42, 3, -7) != DeterministicHash.Combine(42, 3, -8)
);
Check(
	"hash: semillas distintas casi seguro dan resultados distintos para la misma coordenada",
	DeterministicHash.Combine(1, 0, 0) != DeterministicHash.Combine(2, 0, 0)
);
Check(
	"hash: no explota con coordenadas negativas grandes",
	DeterministicHash.Combine(0, int.MinValue, int.MaxValue) == DeterministicHash.Combine(0, int.MinValue, int.MaxValue)
);

// --- RoomMineralTable ---

Check("minerales: roll 0 es Stone", RoomMineralTable.Pick(0) == MineralType.Stone);
Check("minerales: roll 54 es Stone (último del rango)", RoomMineralTable.Pick(54) == MineralType.Stone);
Check("minerales: roll 55 es Coal (primero del rango)", RoomMineralTable.Pick(55) == MineralType.Coal);
Check("minerales: roll 92 es Iron", RoomMineralTable.Pick(92) == MineralType.Iron);
Check("minerales: roll 98 es Gold", RoomMineralTable.Pick(98) == MineralType.Gold);
Check("minerales: roll 99 es Bedrock (el más raro)", RoomMineralTable.Pick(99) == MineralType.Bedrock);

// --- WorldSaveState: minado ---

{
	var state = new WorldSaveState();

	Check("guardado: una celda nueva no está minada", !state.IsCellMined(0, 0, 1, 1, 1));

	state.MarkCellMined(0, 0, 1, 1, 1);

	Check("guardado: después de marcarla, la celda queda minada", state.IsCellMined(0, 0, 1, 1, 1));
	Check("guardado: una celda distinta en la misma sala sigue sin minar", !state.IsCellMined(0, 0, 1, 1, 2));
	Check("guardado: la misma celda en OTRA sala sigue sin minar", !state.IsCellMined(1, 0, 1, 1, 1));
}

// --- WorldSaveState: inventario ---

{
	var state = new WorldSaveState();

	Check("inventario: arranca en 0", state.GetMineralCount("Stone") == 0);

	int afterFirst = state.AddMineral("Stone");
	int afterSecond = state.AddMineral("Stone");

	Check("inventario: sumar de a uno acumula (1, luego 2)", afterFirst == 1 && afterSecond == 2);
	Check("inventario: el conteo final coincide con lo acumulado", state.GetMineralCount("Stone") == 2);
	Check("inventario: otro mineral no se mezcla con el primero", state.GetMineralCount("Coal") == 0);
}

// --- WorldSaveState: round-trip completo (estado -> DTO -> JSON -> DTO -> estado) ---

{
	var original = new WorldSaveState { Seed = 12345 };

	original.MarkCellMined(0, 0, 1, 2, 3);
	original.MarkCellMined(0, 0, -1, 0, -1);
	original.MarkCellMined(2, -3, 5, 5, 5);
	original.AddMineral("Stone", 4);
	original.AddMineral("Coal", 1);

	WorldSaveData dto = original.ToDto(playerX: 10.5f, playerY: 1.0f, playerZ: -20.25f);
	string json = JsonSerializer.Serialize(dto);
	WorldSaveData roundTrippedDto = JsonSerializer.Deserialize<WorldSaveData>(json);
	WorldSaveState restored = WorldSaveState.FromDto(roundTrippedDto);

	Check("guardado: el JSON de ida y vuelta conserva la semilla", roundTrippedDto.Seed == 12345);
	Check("guardado: el JSON de ida y vuelta conserva la posición del jugador", roundTrippedDto.PlayerX == 10.5f && roundTrippedDto.PlayerZ == -20.25f);
	Check("guardado: el estado restaurado recuerda las celdas minadas", restored.IsCellMined(0, 0, 1, 2, 3) && restored.IsCellMined(2, -3, 5, 5, 5));
	Check("guardado: el estado restaurado NO inventa celdas minadas que no estaban", !restored.IsCellMined(0, 0, 9, 9, 9));
	Check("guardado: el estado restaurado conserva el inventario", restored.GetMineralCount("Stone") == 4 && restored.GetMineralCount("Coal") == 1);
}

// --- ProceduralNoise ---

Check(
	"ruido: Hash01 es determinístico (misma semilla+coordenada = mismo valor)",
	ProceduralNoise.Hash01(7, 3, -4) == ProceduralNoise.Hash01(7, 3, -4)
);
Check(
	"ruido: Hash01 cae siempre en [0,1)",
	Enumerable.Range(0, 50).All(i => {
		double h = ProceduralNoise.Hash01(123, i, i * 2 - 10);
		return h >= 0.0 && h < 1.0;
	})
);
Check(
	"ruido: ValueNoise es determinístico",
	ProceduralNoise.ValueNoise(42, 1.5, 2.25) == ProceduralNoise.ValueNoise(42, 1.5, 2.25)
);
Check(
	"ruido: ValueNoise se mantiene en [0,1] (interpola entre hashes que ya están en ese rango)",
	Enumerable.Range(0, 50).All(i => {
		double v = ProceduralNoise.ValueNoise(99, i * 0.37, i * 1.11);
		return v >= 0.0 && v <= 1.0;
	})
);
Check(
	"ruido: Fbm es determinístico",
	ProceduralNoise.Fbm(5, 4.0, 4.0, 4) == ProceduralNoise.Fbm(5, 4.0, 4.0, 4)
);
Check(
	"ruido: Fbm se mantiene en [0,1] (promedio ponderado de ValueNoise, cada uno ya acotado)",
	Enumerable.Range(0, 50).All(i => {
		double v = ProceduralNoise.Fbm(2024, i * 0.53, i * 0.29, 4);
		return v >= 0.0 && v <= 1.0;
	})
);
Check(
	"ruido: coordenadas bien distintas casi seguro dan Fbm distinto (no está devolviendo una constante)",
	ProceduralNoise.Fbm(1, 0.0, 0.0, 4) != ProceduralNoise.Fbm(1, 37.0, 91.0, 4)
);
Check(
	"ruido: octaves=0 no explota (se trata como mínimo 1 octava)",
	ProceduralNoise.Fbm(1, 1.0, 1.0, 0) == ProceduralNoise.Fbm(1, 1.0, 1.0, 1)
);

// --- MineralMaterialProfile ---

Check(
	"materiales: el oro es más metálico que la piedra",
	MineralMaterialProfile.For(MineralType.Gold).Metallic > MineralMaterialProfile.For(MineralType.Stone).Metallic
);
Check(
	"materiales: el bedrock es más rugoso (menos pulido) que el oro",
	MineralMaterialProfile.For(MineralType.Bedrock).Roughness > MineralMaterialProfile.For(MineralType.Gold).Roughness
);
Check(
	"materiales: el piso de las salas no es metálico (es tierra/roca, no un bloque de mineral)",
	MineralMaterialProfile.Floor().Metallic == 0.0f
);
Check(
	"materiales: todos los parámetros PBR de cada mineral quedan en rango [0,1]",
	Enum.GetValues<MineralType>().All(mineral => {
		var p = MineralMaterialProfile.For(mineral);
		float[] values = { p.BaseR, p.BaseG, p.BaseB, p.VeinR, p.VeinG, p.VeinB, p.Metallic, p.Roughness, p.VeinDensity };
		return values.All(v => v >= 0.0f && v <= 1.0f);
	})
);

Console.WriteLine();
Console.WriteLine($"{total - failures}/{total} tests OK");

return failures == 0 ? 0 : 1;
