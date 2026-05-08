using UnityEngine;

public readonly struct AudioSfxPlayContext
{
    public bool HasPosition { get; }
    public Vector2 Position { get; }

    public AudioSfxPlayContext(Vector2 position)
    {
        HasPosition = true;
        Position = position;
    }

    public static AudioSfxPlayContext None => default;
}
