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

SamplerState trilinear_clamp_sampler;
SamplerState point_clamp_sampler;
Texture2D _ColoredShadowMap0;
Texture2D _ColoredShadowMap1;
Texture2D _ColoredShadowMap2;
Texture2D _ColoredShadowMap3;
Texture2D _ColoredShadowMap4;
Texture2D _ColoredShadowMap5;
Texture2D _ColoredShadowMap6;
Texture2D _ColoredShadowMap7;
Texture2D _ColoredShadowMap8;
Texture2D _ColoredShadowMap9;

float4 SampleColoredShadowMap(float2 uv, int mapIndex, int textureSizeX, int textureSizeY, out float mask, int cubemapFaceIndex = -1)
{
    float4 output;
    mask = 0;
    float2 texelOffsetX = float2(1.0 / textureSizeX, 0);
    float2 texelOffsetY = float2(0, 1.0 / textureSizeY);

    float2 uvXPlus = uv + texelOffsetX;
    float2 uvXNegative = uv - texelOffsetX;
    
    float2 uvYPlus = uv + texelOffsetY;
    float2 uvYNegative = uv - texelOffsetY;

    float4 m1;
    float4 m2;
    float4 m3;
    float4 m4;
    float4 m5;

    float l1;
    float l2;
    float l3;
    float l4;
    float l5;
    
    switch (mapIndex)
    {
    case 0:
        m1 = (_ColoredShadowMap0.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap0.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap0.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap0.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap0.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap0.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap0.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap0.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap0.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap0.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 1:
        m1 = (_ColoredShadowMap1.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap1.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap1.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap1.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap1.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap1.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap1.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap1.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap1.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap1.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 2:
        m1 = (_ColoredShadowMap2.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap2.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap2.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap2.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap2.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap2.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap2.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap2.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap2.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap2.Sample(trilinear_clamp_sampler, uv).r);

        if ((uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) || uv.x > (1/6.0 * (cubemapFaceIndex + 1)) - (1.0 / textureSizeX * 2)) && l5.r > 0)
        {
            // l2 = 1 / output.r;
            // mask = l5 * 5;
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 3:
        m1 = (_ColoredShadowMap3.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap3.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap3.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap3.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap3.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap3.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap3.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap3.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap3.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap3.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 4:
        m1 = (_ColoredShadowMap4.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap4.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap4.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap4.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap4.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap4.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap4.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap4.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap4.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap4.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 5:
        m1 = (_ColoredShadowMap5.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap5.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap5.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap5.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap5.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap5.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap5.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap5.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap5.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap5.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 6:
        m1 = (_ColoredShadowMap6.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap6.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap6.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap6.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap6.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap6.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap6.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap6.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap6.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap6.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 7:
        m1 = (_ColoredShadowMap7.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap7.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap7.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap7.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap7.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap7.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap7.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap7.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap7.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap7.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 8:
        m1 = (_ColoredShadowMap8.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap8.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap8.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap8.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap8.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap8.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap8.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap8.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap8.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap8.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    case 9:
        m1 = (_ColoredShadowMap9.Sample(point_clamp_sampler, uvXPlus));
        m2 = (_ColoredShadowMap9.Sample(point_clamp_sampler, uvXNegative));
        m3 = (_ColoredShadowMap9.Sample(point_clamp_sampler, uvYPlus));
        m4 = (_ColoredShadowMap9.Sample(point_clamp_sampler, uvYNegative));
        m5 = (_ColoredShadowMap9.Sample(point_clamp_sampler, uv));
    
        output = max(m1, max(m2, max(m3, max(m4, m5))));

        l1 = (_ColoredShadowMap9.Sample(trilinear_clamp_sampler, uvXPlus).r);
        l2 = (_ColoredShadowMap9.Sample(trilinear_clamp_sampler, uvXNegative).r);
        l3 = (_ColoredShadowMap9.Sample(trilinear_clamp_sampler, uvYPlus).r);
        l4 = (_ColoredShadowMap9.Sample(trilinear_clamp_sampler, uvYNegative).r);
        l5 = (_ColoredShadowMap9.Sample(trilinear_clamp_sampler, uv).r);

        if (uv.x < (1/6.0 * (cubemapFaceIndex)) + (1.0 / textureSizeX * 2) && l5 > 0)
        {
            mask = (max(m4.r, m3.r) / output.r) * 5;
            break;
        }
    
        mask += l1 / output.r;
        mask += l2 / output.r;
        mask += l3 / output.r;
        mask += l4 / output.r;
        mask += l5 / output.r;
        break;
    default:
        mask = 0;
        output = float4(0, 0, 0, 0);
        break;
    }

    mask /= 5.0;
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
    float3 cameraPos;
    int textureSizeX;
    int textureSizeY;
    int lightIDMultiplier;
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
void SampleColoredShadows_float(float3 worldPos, float2 uvOffset, out float4 output, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask)
{
    output = float4(0, 0, 0, 0);
    lightPos = float3(-999999999, -999999999, -999999999);
    fallOffRange = 0;
    float lowestDist = 99999999;
    finalUV = float2(0, 0);
    float highestMask = 0;

    for (int i = 0; i < CurrentAmountCustomLights; ++i)
    {
        LightInformation lightInformation = ColoredShadowLightInformation[i];
        float2 uv = float2(0, 0);
        float4 tempOutput;
        float4 lightSpace;
        float3 lightUv;
        float tempMask;
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
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation.index, textureSizeX, textureSizeY, tempMask);
            if (tempOutput.x > 0 && lightUv.x > 1.0 / textureSizeX && lightUv.x < (textureSizeX - 1) / textureSizeX && lightUv.y > 1.0 / textureSizeY && lightUv.y < (textureSizeY - 1) / textureSizeY)
            {
                highestMask = tempMask;
                mask = saturate(pow(tempMask, 1));
                finalUV = lightUv + uvOffset;
                lowestDist = dist;
                output = tempOutput;
                output.r *= lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
                fallOffRange = lightInformation.fallOffRange;
            }
            break;
        case 1: // Spot
            lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y, worldPos.z, 1));
            lightUv = lightSpace.rgb / lightSpace.a;
            lightUv *= 0.5;
            lightUv += 0.5;
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation.index, textureSizeX, textureSizeY, tempMask);
            if (tempOutput.x > 0 && lightUv.x > 1.0 / textureSizeX && lightUv.x < (textureSizeX - 1) / textureSizeX &&
                lightUv.y > 1.0 / textureSizeY && lightUv.y < (textureSizeY - 1) / textureSizeY && dist < lowestDist && dist < 1)
            {
                highestMask = tempMask;
                mask = saturate(pow(tempMask, 1));
                finalUV = lightUv + uvOffset;
                lowestDist = dist;
                output = tempOutput;
                output.r *= lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
                fallOffRange = lightInformation.fallOffRange;
            }
            break;
        case 2: //Point
            float3 dir = normalize(lightInformation.lightPos - worldPos);
            int faceIndex;
            SampleCustomCubeMap(dir, uv, faceIndex);
            float2 cubemapUV = float2((uv.x / 6.0) + ((1.0/6.0) * faceIndex), uv.y);
            cubemapUV += uvOffset * float2(1/6.0, 1);
            finalUV = cubemapUV;
            textureSizeX *= 6;
            tempOutput = SampleColoredShadowMap(cubemapUV, lightInformation.index, textureSizeX, textureSizeY, tempMask, faceIndex);
            
            if (tempMask > highestMask && tempMask > 0)
            {
                highestMask = tempMask;
                tempMask = saturate(pow(tempMask, 4));
                mask = tempMask;

                if (dist < lowestDist && dist < 1)
                {
                    finalUV = cubemapUV;
                    lowestDist = dist;
                    output = tempOutput;
                    output.r *= lightInformation.lightIDMultiplier;
                    lightPos = lightInformation.lightPos;
                    fallOffRange = lightInformation.fallOffRange;
                }
            }
            break;
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