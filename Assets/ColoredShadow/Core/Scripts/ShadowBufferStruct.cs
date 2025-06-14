using UnityEngine;
using UnityEngine.VFX;

[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
struct ShadowBufferStruct
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    public float shadowID;
}
