using System;

/// <summary>
/// Daño por caída (GDD 2.1.2: "sufre daño al caer si supera cierta altura").
/// C# puro, sin Godot, para probarlo en el harness.
/// </summary>
public static class FallDamage
{
	// Hasta safeHeight metros no pasa nada; desde ahí, damagePerMeter por cada metro extra.
	public static int For(float height, float safeHeight, float damagePerMeter)
	{
		if (height <= safeHeight)
		{
			return 0;
		}

		return (int)MathF.Round((height - safeHeight) * damagePerMeter);
	}
}
