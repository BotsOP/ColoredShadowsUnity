using UnityEngine;
using UnityEngine.VFX;

#if VFX_AVAILABLE
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
#endif
[System.Serializable]
struct ShadowBufferStruct
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    public float shadowID;
}
