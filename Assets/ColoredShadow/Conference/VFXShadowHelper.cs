using ColoredShadows.Scripts;
using EasyButtons;
using UnityEngine;
using UnityEngine.VFX;

public class VFXShadowHelper : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private CustomLight customLight;

    [Button]
    private void SetValues()
    {
        vfx.SetGraphicsBuffer("_ShadowPositions", customLight.vfxAppendBuffer);
        vfx.SetInt("_ShadowPositionsCount", customLight.vfxAppendCount);
    }
}
