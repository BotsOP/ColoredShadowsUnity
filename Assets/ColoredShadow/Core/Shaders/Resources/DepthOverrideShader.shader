Shader "CustomShadows/ColorOnlyNoDepth"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _ShadowID;

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_full v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.pos *= 1 - saturate(ceil(_ShadowID));
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}