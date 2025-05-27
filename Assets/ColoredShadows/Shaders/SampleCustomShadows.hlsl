#ifndef SAMPLE_CUSTOM_CUBEMAP_INCLUDED
#define SAMPLE_CUSTOM_CUBEMAP_INCLUDED

void SampleCustomCubeMap(float3 direction, out float2 uv, out int faceIndex)
{
    uv = float2(-1, -1);
    float forward = dot(direction, float3(0.0f, 0.0f, 1.0f));
    float right = dot(direction, float3(1.0f, 0.0f, 0.0f));
    float back = dot(direction, float3(0.0f, 0.0f, -1.0f));
    float left = dot(direction, float3(-1.0f, 0.0f, 0.0f));
    float down = dot(direction, float3(0.0f, -1.0f, 0.0f));
    float up = dot(direction, float3(0.0f, 1.0f, 0.0f));
    float closestDir = min(up,min(down,min(min(min(forward, right), back), left)));
    faceIndex = -1;
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
    // uv = float2((uv.x / 6.0) + ((1.0/6.0) * faceIndex), uv.y);
}

// sampler2D _ColoredShadowMap0;
Texture2D _ColoredShadowMap0;
SamplerState my_linear_clamp_sampler;
SamplerState my_point_clamp_sampler;
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

float4 SampleColoredShadowMap(float2 uv, int mapIndex, out float mask)
{
    float4 output = float4(0, 0, 0, 0); // Default value if no case matches
    mask = 0;
    
    switch (mapIndex)
    {
    case 0:
        // output = tex2D(_ColoredShadowMap0, uv);
        output = _ColoredShadowMap0.Sample(my_point_clamp_sampler, uv);
        // float4 xplus = _ColoredShadowMap0.Sample(my_linear_clamp_sampler, uv + float2((1.0 / 1024 / 6), 0));
        // float4 xnegative = _ColoredShadowMap0.Sample(my_linear_clamp_sampler, uv - float2((1.0 / 1024 / 6), 0));
        // float4 yplus = _ColoredShadowMap0.Sample(my_linear_clamp_sampler, uv + float2(0, (1.0 / 1024)));
        // float4 ynegative = _ColoredShadowMap0.Sample(my_linear_clamp_sampler, uv - float2(0, (1.0 / 1024)));
        // mask += xplus.r;
        // mask += yplus.r;
        // mask += xnegative.r;
        // mask += ynegative.r;
        mask += _ColoredShadowMap0.Sample(my_linear_clamp_sampler, uv).r;
        mask /= 5;
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
    float fallOffRange;
    float farPlane;
};

float invLerp(float from, float to, float value){
    return (value - from) / (to - from);
}

float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value){
    float rel = invLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

int CurrentAmountCustomLights;
StructuredBuffer<LightInformation> ColoredShadowLightInformation;
void SampleColoredShadows_float(float3 worldPos, out float4 output, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask)
{
    output = float4(0, 0, 0, 0);
    lightPos = float3(-999999999, -999999999, -999999999);
    fallOffRange = 0;
    float lowestDist = 99999999;
    finalUV = float2(0, 0);
    for (int i = 0; i < CurrentAmountCustomLights; ++i)
    {
        LightInformation lightInformation = ColoredShadowLightInformation[i];
        float2 uv = float2(0, 0);
        float4 tempOutput;
        float4 lightSpace;
        float3 lightUv;
        float dist = distance(worldPos, lightInformation.lightPos) / lightInformation.fallOffRange;
        switch (lightInformation.lightMode)
        {
        case 0: // Directional
            lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y * -1, worldPos.z, 1));
            lightUv = lightSpace.rgb / lightSpace.a;
            lightUv *= 0.5;
            lightUv += 0.5;
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation.index, mask);
            tempOutput.y = dist;
            if (tempOutput.x > 0 && lightUv.x > 0 && lightUv.x < 1 && lightUv.y > 0 && lightUv.y < 1)
            {
                output = tempOutput;
                lightPos = lightInformation.lightPos;
                fallOffRange = -1;
                finalUV = lightUv.rg;
            }
            break;
        case 1: // Spot
            lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y * -1, worldPos.z, 1));
            lightUv = lightSpace.rgb / lightSpace.a;
            lightUv *= 0.5;
            lightUv += 0.5;
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation.index, mask);
            tempOutput.y = dist;
            if (tempOutput.x > 0 && lightUv.x > 0 && lightUv.x < 1 && lightUv.y > 0 && lightUv.y < 1 && distance(worldPos, lightInformation.lightPos) < lightInformation.fallOffRange)
            {
                output = tempOutput;
                lightPos = lightInformation.lightPos;
                fallOffRange = lightInformation.fallOffRange;
                finalUV = lightUv.rg;
            }
            break;
        case 2: //Point
            float3 dir = normalize(lightInformation.lightPos - worldPos);
            int faceIndex;
            SampleCustomCubeMap(dir, uv, faceIndex);
            float2 cubemapUV = float2((uv.x / 6.0) + ((1.0/6.0) * faceIndex), uv.y);
            tempOutput = SampleColoredShadowMap(cubemapUV, lightInformation.index, mask);
            finalUV = uv;
            lowestDist = dist;
            output = tempOutput;
            lightPos = lightInformation.lightPos;
            fallOffRange = lightInformation.fallOffRange;
            // if (tempOutput.x > 0 && dist < lowestDist && dist < 1)
            // {
            //     finalUV = uv;
            //     lowestDist = dist;
            //     output = tempOutput;
            //     lightPos = lightInformation.lightPos;
            //     fallOffRange = lightInformation.fallOffRange;
            // }
            break;
        }
    }
}

void SampleColoredShadows_half(float3 worldPos, out float4 output, out float2 finalUV, out float3 lightPos, out float fallOffRange)
{
}

#endif 