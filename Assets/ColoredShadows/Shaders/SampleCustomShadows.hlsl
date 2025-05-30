#ifndef SAMPLE_CUSTOM_CUBEMAP_INCLUDED
#define SAMPLE_CUSTOM_CUBEMAP_INCLUDED

float4x4 InverseMatrix(float4x4 m)
{
    float4x4 inv;

    inv[0][0] =  m[1][1]*m[2][2]*m[3][3] - m[1][1]*m[2][3]*m[3][2] - m[2][1]*m[1][2]*m[3][3] +
                 m[2][1]*m[1][3]*m[3][2] + m[3][1]*m[1][2]*m[2][3] - m[3][1]*m[1][3]*m[2][2];

    inv[0][1] = -m[0][1]*m[2][2]*m[3][3] + m[0][1]*m[2][3]*m[3][2] + m[2][1]*m[0][2]*m[3][3] -
                 m[2][1]*m[0][3]*m[3][2] - m[3][1]*m[0][2]*m[2][3] + m[3][1]*m[0][3]*m[2][2];

    inv[0][2] =  m[0][1]*m[1][2]*m[3][3] - m[0][1]*m[1][3]*m[3][2] - m[1][1]*m[0][2]*m[3][3] +
                 m[1][1]*m[0][3]*m[3][2] + m[3][1]*m[0][2]*m[1][3] - m[3][1]*m[0][3]*m[1][2];

    inv[0][3] = -m[0][1]*m[1][2]*m[2][3] + m[0][1]*m[1][3]*m[2][2] + m[1][1]*m[0][2]*m[2][3] -
                 m[1][1]*m[0][3]*m[2][2] - m[2][1]*m[0][2]*m[1][3] + m[2][1]*m[0][3]*m[1][2];

    inv[1][0] = -m[1][0]*m[2][2]*m[3][3] + m[1][0]*m[2][3]*m[3][2] + m[2][0]*m[1][2]*m[3][3] -
                 m[2][0]*m[1][3]*m[3][2] - m[3][0]*m[1][2]*m[2][3] + m[3][0]*m[1][3]*m[2][2];

    inv[1][1] =  m[0][0]*m[2][2]*m[3][3] - m[0][0]*m[2][3]*m[3][2] - m[2][0]*m[0][2]*m[3][3] +
                 m[2][0]*m[0][3]*m[3][2] + m[3][0]*m[0][2]*m[2][3] - m[3][0]*m[0][3]*m[2][2];

    inv[1][2] = -m[0][0]*m[1][2]*m[3][3] + m[0][0]*m[1][3]*m[3][2] + m[1][0]*m[0][2]*m[3][3] -
                 m[1][0]*m[0][3]*m[3][2] - m[3][0]*m[0][2]*m[1][3] + m[3][0]*m[0][3]*m[1][2];

    inv[1][3] =  m[0][0]*m[1][2]*m[2][3] - m[0][0]*m[1][3]*m[2][2] - m[1][0]*m[0][2]*m[2][3] +
                 m[1][0]*m[0][3]*m[2][2] + m[2][0]*m[0][2]*m[1][3] - m[2][0]*m[0][3]*m[1][2];

    inv[2][0] =  m[1][0]*m[2][1]*m[3][3] - m[1][0]*m[2][3]*m[3][1] - m[2][0]*m[1][1]*m[3][3] +
                 m[2][0]*m[1][3]*m[3][1] + m[3][0]*m[1][1]*m[2][3] - m[3][0]*m[1][3]*m[2][1];

    inv[2][1] = -m[0][0]*m[2][1]*m[3][3] + m[0][0]*m[2][3]*m[3][1] + m[2][0]*m[0][1]*m[3][3] -
                 m[2][0]*m[0][3]*m[3][1] - m[3][0]*m[0][1]*m[2][3] + m[3][0]*m[0][3]*m[2][1];

    inv[2][2] =  m[0][0]*m[1][1]*m[3][3] - m[0][0]*m[1][3]*m[3][1] - m[1][0]*m[0][1]*m[3][3] +
                 m[1][0]*m[0][3]*m[3][1] + m[3][0]*m[0][1]*m[1][3] - m[3][0]*m[0][3]*m[1][1];

    inv[2][3] = -m[0][0]*m[1][1]*m[2][3] + m[0][0]*m[1][3]*m[2][1] + m[1][0]*m[0][1]*m[2][3] -
                 m[1][0]*m[0][3]*m[2][1] - m[2][0]*m[0][1]*m[1][3] + m[2][0]*m[0][3]*m[1][1];

    inv[3][0] = -m[1][0]*m[2][1]*m[3][2] + m[1][0]*m[2][2]*m[3][1] + m[2][0]*m[1][1]*m[3][2] -
                 m[2][0]*m[1][2]*m[3][1] - m[3][0]*m[1][1]*m[2][2] + m[3][0]*m[1][2]*m[2][1];

    inv[3][1] =  m[0][0]*m[2][1]*m[3][2] - m[0][0]*m[2][2]*m[3][1] - m[2][0]*m[0][1]*m[3][2] +
                 m[2][0]*m[0][2]*m[3][1] + m[3][0]*m[0][1]*m[2][2] - m[3][0]*m[0][2]*m[2][1];

    inv[3][2] = -m[0][0]*m[1][1]*m[3][2] + m[0][0]*m[1][2]*m[3][1] + m[1][0]*m[0][1]*m[3][2] -
                 m[1][0]*m[0][2]*m[3][1] - m[3][0]*m[0][1]*m[1][2] + m[3][0]*m[0][2]*m[1][1];

    inv[3][3] =  m[0][0]*m[1][1]*m[2][2] - m[0][0]*m[1][2]*m[2][1] - m[1][0]*m[0][1]*m[2][2] +
                 m[1][0]*m[0][2]*m[2][1] + m[2][0]*m[0][1]*m[1][2] - m[2][0]*m[0][2]*m[1][1];

    float det = m[0][0]*inv[0][0] + m[0][1]*inv[1][0] + m[0][2]*inv[2][0] + m[0][3]*inv[3][0];

    if (abs(det) < 1e-6)
        return float4x4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0); // Non-invertible

    return inv / det;
}

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

float4 SampleColoredShadowMap(float2 uv, int mapIndex, int textureSizeX, int textureSizeY, out float mask)
{
    float4 output = float4(0, 0, 0, 0);
    mask = 0;
    float2 texelOffsetX = float2(1.0 / textureSizeX, 0);
    float2 texelOffsetY = float2(0, 1.0 / textureSizeY);

    switch (mapIndex)
    {
        case 0:
            output = _ColoredShadowMap0.Sample(point_clamp_sampler, uv);
            mask += _ColoredShadowMap0.Sample(trilinear_clamp_sampler, uv + texelOffsetX).r;
            mask += _ColoredShadowMap0.Sample(trilinear_clamp_sampler, uv - texelOffsetX).r;
            mask += _ColoredShadowMap0.Sample(trilinear_clamp_sampler, uv + texelOffsetY).r;
            mask += _ColoredShadowMap0.Sample(trilinear_clamp_sampler, uv - texelOffsetY).r;
            mask += _ColoredShadowMap0.Sample(trilinear_clamp_sampler, uv).r;
            break;
        case 1:
            output = _ColoredShadowMap1.Sample(point_clamp_sampler, uv);
            mask += _ColoredShadowMap1.Sample(trilinear_clamp_sampler, uv + texelOffsetX).r;
            mask += _ColoredShadowMap1.Sample(trilinear_clamp_sampler, uv - texelOffsetX).r;
            mask += _ColoredShadowMap1.Sample(trilinear_clamp_sampler, uv + texelOffsetY).r;
            mask += _ColoredShadowMap1.Sample(trilinear_clamp_sampler, uv - texelOffsetY).r;
            mask += _ColoredShadowMap1.Sample(trilinear_clamp_sampler, uv).r;
            break;
        case 2:
            output = _ColoredShadowMap2.Sample(point_clamp_sampler, uv);
            mask += _ColoredShadowMap2.Sample(trilinear_clamp_sampler, uv + texelOffsetX).r;
            mask += _ColoredShadowMap2.Sample(trilinear_clamp_sampler, uv - texelOffsetX).r;
            mask += _ColoredShadowMap2.Sample(trilinear_clamp_sampler, uv + texelOffsetY).r;
            mask += _ColoredShadowMap2.Sample(trilinear_clamp_sampler, uv - texelOffsetY).r;
            mask += _ColoredShadowMap2.Sample(trilinear_clamp_sampler, uv).r;
            break;
        case 3:
            output = _ColoredShadowMap3.Sample(point_clamp_sampler, uv);
            mask += _ColoredShadowMap3.Sample(trilinear_clamp_sampler, uv + texelOffsetX).r;
            mask += _ColoredShadowMap3.Sample(trilinear_clamp_sampler, uv - texelOffsetX).r;
            mask += _ColoredShadowMap3.Sample(trilinear_clamp_sampler, uv + texelOffsetY).r;
            mask += _ColoredShadowMap3.Sample(trilinear_clamp_sampler, uv - texelOffsetY).r;
            mask += _ColoredShadowMap3.Sample(trilinear_clamp_sampler, uv).r;
            break;
        case 4:
            output = _ColoredShadowMap4.Sample(point_clamp_sampler, uv);
            mask += _ColoredShadowMap4.Sample(trilinear_clamp_sampler, uv + texelOffsetX).r;
            mask += _ColoredShadowMap4.Sample(trilinear_clamp_sampler, uv - texelOffsetX).r;
            mask += _ColoredShadowMap4.Sample(trilinear_clamp_sampler, uv + texelOffsetY).r;
            mask += _ColoredShadowMap4.Sample(trilinear_clamp_sampler, uv - texelOffsetY).r;
            mask += _ColoredShadowMap4.Sample(trilinear_clamp_sampler, uv).r;
            break;
        case 5:
            output = _ColoredShadowMap5.Sample(point_clamp_sampler, uv);
            mask += _ColoredShadowMap5.Sample(trilinear_clamp_sampler, uv + texelOffsetX).r;
            mask += _ColoredShadowMap5.Sample(trilinear_clamp_sampler, uv - texelOffsetX).r;
            mask += _ColoredShadowMap5.Sample(trilinear_clamp_sampler, uv + texelOffsetY).r;
            mask += _ColoredShadowMap5.Sample(trilinear_clamp_sampler, uv - texelOffsetY).r;
            mask += _ColoredShadowMap5.Sample(trilinear_clamp_sampler, uv).r;
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
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation.index, textureSizeX, textureSizeY, mask);
            if (tempOutput.x > 0 && lightUv.x > 1.0 / textureSizeX && lightUv.x < (textureSizeX - 1) / textureSizeX && lightUv.y > 1.0 / textureSizeY && lightUv.y < (textureSizeY - 1) / textureSizeY)
            {
                finalUV = lightUv;
                lowestDist = dist;
                output = tempOutput;
                output.r *= lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
                fallOffRange = lightInformation.fallOffRange;
                mask = saturate(((1 - (saturate(output.r) * mask)) * 3));
            }
            break;
        case 1: // Spot
            lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y, worldPos.z, 1));
            lightUv = lightSpace.rgb / lightSpace.a;
            lightUv *= 0.5;
            lightUv += 0.5;
            tempOutput = SampleColoredShadowMap(lightUv.rg, lightInformation.index, textureSizeX, textureSizeY, mask);
            if (tempOutput.x > 0 && lightUv.x > 1.0 / textureSizeX && lightUv.x < (textureSizeX - 1) / textureSizeX && lightUv.y > 1.0 / textureSizeY && lightUv.y < (textureSizeY - 1) / textureSizeY && dist < lowestDist && dist < 1)
            {
                finalUV = lightUv;
                lowestDist = dist;
                output = tempOutput;
                output.r *= lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
                fallOffRange = lightInformation.fallOffRange;
                mask = saturate(((1 - (saturate(output.r) * mask)) * 3));
            }
            break;
        case 2: //Point
            float3 dir = normalize(lightInformation.lightPos - worldPos);
            int faceIndex;
            SampleCustomCubeMap(dir, uv, faceIndex);
            float2 cubemapUV = float2((uv.x / 6.0) + ((1.0/6.0) * faceIndex), uv.y);
            textureSizeX *= 6;
            tempOutput = SampleColoredShadowMap(cubemapUV, lightInformation.index, textureSizeX, textureSizeY, mask);
            mask = saturate((1 - mask) * 2);
            if (tempOutput.x > 0 && dist < lowestDist && dist < 1)
            {
                finalUV = uv;
                lowestDist = dist;
                output = tempOutput;
                output.r *= lightInformation.lightIDMultiplier;
                lightPos = lightInformation.lightPos;
                fallOffRange = lightInformation.fallOffRange;
            }
            break;
        }
    }
}

void SampleColoredShadows_half(float3 worldPos, out float4 output, out float2 finalUV, out float3 lightPos, out float fallOffRange, out float mask)
{
    output = float4(0, 0, 0, 0);
    lightPos = float3(-999999999, -999999999, -999999999);
    fallOffRange = 0;
    finalUV = float2(0, 0);
    mask = 0;
}

#endif 