using UnityEngine;

public class CustomLight : MonoBehaviour
{
    public ColoredShadowsRenderFeature.CustomLightData lightData = new ColoredShadowsRenderFeature.CustomLightData(ColoredShadowsRenderFeature.CustomLightData.LightMode.Ortho, 0.1f, 50, 20, 20, 30, 1);
    public Vector2Int depthDimensions = new Vector2Int(1024, 1024);
    public Vector2Int shadowIDimension = new Vector2Int(1024, 1024);
}
