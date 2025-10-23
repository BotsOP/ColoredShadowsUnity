#ifndef SAMPLE_CUSTOM_SHADOW_INCLUDED
#define SAMPLE_CUSTOM_SHADOW_INCLUDED

#include "CustomShadowsLibrary.hlsl"

int _CustomShadowAtlasWidth;
int _CustomShadowAtlasHeight;
SamplerState trilinear_clamp_sampler;
SamplerState point_clamp_sampler;
Texture2D _ColoredShadowMap0;

int _CurrentAmountCustomLights;
StructuredBuffer<LightInformation> _ColoredShadowLightInformation;

float4x2 GetSample(float2 uv)
{
    float4 packedOutput = _ColoredShadowMap0.Sample(point_clamp_sampler, uv);
    float4x2 output;
    float2 unpackedR = UnpackFloatTo2Half_float(packedOutput.r);
    output[0][0] = unpackedR.r;
    output[1][0] = unpackedR.g;
    float2 unpackedG = UnpackFloatTo2Half_float(packedOutput.g);
    output[2][0] = unpackedG.r;
    output[3][0] = unpackedG.g;
    float2 unpackedB = UnpackFloatTo2Half_float(packedOutput.b);
    output[0][1] = unpackedB.r;
    output[1][1] = unpackedB.g;
    float2 unpackedA = UnpackFloatTo2Half_float(packedOutput.a);
    output[2][1] = unpackedA.r;
    output[3][1] = unpackedA.g;
    return output;
}

float GetMask(float2 uv, float2 testUV, LightInformation lightInformation)
{
    float2 texelSize = float2(1, 1) / int2(lightInformation.textureSizeX, lightInformation.textureSizeY);
    
    float2 subPixelOffset = ((frac(uv * int2(lightInformation.textureSizeX, lightInformation.textureSizeY)) - 0.5) * -1) / int2(lightInformation.textureSizeX, lightInformation.textureSizeY);
    float2 centerUV = uv;
    float2 bottomLeft = uv + subPixelOffset - texelSize;
    float2 topRight = uv + subPixelOffset + texelSize;
    float2 localUV = float2(remap(uv.x, bottomLeft.x, topRight.x, 0, 1), remap(uv.y, bottomLeft.y, topRight.y, 0, 1));

    float midCenterSample = ceil(saturate(GetSample(centerUV)[0][0]));
    return midCenterSample;
    if (testUV.x < texelSize.x * 3.0 || testUV.y < texelSize.x * 3.0 || testUV.x > 1 - texelSize.x * 3.0 || testUV.y > 1 - texelSize.x * 3.0)
    {
        return midCenterSample;
    }
    
    float topLeftSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(-texelSize.x, texelSize.y), lightInformation).r));
    float topCenterSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(0, texelSize.y), lightInformation).r));
    float topRightSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(texelSize.x, texelSize.y), lightInformation).r));
    float midLeftSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(-texelSize.x, 0), lightInformation).r));
    float midRightSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(texelSize.x, 0), lightInformation).r));
    float bottomLeftSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(-texelSize.x, -texelSize.y), lightInformation).r));
    float bottomCenterSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(0, -texelSize.y), lightInformation).r));
    float bottomRightSample = ceil(saturate(GetLocalShadowAtlasUV(centerUV + float2(texelSize.x, -texelSize.y), lightInformation).r));

    float mask = NinePointBlend(topLeftSample, topCenterSample, topRightSample, midLeftSample, midCenterSample, midRightSample, bottomLeftSample, bottomCenterSample, bottomRightSample, localUV);

    return mask;
}

void SampleColoredShadows_float(float3 worldPos, float2 uvOffset, float shadowUVMultiplier, bool relativeUVSize, out float4 output, out float2 shadowUV, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask, out float4 customValues1, out float4 customValues2, out float4 customValues3)
{
    output = float4(0, 0, 0, 0);
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
        float2 uv = float2(0, 0);
        float4 tempOutput;
        float2 lightUv;
        float tempMask = 0;
        float dist = distance(worldPos, lightInformation.lightPos) / lightInformation.fallOffRange;
        float2 shadowAtlasMappedUV;
        float3 newWorldPos;

        switch (lightInformation.lightMode)
        {
        case 0: // Directional
        case 1: // Spot
            lightUv = GetLightUV(lightInformation, worldPos);
            shadowAtlasMappedUV = GetLocalShadowAtlasUV(lightUv + uvOffset, lightInformation);
        
            tempOutput = _ColoredShadowMap0.Sample(point_clamp_sampler, shadowAtlasMappedUV);
            tempOutput.r = UnpackFloatTo2Half_float(tempOutput.r).r;
            tempMask = lightInformation.blurredEdges == 1 ? _ColoredShadowMap0.Sample(trilinear_clamp_sampler, shadowAtlasMappedUV).a : tempOutput.r;
            
            newWorldPos = DepthToWorldPositionViewProj(lightInformation.invLightMatrix, shadowAtlasMappedUV, tempOutput.b);

            if (tempMask > highestMask && dist <= lowestDist && dist < 1 && CheckIsInBounds(lightInformation, shadowAtlasMappedUV) && distance(newWorldPos, lightInformation.lightPos) + 0.1 > distance(worldPos, lightInformation.lightPos))
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, UnpackFloatTo2Half_float(tempOutput.r).g, UnpackFloatTo2Half_float(tempOutput.g), lightUv);
            
                tempMask = pow(tempMask, 4);
                tempMask = step(0.5, pow(tempMask, 1));
                fallOffRange = 1 - dist;
                highestMask = tempMask;
                mask = tempMask;
                finalUV = lightUv;
                lowestDist = dist;
                output = tempOutput;
                output.r += lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
            }
            break;
        case 2: //Point
            float3 dir = normalize(lightInformation.lightPos - worldPos);
            int faceIndex = 0;
            GetCubemapUV(dir, uv, faceIndex);
            uv.x = 1 - uv.x;
            uv += uvOffset;
            float2 minCorner = float2(lightInformation.shadowAtlasPosX * 2, lightInformation.shadowAtlasPosY * 2);
            minCorner.x += lightInformation.textureSizeX * (faceIndex % 3);
            minCorner.y += lightInformation.textureSizeY * floor(faceIndex / 3);
            float2 maxCorner = float2(minCorner.x + lightInformation.textureSizeX, minCorner.y + lightInformation.textureSizeY);
            float2 cubemapUV = float2(remap(uv.x, 0, 3, minCorner.x, maxCorner.x), remap(uv.y, 0, 2, minCorner.y, maxCorner.y));

            tempOutput = _ColoredShadowMap0.Sample(point_clamp_sampler, cubemapUV);
            tempOutput.r = UnpackFloatTo2Half_float(tempOutput.r).r;
            tempMask = GetMask(cubemapUV, uv, lightInformation);

            if (tempMask > highestMask && dist < lowestDist && dist < 1 && CheckIsInBounds(lightInformation, cubemapUV))
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, UnpackFloatTo2Half_float(tempOutput.r).g, UnpackFloatTo2Half_float(tempOutput.g), uv);
                // shadowUV = UnpackFloatTo2Half_float(tempOutput.g);
                shadowUV = uv;
                
                fallOffRange = 1 - dist;
                highestMask = tempMask * fallOffRange;
                mask = tempMask;
                finalUV = cubemapUV;
                lowestDist = dist;
                output = tempOutput;
                output.r += lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
            }
            break;
        default: break;
        }
    }
}

void SampleColoredShadows_half(float3 worldPos, float2 uvOffset, out float4 output, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask)
{
    output = float4(0, 0, 0, 0);
    lightPos = float3(-999999999, -999999999, -999999999);
    fallOffRange = 0;
    finalUV = float2(0, 0);
    mask = 0;
}

#endif 