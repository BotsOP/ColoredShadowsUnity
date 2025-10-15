#ifndef SAMPLE_CUSTOM_SHADOW_INCLUDED
#define SAMPLE_CUSTOM_SHADOW_INCLUDED

struct LightInformation
{
    int index;
    int lightMode;
    float4x4 lightMatrix;
    float4x4 invLightMatrix;
    float3 lightPos;
    float fallOffRange;
    float farPlane;
    float3 cameraPos;
    float textureSizeX;
    float textureSizeY;
    int lightIDMultiplier;
    float shadowAtlasPosX;
    float shadowAtlasPosY;
    int passthroughShadows;
    int blurredEdges;
    float customValue0;
    float customValue1;
    float customValue2;
    float customValue3;
    float customValue4;
    float customValue5;
    float customValue6;
    float customValue7;
    float customValue8;
    float customValue9;
    float customValue10;
    float customValue11;
};

float Pack2HalfToFloat_float(float2 v)
{
    uint low  = f32tof16(v.x);     // float → half bits
    uint high = f32tof16(v.y);
    uint packed = (high << 16) | (low & 0xFFFF);
    return asfloat(packed);       // reinterpret bits as float
}

float2 UnpackFloatTo2Half_float(float packed)
{
    uint bits = asuint(packed);
    float x = f16tof32( bits & 0xFFFF );
    float y = f16tof32( bits >> 16 );
    return float2(x, y);
}

int _CustomShadowAtlasWidth;
int _CustomShadowAtlasHeight;
SamplerState trilinear_clamp_sampler;
SamplerState point_clamp_sampler;
Texture2D _ColoredShadowMap0;

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

float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
{
    float a = newMin + (value - oldMin) * (newMax - newMin);
    float b = (oldMax - oldMin);
    return a / b;
}

void GetCubemapUV(float3 direction, out float2 uv, out int faceIndex)
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
    if (closestDir == forward) // POSITIVE_Z
    {
        faceIndex = 0;
    }
    else if (closestDir == back) // NEGATIVE_Z
    {
        faceIndex = 2;
    }
    else if (closestDir == left) // POSITIVE_X
    {
        faceIndex = 3;
    }
    else if (closestDir == right) // NEGATIVE_X 
    {
        faceIndex = 1;
    }
    else if (closestDir == down) // NEGATIVE_Y
    {
        faceIndex = 4;
    }
    else if (closestDir == up) // POSITIVE_Y
    {
        faceIndex = 5;
    }
    
    switch(faceIndex)
    {
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
}

float BilinearSampleCompact(float bottomLeft, float bottomRight, float topLeft, float topRight, float2 uv)
{
    return lerp(
        lerp(bottomLeft, bottomRight, uv.x),  // Bottom edge interpolation
        lerp(topLeft, topRight, uv.x),       // Top edge interpolation
        uv.y                                 // Vertical interpolation
    );
}

float2 GetLocalShadowAtlasUV(float2 uv, LightInformation lightInformation)
{
    int shadowAtlasPosX = lightInformation.shadowAtlasPosX;
    int shadowAtlasPosY = lightInformation.shadowAtlasPosY;
    int textureWidth = lightInformation.textureSizeX;
    int textureHeight = lightInformation.textureSizeY;
    uv *= float2(textureWidth / (float)_CustomShadowAtlasWidth, textureHeight / (float)_CustomShadowAtlasHeight);
    uv += float2(shadowAtlasPosX / (float)_CustomShadowAtlasWidth, shadowAtlasPosY / (float)_CustomShadowAtlasHeight);
    return uv;
}

float NinePointBlend(
    float topLeft,    float topCenter,    float topRight,
    float midLeft,    float center,       float midRight,
    float bottomLeft, float bottomCenter, float bottomRight,
    float2 localUV)
{
    float2 gridPos = localUV * 2.0; // Scale to 0-2 range
    
    if (gridPos.x <= 1.0 && gridPos.y <= 1.0)
    {
        float2 t = gridPos; // 0-1 within this quadrant
        return BilinearSampleCompact(bottomLeft, bottomCenter, midLeft, center, t);
    }
    if (gridPos.x > 1.0 && gridPos.y <= 1.0)
    {
        float2 t = float2(gridPos.x - 1.0, gridPos.y); // 0-1 within this quadrant
        return BilinearSampleCompact(bottomCenter, bottomRight, center, midRight, t);
    }
    if (gridPos.x <= 1.0 && gridPos.y > 1.0)
    {
        float2 t = float2(gridPos.x, gridPos.y - 1.0); // 0-1 within this quadrant
        return BilinearSampleCompact(midLeft, center, topLeft, topCenter, t);
    }
    float2 t = float2(gridPos.x - 1.0, gridPos.y - 1.0); // 0-1 within this quadrant
    return BilinearSampleCompact(center, midRight, topCenter, topRight, t);
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

bool CheckIsInBounds(LightInformation lightInformation, float2 lightUv)
{
    return lightUv.x < (lightInformation.shadowAtlasPosX + lightInformation.textureSizeX) / (float)_CustomShadowAtlasWidth && lightUv.x >  lightInformation.shadowAtlasPosX / (float)_CustomShadowAtlasWidth &&
                lightUv.y < (lightInformation.shadowAtlasPosY + lightInformation.textureSizeY) / (float)_CustomShadowAtlasHeight && lightUv.y >  lightInformation.shadowAtlasPosY / (float)_CustomShadowAtlasHeight;
}

float4x4 InvertMatrix(float4x4 m)
{
    float4x4 inv;

    inv[0][0] =  m[1][1]*m[2][2]*m[3][3] - m[1][1]*m[2][3]*m[3][2] - m[2][1]*m[1][2]*m[3][3]
                + m[2][1]*m[1][3]*m[3][2] + m[3][1]*m[1][2]*m[2][3] - m[3][1]*m[1][3]*m[2][2];
    inv[0][1] = -m[0][1]*m[2][2]*m[3][3] + m[0][1]*m[2][3]*m[3][2] + m[2][1]*m[0][2]*m[3][3]
                - m[2][1]*m[0][3]*m[3][2] - m[3][1]*m[0][2]*m[2][3] + m[3][1]*m[0][3]*m[2][2];
    inv[0][2] =  m[0][1]*m[1][2]*m[3][3] - m[0][1]*m[1][3]*m[3][2] - m[1][1]*m[0][2]*m[3][3]
                + m[1][1]*m[0][3]*m[3][2] + m[3][1]*m[0][2]*m[1][3] - m[3][1]*m[0][3]*m[1][2];
    inv[0][3] = -m[0][1]*m[1][2]*m[2][3] + m[0][1]*m[1][3]*m[2][2] + m[1][1]*m[0][2]*m[2][3]
                - m[1][1]*m[0][3]*m[2][2] - m[2][1]*m[0][2]*m[1][3] + m[2][1]*m[0][3]*m[1][2];

    inv[1][0] = -m[1][0]*m[2][2]*m[3][3] + m[1][0]*m[2][3]*m[3][2] + m[2][0]*m[1][2]*m[3][3]
                - m[2][0]*m[1][3]*m[3][2] - m[3][0]*m[1][2]*m[2][3] + m[3][0]*m[1][3]*m[2][2];
    inv[1][1] =  m[0][0]*m[2][2]*m[3][3] - m[0][0]*m[2][3]*m[3][2] - m[2][0]*m[0][2]*m[3][3]
                + m[2][0]*m[0][3]*m[3][2] + m[3][0]*m[0][2]*m[2][3] - m[3][0]*m[0][3]*m[2][2];
    inv[1][2] = -m[0][0]*m[1][2]*m[3][3] + m[0][0]*m[1][3]*m[3][2] + m[1][0]*m[0][2]*m[3][3]
                - m[1][0]*m[0][3]*m[3][2] - m[3][0]*m[0][2]*m[1][3] + m[3][0]*m[0][3]*m[1][2];
    inv[1][3] =  m[0][0]*m[1][2]*m[2][3] - m[0][0]*m[1][3]*m[2][2] - m[1][0]*m[0][2]*m[2][3]
                + m[1][0]*m[0][3]*m[2][2] + m[2][0]*m[0][2]*m[1][3] - m[2][0]*m[0][3]*m[1][2];

    inv[2][0] =  m[1][0]*m[2][1]*m[3][3] - m[1][0]*m[2][3]*m[3][1] - m[2][0]*m[1][1]*m[3][3]
                + m[2][0]*m[1][3]*m[3][1] + m[3][0]*m[1][1]*m[2][3] - m[3][0]*m[1][3]*m[2][1];
    inv[2][1] = -m[0][0]*m[2][1]*m[3][3] + m[0][0]*m[2][3]*m[3][1] + m[2][0]*m[0][1]*m[3][3]
                - m[2][0]*m[0][3]*m[3][1] - m[3][0]*m[0][1]*m[2][3] + m[3][0]*m[0][3]*m[2][1];
    inv[2][2] =  m[0][0]*m[1][1]*m[3][3] - m[0][0]*m[1][3]*m[3][1] - m[1][0]*m[0][1]*m[3][3]
                + m[1][0]*m[0][3]*m[3][1] + m[3][0]*m[0][1]*m[1][3] - m[3][0]*m[0][3]*m[1][1];
    inv[2][3] = -m[0][0]*m[1][1]*m[2][3] + m[0][0]*m[1][3]*m[2][1] + m[1][0]*m[0][1]*m[2][3]
                - m[1][0]*m[0][3]*m[2][1] - m[2][0]*m[0][1]*m[1][3] + m[2][0]*m[0][3]*m[1][1];

    inv[3][0] = -m[1][0]*m[2][1]*m[3][2] + m[1][0]*m[2][2]*m[3][1] + m[2][0]*m[1][1]*m[3][2]
                - m[2][0]*m[1][2]*m[3][1] - m[3][0]*m[1][1]*m[2][2] + m[3][0]*m[1][2]*m[2][1];
    inv[3][1] =  m[0][0]*m[2][1]*m[3][2] - m[0][0]*m[2][2]*m[3][1] - m[2][0]*m[0][1]*m[3][2]
                + m[2][0]*m[0][2]*m[3][1] + m[3][0]*m[0][1]*m[2][2] - m[3][0]*m[0][2]*m[2][1];
    inv[3][2] = -m[0][0]*m[1][1]*m[3][2] + m[0][0]*m[1][2]*m[3][1] + m[1][0]*m[0][1]*m[3][2]
                - m[1][0]*m[0][2]*m[3][1] - m[3][0]*m[0][1]*m[1][2] + m[3][0]*m[0][2]*m[1][1];
    inv[3][3] =  m[0][0]*m[1][1]*m[2][2] - m[0][0]*m[1][2]*m[2][1] - m[1][0]*m[0][1]*m[2][2]
                + m[1][0]*m[0][2]*m[2][1] + m[2][0]*m[0][1]*m[1][2] - m[2][0]*m[0][2]*m[1][1];

    float det = m[0][0]*inv[0][0] + m[0][1]*inv[1][0] + m[0][2]*inv[2][0] + m[0][3]*inv[3][0];

    if (abs(det) < 1e-6)
        return float4x4(0, 0, 0, 0,
                        0, 0, 0, 0,
                        0, 0, 0, 0,
                        0, 0, 0, 0); // Or handle as error

    return inv / det;
}

float UnlinearizeDepth(float zLinear, float nearPlane, float farPlane)
{
    return (farPlane + nearPlane - (2.0 * nearPlane * farPlane) / zLinear) / (farPlane - nearPlane);
}

float3 DepthToWorldPositionViewProj(float4x4 projViewMatrix, float2 screenUV, float depth)
{
    float2 ndcXY = screenUV * 2.0 - 1;
    float worldZDistance = 0.1 + (depth * (100 - 0.1));
    float ndcZ = (worldZDistance - 0.1) * 2.0f / (100 - 0.1) - 1.0f;
    ndcZ *= -1;
    float4 ndcPos = float4(ndcXY.x, ndcXY.y, ndcZ, 1.0);
    float4 worldPos = mul(projViewMatrix, ndcPos);
    worldPos.xyz /= worldPos.w;
    
    return worldPos.xyz;
}

float2 GetLightUV(LightInformation lightInformation, float3 worldPos, float2 uvOffset)
{
    float4 lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y, worldPos.z, 1));
    float3 lightUv = lightSpace.rgb / lightSpace.a;
    lightUv *= 0.5;
    lightUv += 0.5;
    lightUv.xy += uvOffset;
    return GetLocalShadowAtlasUV(lightUv.rg, lightInformation).xy;
}


int _CurrentAmountCustomLights;
StructuredBuffer<LightInformation> _ColoredShadowLightInformation;

float2 GetLocalShadowUV(float shadowUVMultiplier, bool relativeUVSize, float4 tempOutput, float2 lightUv)
{
    float shadowSize = relativeUVSize ? tempOutput.a : 1;
    shadowSize *= shadowUVMultiplier;
    float2 shadowUVX = float2(tempOutput.g - shadowSize, tempOutput.g + shadowSize);
    float2 shadowUVY = float2(tempOutput.b - shadowSize, tempOutput.b + shadowSize);
    return float2(remap(lightUv.x, shadowUVX.x, shadowUVX.y, 0, 1), remap(lightUv.y, shadowUVY.x, shadowUVY.y, 0, 1));
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

        switch (lightInformation.lightMode)
        {
        case 0: // Directional
            lightUv = GetLightUV(lightInformation, worldPos, uvOffset);
        
            tempOutput = _ColoredShadowMap0.Sample(point_clamp_sampler, lightUv);
            tempOutput.r = UnpackFloatTo2Half_float(tempOutput.r).r;
            tempMask = lightInformation.blurredEdges == 1 ? _ColoredShadowMap0.Sample(trilinear_clamp_sampler, lightUv).a : tempOutput.r;
            
            float3 newWorldPos = DepthToWorldPositionViewProj(lightInformation.invLightMatrix, lightUv, tempOutput.b);

            if (tempMask > highestMask && dist <= lowestDist && dist < 1 && CheckIsInBounds(lightInformation, lightUv) && distance(newWorldPos, lightInformation.lightPos) + 0.1 > distance(worldPos, lightInformation.lightPos))
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, tempOutput, lightUv);
            
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
        case 1: // Spot
            lightUv = GetLightUV(lightInformation, worldPos, uvOffset);

            tempOutput = _ColoredShadowMap0.Sample(point_clamp_sampler, lightUv);
            tempOutput.r = UnpackFloatTo2Half_float(tempOutput.r).r;
            tempMask = _ColoredShadowMap0.Sample(trilinear_clamp_sampler, lightUv).a * ceil(saturate(tempOutput.r));

            if (tempMask > highestMask && dist <= lowestDist && dist < 1 && CheckIsInBounds(lightInformation, lightUv))
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, tempOutput, lightUv);

                tempMask = step(0.5, pow(tempMask, 4));
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
            float2 minCorner = float2(lightInformation.shadowAtlasPosX, lightInformation.shadowAtlasPosY);
            minCorner.x += (float)lightInformation.textureSizeX * (faceIndex % 3);
            minCorner.y += (float)lightInformation.textureSizeY * floor(faceIndex / 3);
            float2 maxCorner = float2(minCorner.x + (float)lightInformation.textureSizeX, minCorner.y + (float)lightInformation.textureSizeY);
            float2 cubemapUV = float2(remap(uv.x, 0, 3, minCorner.x, maxCorner.x), remap(uv.y, 0, 2, minCorner.y, maxCorner.y));
            if (i == 1)
            {
                finalUV = cubemapUV;
            }
            

            tempOutput = _ColoredShadowMap0.Sample(point_clamp_sampler, cubemapUV);
            tempOutput.r = UnpackFloatTo2Half_float(tempOutput.r).r;
            tempMask = GetMask(cubemapUV, uv, lightInformation);

            if (tempMask > highestMask && dist < lowestDist && dist < 1 && CheckIsInBounds(lightInformation, cubemapUV))
            {
                shadowUV = GetLocalShadowUV(shadowUVMultiplier, relativeUVSize, tempOutput, cubemapUV);
                
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