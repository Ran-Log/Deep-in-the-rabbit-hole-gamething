/// <summary>
/// Combina la semilla con una coordenada en un número: la misma sala sale
/// siempre igual, sin importar en qué orden se visiten.
/// </summary>
public static class DeterministicHash
{
	public static ulong Combine(ulong seed, int x, int y)
	{
		ulong h = seed;

		h ^= (ulong)(uint)x * 0x9E3779B97F4A7C15UL;
		h = Mix(h);

		h ^= (ulong)(uint)y * 0xBF58476D1CE4E5B9UL;
		h = Mix(h);

		return h;
	}

	private static ulong Mix(ulong z)
	{
		z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
		z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

		return z ^ (z >> 31);
	}
}
