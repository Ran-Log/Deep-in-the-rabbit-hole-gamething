/// <summary>
/// Hash determinístico y sin estado (variante de splitmix64): los mismos
/// argumentos siempre dan el mismo resultado, sin importar el orden en que
/// se llame. Se usa para generar el mundo infinito de forma reproducible
/// (misma semilla + misma coordenada de sala = siempre la misma sala), algo
/// que un RandomNumberGenerator con estado secuencial no puede garantizar
/// cuando las salas se generan en cualquier orden según por dónde camine el
/// jugador. No es para criptografía.
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
