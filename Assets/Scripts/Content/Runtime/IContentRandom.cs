public interface IContentRandom
{
    float Value01();
    int Range(int minInclusive, int maxExclusive);
}

public sealed class UnityContentRandom : IContentRandom
{
    public float Value01()
    {
        return UnityEngine.Random.value;
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return UnityEngine.Random.Range(minInclusive, maxExclusive);
    }
}

public sealed class SystemContentRandom : IContentRandom
{
    private readonly System.Random random;

    public SystemContentRandom(int seed)
    {
        random = new System.Random(seed);
    }

    public float Value01()
    {
        return (float)random.NextDouble();
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        return random.Next(minInclusive, maxExclusive);
    }
}
