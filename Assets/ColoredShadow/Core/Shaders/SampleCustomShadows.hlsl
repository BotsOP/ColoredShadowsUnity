#ifndef SAMPLE_CUSTOM_CUBEMAP_INCLUDED
#define SAMPLE_CUSTOM_CUBEMAP_INCLUDED

struct LightInformation
{
    int index;
    int lightMode;
    float4x4 lightMatrix;
    float3 lightPos;
    float fallOffRange;
    float farPlane;
    float3 cameraPos;
    int textureSizeX;
    int textureSizeY;
    int lightIDMultiplier;
    int shadowAtlasPosX;
    int shadowAtlasPosY;
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
}

float BilinearSampleCompact(float bottomLeft, float bottomRight, float topLeft, float topRight, float2 uv)
{
    return lerp(
        lerp(bottomLeft, bottomRight, uv.x),  // Bottom edge interpolation
        lerp(topLeft, topRight, uv.x),       // Top edge interpolation
        uv.y                                 // Vertical interpolation
    );
}

int _ShadowAtlasWidth;
int _ShadowAtlasHeight;
SamplerState trilinear_clamp_sampler;
SamplerState point_clamp_sampler;
Texture2D _ColoredShadowMap0;

float4 SampleColoredShadowMap(float2 uv, LightInformation lightInformation)
{
    int shadowAtlasPosX = lightInformation.shadowAtlasPosX;
    int shadowAtlasPosY = lightInformation.shadowAtlasPosY;
    int textureWidth = lightInformation.textureSizeX;
    int textureHeight = lightInformation.textureSizeY;
    uv *= float2(textureWidth / (float)_ShadowAtlasWidth, textureHeight / (float)_ShadowAtlasHeight);;
    uv += float2(shadowAtlasPosX / (float)_ShadowAtlasWidth, shadowAtlasPosY / (float)_ShadowAtlasHeight);
    return _ColoredShadowMap0.Sample(point_clamp_sampler, uv);
}

float NinePointBlend(
    float topLeft,    float topCenter,    float topRight,
    float midLeft,    float center,       float midRight,
    float bottomLeft, float bottomCenter, float bottomRight,
    float2 localUV)
{
    // localUV should be from 0-1 within the 3x3 grid cell
    // where (0,0) is bottom-left and (1,1) is top-right
    
    // Determine which quadrant we're in and get interpolation factors
    float2 gridPos = localUV * 2.0; // Scale to 0-2 range
    
    if (gridPos.x <= 1.0 && gridPos.y <= 1.0)
    {
        // Bottom-left quadrant
        float2 t = gridPos; // 0-1 within this quadrant
        return BilinearSampleCompact(bottomLeft, bottomCenter, midLeft, center, t);
    }
    else if (gridPos.x > 1.0 && gridPos.y <= 1.0)
    {
        // Bottom-right quadrant
        float2 t = float2(gridPos.x - 1.0, gridPos.y); // 0-1 within this quadrant
        return BilinearSampleCompact(bottomCenter, bottomRight, center, midRight, t);
    }
    else if (gridPos.x <= 1.0 && gridPos.y > 1.0)
    {
        // Top-left quadrant
        float2 t = float2(gridPos.x, gridPos.y - 1.0); // 0-1 within this quadrant
        return BilinearSampleCompact(midLeft, center, topLeft, topCenter, t);
    }
    else
    {
        // Top-right quadrant
        float2 t = float2(gridPos.x - 1.0, gridPos.y - 1.0); // 0-1 within this quadrant
        return BilinearSampleCompact(center, midRight, topCenter, topRight, t);
    }
}

float2 ConstrainToCardinalDirectionsFast(float2 direction)
{
    // Normalize the input direction
    direction = normalize(direction);
    
    // Get absolute values for octant determination
    float2 abs_dir = abs(direction);
    
    // Determine which octant we're in based on which component is larger
    // and the signs of the components
    
    if (abs_dir.x > abs_dir.y)
    {
        // Horizontal dominant
        if (abs_dir.x > abs_dir.y * 2.414) // tan(67.5°) ≈ 2.414
        {
            // Pure horizontal: East or West
            return float2(sign(direction.x), 0.0);
        }
        else
        {
            // Diagonal: Northeast, Southeast, Northwest, Southwest
            return normalize(float2(sign(direction.x), sign(direction.y)));
        }
    }
    else
    {
        // Vertical dominant
        if (abs_dir.y > abs_dir.x * 2.414) // tan(67.5°) ≈ 2.414
        {
            // Pure vertical: North or South
            return float2(0.0, sign(direction.y));
        }
        else
        {
            // Diagonal: Northeast, Southeast, Northwest, Southwest
            return normalize(float2(sign(direction.x), sign(direction.y)));
        }
    }
}

float GetMask(float2 uv, float2 testUV, LightInformation lightInformation)
{
    float2 texelSize = float2(1, 1) / int2(lightInformation.textureSizeX, lightInformation.textureSizeY);
    
    float2 subPixelOffset = ((frac(uv * int2(lightInformation.textureSizeX, lightInformation.textureSizeY)) - 0.5) * -1) / int2(lightInformation.textureSizeX, lightInformation.textureSizeY);
    float2 centerUV = uv;
    float2 bottomLeft = uv + subPixelOffset - texelSize;
    float2 topRight = uv + subPixelOffset + texelSize;
    float2 localUV = float2(remap(uv.x, bottomLeft.x, topRight.x, 0, 1), remap(uv.y, bottomLeft.y, topRight.y, 0, 1));

    float midCenterSample = ceil(saturate(SampleColoredShadowMap(centerUV, lightInformation).r));
    if (testUV.x < texelSize.x * 3.0 || testUV.y < texelSize.x * 3.0 || testUV.x > 1 - texelSize.x * 3.0 || testUV.y > 1 - texelSize.x * 3.0)
    {
        return midCenterSample;
    }
    
    float topLeftSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(-texelSize.x, texelSize.y), lightInformation).r));
    float topCenterSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(0, texelSize.y), lightInformation).r));
    float topRightSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(texelSize.x, texelSize.y), lightInformation).r));
    float midLeftSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(-texelSize.x, 0), lightInformation).r));
    float midRightSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(texelSize.x, 0), lightInformation).r));
    float bottomLeftSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(-texelSize.x, -texelSize.y), lightInformation).r));
    float bottomCenterSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(0, -texelSize.y), lightInformation).r));
    float bottomRightSample = ceil(saturate(SampleColoredShadowMap(centerUV + float2(texelSize.x, -texelSize.y), lightInformation).r));

    float mask = NinePointBlend(topLeftSample, topCenterSample, topRightSample, midLeftSample, midCenterSample, midRightSample, bottomLeftSample, bottomCenterSample, bottomRightSample, localUV);

    return mask;
}

int _CurrentAmountCustomLights;
StructuredBuffer<LightInformation> _ColoredShadowLightInformation;
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
        float4 lightSpace;
        float3 lightUv;
        float tempMask = 0;
        float textureSizeX = lightInformation.textureSizeX;
        float textureSizeY = lightInformation.textureSizeY;
        float dist = distance(worldPos, lightInformation.lightPos) / lightInformation.fallOffRange;

        switch (lightInformation.lightMode)
        {
        case 0: // Directional
            lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y, worldPos.z, 1));
            lightUv = lightSpace.rgb / lightSpace.a;
            lightUv *= 0.5;
            lightUv += 0.5;
            lightUv.xy += uvOffset;
        
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation);
            tempMask = GetMask(lightUv.rg, lightUv.rg, lightInformation);

            if (tempMask > highestMask && lightUv.x > 1.0 / textureSizeX && lightUv.x < (textureSizeX - 1) / textureSizeX &&
                lightUv.y > 1.0 / textureSizeY && lightUv.y < (textureSizeY - 1) / textureSizeY && dist <= lowestDist && dist < 1)
            {
                float shadowSize = relativeUVSize ? tempOutput.a : 1;
                shadowSize *= shadowUVMultiplier;
                float2 shadowUVX = float2(tempOutput.g - shadowSize, tempOutput.g + shadowSize);
                float2 shadowUVY = float2(tempOutput.b - shadowSize, tempOutput.b + shadowSize);
                shadowUV = float2(remap(lightUv.x, shadowUVX.x, shadowUVX.y, 0, 1), remap(lightUv.y, shadowUVY.x, shadowUVY.y, 0, 1));
                    
                fallOffRange = 1 - dist;
                highestMask = tempMask;
                mask = tempMask;
                // mask = pow(tempMask * ceil(saturate(tempOutput.r)), 4);
                finalUV = lightUv;
                lowestDist = dist;
                output = tempOutput;
                output.r += lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
            }
            break;
        case 1: // Spot
            lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y, worldPos.z, 1));
            lightUv = lightSpace.rgb / lightSpace.a;
            lightUv *= 0.5;
            lightUv += 0.5;
            lightUv.xy += uvOffset;

            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation);
            tempMask = GetMask(lightUv.rg, lightUv.rg, lightInformation);

            if (tempMask > highestMask && lightUv.x > 1.0 / textureSizeX && lightUv.x < (textureSizeX - 1) / textureSizeX && lightUv.y > 1.0 / textureSizeY && lightUv.y < (textureSizeY - 1) / textureSizeY && dist <= lowestDist && dist < 1)
            {
                float shadowSize = relativeUVSize ? tempOutput.a : 1;
                shadowSize *= shadowUVMultiplier;
                float2 shadowUVX = float2(tempOutput.g - shadowSize, tempOutput.g + shadowSize);
                float2 shadowUVY = float2(tempOutput.b - shadowSize, tempOutput.b + shadowSize);
                shadowUV = float2(remap(lightUv.x, shadowUVX.x, shadowUVX.y, 0, 1), remap(lightUv.y, shadowUVY.x, shadowUVY.y, 0, 1));

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
            int faceIndex;
            GetCubemapUV(dir, uv, faceIndex);
            uv += uvOffset;
            
            float2 cubemapUV = float2((uv.x / 6.0) + ((1.0/6.0) * faceIndex), uv.y);

            tempOutput = SampleColoredShadowMap(cubemapUV, lightInformation);
            tempMask = GetMask(cubemapUV, uv, lightInformation);

            if (tempMask > highestMask && dist < lowestDist && dist < 1)
            {
                float shadowSize = relativeUVSize ? tempOutput.a : 1;
                shadowSize *= shadowUVMultiplier;
                float2 shadowUVX = float2(tempOutput.g - shadowSize, tempOutput.g + shadowSize);
                float2 shadowUVY = float2(tempOutput.b - shadowSize, tempOutput.b + shadowSize);
                shadowUV = float2(remap(uv.x, shadowUVX.x, shadowUVX.y, 0, 1), remap(uv.y, shadowUVY.x, shadowUVY.y, 0, 1));
                    
                fallOffRange = 1 - dist;
                highestMask = tempMask * fallOffRange;
                mask = tempMask;
                finalUV = uv;
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