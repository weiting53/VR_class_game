using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Cyan,
    Magenta,
    White,
    Pure
}

public static class GameColorExtensions
{
    public static GameColor Add(GameColor c1, GameColor c2)
    {
        if (c1 == GameColor.Pure) return c2;
        if (c2 == GameColor.Pure) return c1;

        if (c1 == c2)
            return c1;

        if ((c1 == GameColor.Red && c2 == GameColor.Green) ||
            (c1 == GameColor.Green && c2 == GameColor.Red))
            return GameColor.Yellow;

        if ((c1 == GameColor.Red && c2 == GameColor.Blue) ||
            (c1 == GameColor.Blue && c2 == GameColor.Red))
            return GameColor.Magenta;

        if ((c1 == GameColor.Green && c2 == GameColor.Blue) ||
            (c1 == GameColor.Blue && c2 == GameColor.Green))
            return GameColor.Cyan;

        // Default fallback
        return GameColor.White;
    }

    public static GameColor Add(GameColor c1, GameColor c2, GameColor c3)
    {
        return GameColor.White;
    }

    public static Color ToColor(GameColor gameColor)
    {
        switch (gameColor)
        {
            case GameColor.Red:
                return Color.red;
            case GameColor.Green:
                return Color.green;
            case GameColor.Blue:
                return Color.blue;
            case GameColor.Yellow:
                return Color.yellow;
            case GameColor.Cyan:
                return Color.cyan;
            case GameColor.Magenta:
                return Color.magenta;
            case GameColor.White:
                return Color.white;
            default:
                return Color.white;
        }
    }
}

public class QuarterRing : MonoBehaviour
{
    public GameColor color;
    private Renderer renderer;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.material.color = GameColorExtensions.ToColor(color);
    }

    public void SetColor(GameColor c)
    {
        Debug.Log("Set color");
        color = c;
        renderer.material.color = GameColorExtensions.ToColor(color);
    }

    public void SwitchColor()
    {
        if (color == GameColor.Red)
            SetColor(GameColor.Green);
        else if (color == GameColor.Green)
            SetColor(GameColor.Blue);
        else if (color == GameColor.Blue)
            SetColor(GameColor.Red);
    }
}
