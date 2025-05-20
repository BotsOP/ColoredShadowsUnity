#ifndef SAMPLE_CUSTOM_CUBEMAP_INCLUDED
#define SAMPLE_CUSTOM_CUBEMAP_INCLUDED

void SampleCustomCubeMap(float3 direction, out float2 uv)
{
    uv = float2(-1, -1);
    float forward = dot(direction, float3(0.0f, 0.0f, 1.0f));
    float right = dot(direction, float3(1.0f, 0.0f, 0.0f));
    float back = dot(direction, float3(0.0f, 0.0f, -1.0f));
    float left = dot(direction, float3(-1.0f, 0.0f, 0.0f));
    float down = dot(direction, float3(0.0f, -1.0f, 0.0f));
    float up = dot(direction, float3(0.0f, 1.0f, 0.0f));
    float closestDir = min(up,min(down,min(min(min(forward, right), back), left)));
    int faceIndex = -1;
    if (closestDir == forward)
    {
        faceIndex = 0;
    }
    if (closestDir == back)
    {
        faceIndex = 2;
    }
    if (closestDir == left)
    {
        faceIndex = 3;
    }
    if (closestDir == right)
    {
        faceIndex = 1;
    }
    if (closestDir == down)
    {
        faceIndex = 4;
    }
    if (closestDir == up)
    {
        faceIndex = 5;
    }
    switch(faceIndex) {
    case 1: // POSITIVE_X
        uv = float2(-direction.z, -direction.y) / abs(direction.x);
        break;
    case 3: // NEGATIVE_X
        uv = float2(direction.z, -direction.y) / abs(direction.x);
        break;
    case 5: // POSITIVE_Y
        uv = float2(direction.x, direction.z) / abs(direction.y);
        break;
    case 4: // NEGATIVE_Y
        uv = float2(direction.x, -direction.z) / abs(direction.y);
        break;
    case 0: // POSITIVE_Z
        uv = float2(direction.x, -direction.y) / abs(direction.z);
        break;
    case 2: // NEGATIVE_Z
        uv = float2(-direction.x, -direction.y) / abs(direction.z);
        break;
    }
    uv = uv * 0.5 + 0.5;
    uv = float2(1 - uv.x, uv.y);
    uv = float2((uv.x / 6.0) + ((1.0/6.0) * faceIndex), uv.y);
    // uv.x = faceIndex;
}

sampler2D _ColoredShadowMap0;
sampler2D _ColoredShadowMap1;
sampler2D _ColoredShadowMap2;
sampler2D _ColoredShadowMap3;
sampler2D _ColoredShadowMap4;
sampler2D _ColoredShadowMap5;
sampler2D _ColoredShadowMap6;
sampler2D _ColoredShadowMap7;
sampler2D _ColoredShadowMap8;
sampler2D _ColoredShadowMap9;
sampler2D _ColoredShadowMap10;

float2 SampleColoredShadowMap(float2 uv, int mapIndex)
{
    float2 output = float2(0, 0); // Default value if no case matches
    
    switch (mapIndex)
    {
    case 0:
        output = tex2D(_ColoredShadowMap0, uv);
        break;
    case 1:
        output = tex2D(_ColoredShadowMap1, uv);
        break;
    case 2:
        output = tex2D(_ColoredShadowMap2, uv);
        break;
    case 3:
        output = tex2D(_ColoredShadowMap3, uv);
        break;
    case 4:
        output = tex2D(_ColoredShadowMap4, uv);
        break;
    case 5:
        output = tex2D(_ColoredShadowMap5, uv);
        break;
    case 6:
        output = tex2D(_ColoredShadowMap6, uv);
        break;
    case 7:
        output = tex2D(_ColoredShadowMap7, uv);
        break;
    case 8:
        output = tex2D(_ColoredShadowMap8, uv);
        break;
    case 9:
        output = tex2D(_ColoredShadowMap9, uv);
        break;
    case 10:
        output = tex2D(_ColoredShadowMap10, uv);
        break;
    }
    
    return output;
}

struct LightInformation
{
    int index;
    int lightMode;
    float4x4 lightMatrix;
    float3 lightPos;
};

int CurrentAmountCustomLights;
StructuredBuffer<LightInformation> ColoredShadowLightInformation;
// The function signature must match exactly what you define in the Custom Function node
void SampleColoredShadows_float(float3 worldPos, out float2 output)
{
    output = float2(0, 0);
    for (int i = 0; i < CurrentAmountCustomLights; ++i)
    {
        LightInformation lightInformation = ColoredShadowLightInformation[i];
        float3 dir = normalize(lightInformation.lightPos - worldPos);
        float2 uv = float2(0, 0);
        SampleCustomCubeMap(dir, uv);
        output = max(SampleColoredShadowMap(uv, lightInformation.index), output);
    }
}





// Half precision version for mobile platforms
// void ProceduralWave_half(half2 UV, half Time, half WaveCount, half WaveSpeed, half WaveAmplitude, out half3 Displacement, out half3 Normal)
// {
// }

#endif 