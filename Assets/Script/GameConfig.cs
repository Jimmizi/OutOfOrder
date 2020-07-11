using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameConfig
{
    public static float TimeMod = 1.0f;

    public static float GetDeltaTime()
    {
        return Time.deltaTime * TimeMod;
    }
}
