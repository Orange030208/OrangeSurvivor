using System;

namespace Orange.GameServices
{
    [Flags]
    public enum GameServiceTickMode
    {
        None = 0,
        Update = 1,
        FixedUpdate = 2,
        LateUpdate = 4,
        UnscaledUpdate = 8
    }
}
