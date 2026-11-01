using Godot;

/// <summary>
/// Efecto de la explosión del gas: un destello y una bola de fuego que crece y se apaga.
/// Se anima con un Tween y se borra sola.
/// </summary>
public partial class GasExplosion : Node3D
{
	[ExportCategory("GasExplosion")]
	[ExportGroup("Referencias")]
	[Export] public OmniLight3D Flash;
	[Export] public MeshInstance3D Fireball;

	[ExportGroup("Animación")]
	[Export] public float Duration = 0.7f;
	[Export] public float FinalScale = 7.0f;

	public override void _Ready()
	{
		Tween tween = CreateTween().SetParallel();

		if (Fireball != null)
		{
			Fireball.Scale = Vector3.One * 0.3f;
			tween.TweenProperty(Fireball, "scale", Vector3.One * FinalScale, Duration)
				.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);

			if (Fireball.MaterialOverride is StandardMaterial3D material)
			{
				// Copia propia, para no apagar las otras explosiones.
				Fireball.MaterialOverride = (Material)material.Duplicate();
				tween.TweenProperty(Fireball.MaterialOverride, "albedo_color:a", 0.0f, Duration);
			}
		}

		if (Flash != null)
		{
			tween.TweenProperty(Flash, "light_energy", 0.0f, Duration);
		}

		tween.Chain().TweenCallback(Callable.From(QueueFree));
	}
}
