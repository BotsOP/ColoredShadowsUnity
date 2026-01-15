#ifndef SAMPLE_CUSTOM_SHADOW_INCLUDED
#define SAMPLE_CUSTOM_SHADOW_INCLUDED

#include "Resources/CustomShadowsLibrary.hlsl"
#include "Resources/PackCustomShadowValues.hlsl"

int _CustomShadowAtlasWidth;
int _CustomShadowAtlasHeight;
SamplerState trilinear_clamp_sampler;
SamplerState point_clamp_sampler;
SamplerState linear_clamp_sampler;
Texture2D _ColoredShadowMap0;
Texture2D _DepthShadowMap;

int _CurrentAmountCustomLights;
StructuredBuffer<LightInformation> _ColoredShadowLightInformation;

void GetShadowMapValues(float2 uv, out float shadowID, out float blur, out float depth, out float2 shadowUVPos, out float shadowUVSize)
{
    uint2 input = _ColoredShadowMap0.Load(int3(uv.x * _CustomShadowAtlasWidth, uv.y * _CustomShadowAtlasHeight,0));
    // uint2 input = _ColoredShadowMap0.Sample(point_clamp_sampler, uv);
    depth = _DepthShadowMap.Sample(point_clamp_sampler, uv);
    
    UnpackCustomShadowValues_float(input, shadowID, blur, shadowUVPos.x, shadowUVPos.y, shadowUVSize);
}

void SampleColoredShadows_float(float3 worldPos, float2 uvOffset, float shadowUVMultiplier, bool relativeUVSize, out float shadowID, out float2 shadowUV, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask, out float4 customValues1, out float4 customValues2, out float4 customValues3)
{
    lightPos = float3(-999999999, -999999999, -999999999);
    fallOffRange = 0;
    float lowestDist = 99999999;
    shadowUV = float2(0, 0);
    finalUV = float2(0, 0);
    float highestMask = 0;
    mask = 0;
    customValues1 = float4(0, 0, 0, 0);
    customValues2 = float4(0, 0, 0, 0);
    customValues3 = float4(0, 0, 0, 0);

    for (int i = 0; i < _CurrentAmountCustomLights; ++i)
    {
        LightInformation lightInformation = _ColoredShadowLightInformation[i];
        float tempMask = 0;
        float dist = distance(worldPos, lightInformation.lightPos) / lightInformation.fallOffRange;

        if (lightInformation.lightMode == 0 || lightInformation.lightMode == 1) //Directional and Spot
        {
            float2 lightUv = GetLightUV(lightInformation, worldPos);
            float2 shadowAtlasMappedUV = GetLocalShadowAtlasUV(lightUv + uvOffset, lightInformation);

            float shadowIDTemp, blur, depth, shadowUVSize;
            float2 shadowUVPos;
            GetShadowMapValues(shadowAtlasMappedUV, shadowIDTemp, blur, depth, shadowUVPos, shadowUVSize);
            tempMask = blur;
            
            float3 newWorldPos = DepthToWorldPositionViewProj(lightInformation.invLightMatrix, lightUv, lightInformation.nearPlane, lightInformation.farPlane, depth);

            bool firstObjectHit = distance(newWorldPos, lightInformation.lightPos) + 0.1 > distance(worldPos, lightInformation.lightPos) || lightInformation.passthroughShadows == 0;
            if (tempMask > highestMask && dist < lowestDist && dist < 1 && CheckIsInBounds(lightInformation, shadowAtlasMappedUV) && firstObjectHit)
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, shadowUVSize, shadowUVPos, lightUv);
            
                fallOffRange = 1 - dist;
                highestMask = tempMask;
                mask = saturate(tempMask);
                finalUV = lightUv;
                lowestDist = dist;
                shadowID = shadowIDTemp + lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
            }
        }
        else if (lightInformation.lightMode == 2) // Point
        {
            float3 dir = normalize(lightInformation.lightPos - worldPos);
            int faceIndex = 0;
            float2 lightUv;
            GetCubemapUV(dir, lightUv, faceIndex);
            lightUv.x = 1 - lightUv.x;
            lightUv += uvOffset;
            float2 minCorner = float2(lightInformation.shadowAtlasPosX * 2, lightInformation.shadowAtlasPosY * 2);
            minCorner.x += lightInformation.textureSizeX * (faceIndex % 3);
            minCorner.y += lightInformation.textureSizeY * floor(faceIndex / 3);
            float2 maxCorner = float2(minCorner.x + lightInformation.textureSizeX, minCorner.y + lightInformation.textureSizeY);
            float2 cubemapUV = float2(remap(lightUv.x, 0, 3, minCorner.x, maxCorner.x), remap(lightUv.y, 0, 2, minCorner.y, maxCorner.y));

            float shadowIDTemp, blur, depth, shadowUVSize;
            float2 shadowUVPos;
            GetShadowMapValues(cubemapUV, shadowIDTemp, blur, depth, shadowUVPos, shadowUVSize);
            tempMask = blur;

            float3 newWorldPos = DepthToWorldPositionViewProj(lightUv, depth, lightInformation.nearPlane, lightInformation.farPlane, lightInformation.lightPos, faceIndex);

            bool firstObjectHit = distance(newWorldPos, lightInformation.lightPos) + 0.1 > distance(worldPos, lightInformation.lightPos) || lightInformation.passthroughShadows == 0;
            if (tempMask > highestMask && dist < lowestDist && dist < 1 && CheckIsInBounds(lightInformation, cubemapUV) && firstObjectHit)
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, shadowUVSize, shadowUVPos, lightUv);
                
                fallOffRange = 1 - dist;
                highestMask = tempMask * fallOffRange;
                mask = tempMask;
                finalUV = cubemapUV;
                lowestDist = dist;
                shadowID = shadowIDTemp + lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
            }
        }
    }
}

void SampleColoredShadows_half(float3 worldPos, float2 uvOffset, out float shadowID, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask)
{
    shadowID = 0;
    lightPos = float3(-999999999, -999999999, -999999999);
    fallOffRange = 0;
    finalUV = float2(0, 0);
    mask = 0;
}

#endif 