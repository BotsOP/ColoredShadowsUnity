using EasyButtons;
using UnityEngine;
using UnityEngine.VFX;

public class VFXShadowHelper : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;

    [Button]
    private void SetValues()
    {
        Texture shadowMap = Shader.GetGlobalTexture("_ColoredShadowMap0");
        vfx.SetTexture("_ColoredShadowMap0", shadowMap);
    }
}
