using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameColor
{
    Red,
    Yellow,
    Blue,
    Transparent,
    Orange,
    Green,
    Purple
}

public static class GameColorExtensions
{
    public static GameColor Add(GameColor c1, GameColor c2)
    {
        if (c1 == GameColor.Transparent) return c2;
        if (c2 == GameColor.Transparent) return c1;

        if (c1 == c2)
            return c1;

        if ((c1 == GameColor.Red && c2 == GameColor.Yellow) ||
            (c1 == GameColor.Yellow && c2 == GameColor.Red))
            return GameColor.Orange;

        if ((c1 == GameColor.Red && c2 == GameColor.Blue) ||
            (c1 == GameColor.Blue && c2 == GameColor.Red))
            return GameColor.Purple;

        if ((c1 == GameColor.Yellow && c2 == GameColor.Blue) ||
            (c1 == GameColor.Blue && c2 == GameColor.Yellow))
            return GameColor.Green;

        // Default fallback
        return GameColor.Transparent;
    }

    // Deprecated
    public static Color ToColor(GameColor gameColor)
    {
        return Color.red;
    }
}