using System.Collections.Generic;
using ColoredShadows.Scripts;
using UnityEngine;

public static class CustomLightManager
{
    public static List<CustomLight> customLights = new List<CustomLight>();

    static CustomLightManager()
    {
        customLights = new List<CustomLight>();
    }
}
