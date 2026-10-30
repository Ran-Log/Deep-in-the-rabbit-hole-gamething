namespace Tests.Integration;

using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MineralTextureFactoryTest
{
	[TestCase]
	public void LaTexturaMide64x64()
	{
		Texture2D texture = MineralTextureFactory.GetBlockMaterial(MineralType.Stone).AlbedoTexture;

		AssertThat(texture.GetSize()).IsEqual(new Vector2(64, 64));
	}

	[TestCase]
	public void ElMismoMineralDevuelveElMismoMaterial()
	{
		StandardMaterial3D first = MineralTextureFactory.GetBlockMaterial(MineralType.Coal);
		StandardMaterial3D second = MineralTextureFactory.GetBlockMaterial(MineralType.Coal);

		AssertThat(second).IsSame(first);
	}

	[TestCase]
	public void PiedraYOroSeVenDistintos()
	{
		byte[] stone = MineralTextureFactory.GetBlockMaterial(MineralType.Stone).AlbedoTexture.GetImage().GetData();
		byte[] gold = MineralTextureFactory.GetBlockMaterial(MineralType.Gold).AlbedoTexture.GetImage().GetData();

		AssertThat(stone).IsNotEqual(gold);
	}

	[TestCase]
	public void ElPisoTieneTextura()
	{
		StandardMaterial3D floor = MineralTextureFactory.GetFloorMaterial();

		AssertThat(floor.AlbedoTexture).IsNotNull();
		AssertThat(floor.Uv1Triplanar).IsTrue();
	}
}
