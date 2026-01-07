struct LightInformation
{
    int index;
    int lightMode;
    float4x4 lightMatrix;
    float4x4 invLightMatrix;
    float3 lightPos;
    float fallOffRange;
    float nearPlane;
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

float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
{
    float a = newMin + (value - oldMin) * (newMax - newMin);
    float b = (oldMax - oldMin);
    return a / b;
}

float BilinearSampleCompact(float bottomLeft, float bottomRight, float topLeft, float topRight, float2 uv)
{
    return lerp(
        lerp(bottomLeft, bottomRight, uv.x),  // Bottom edge interpolation
        lerp(topLeft, topRight, uv.x),       // Top edge interpolation
        uv.y                                 // Vertical interpolation
    );
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

bool CheckIsInBounds(LightInformation lightInformation, float2 lightUv)
{
    return lightUv.x < (lightInformation.shadowAtlasPosX + lightInformation.textureSizeX) && lightUv.x >  lightInformation.shadowAtlasPosX &&
                lightUv.y < (lightInformation.shadowAtlasPosY + lightInformation.textureSizeY) && lightUv.y >  lightInformation.shadowAtlasPosY;
}

float2 GetLocalShadowUV(float shadowUVMultiplier, bool relativeUVSize, float shadowRelativeSize, float2 shadowSize, float2 lightUv)
{
    int clampAmount = pow(2, 8);
    // shadowSize.x = floor(shadowSize.x * clampAmount) / clampAmount;
    // shadowSize.y = floor(shadowSize.y * clampAmount) / clampAmount;
    // shadowRelativeSize = floor(shadowRelativeSize * clampAmount / 2) / clampAmount / 2;
    float shadowSizeMultiplier = relativeUVSize ? shadowRelativeSize : 1;
    shadowSizeMultiplier *= shadowUVMultiplier;
    float2 shadowUVX = float2(shadowSize.r - shadowSizeMultiplier, shadowSize.r + shadowSizeMultiplier);
    float2 shadowUVY = float2(shadowSize.g - shadowSizeMultiplier, shadowSize.g + shadowSizeMultiplier);
    return float2(remap(lightUv.x, shadowUVX.x, shadowUVX.y, 0, 1), remap(lightUv.y, shadowUVY.x, shadowUVY.y, 0, 1));
}

float2 GetLocalShadowAtlasUV(float2 uv, LightInformation lightInformation)
{
    float shadowAtlasPosX = lightInformation.shadowAtlasPosX;
    float shadowAtlasPosY = lightInformation.shadowAtlasPosY;
    float textureWidth = lightInformation.textureSizeX;
    float textureHeight = lightInformation.textureSizeY;
    uv *= float2(textureWidth, textureHeight);
    uv += float2(shadowAtlasPosX, shadowAtlasPosY);
    return uv;
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



static const float4x4 pointRotXNegMatrix = float4x4(
0.00000, 0.00000, 1.00000, 0.00000,
0.00000, 1.00000, 0.00000, 0.00000,
-1.00000, 0.00000, 0.00000, 0.00000,
0.00000, 0.00000, 0.00000, 1.00000
);

static const float4x4 pointRotZNegMatrix = float4x4(
-1.00000, 0.00000, 0.00000, 0.00000,
0.00000, 1.00000, 0.00000, 0.00000,
0.00000, 0.00000, -1.00000, 0.00000,
0.00000, 0.00000, 0.00000, 1.00000
);

static const float4x4 pointRotXPlusMatrix = float4x4(
0.00000, 0.00000, -1.00000, 0.00000,
0.00000, 1.00000, 0.00000, 0.00000,
1.00000, 0.00000, 0.00000, 0.00000,
0.00000, 0.00000, 0.00000, 1.00000
);

static const float4x4 pointRotYNegMatrix = float4x4(
1.00000, 0.00000, 0.00000, 0.00000,
0.00000, 0.00000, -1.00000, 0.00000,
0.00000, 1.00000, 0.00000, 0.00000,
0.00000, 0.00000, 0.00000, 1.00000
);

static const float4x4 pointRotYPlusMatrix = float4x4(
1.00000, 0.00000, 0.00000, 0.00000,
0.00000, 0.00000, 1.00000, 0.00000,
0.00000, -1.00000, 0.00000, 0.00000,
0.00000, 0.00000, 0.00000, 1.00000
);

float4x4 InverseMatrixCM(float4x4 m)
{
    float4x4 inv;

    inv[0][0] =  m[1][1]*m[2][2]*m[3][3] - m[1][1]*m[2][3]*m[3][2]
               - m[2][1]*m[1][2]*m[3][3] + m[2][1]*m[1][3]*m[3][2]
               + m[3][1]*m[1][2]*m[2][3] - m[3][1]*m[1][3]*m[2][2];

    inv[1][0] = -m[1][0]*m[2][2]*m[3][3] + m[1][0]*m[2][3]*m[3][2]
               + m[2][0]*m[1][2]*m[3][3] - m[2][0]*m[1][3]*m[3][2]
               - m[3][0]*m[1][2]*m[2][3] + m[3][0]*m[1][3]*m[2][2];

    inv[2][0] =  m[1][0]*m[2][1]*m[3][3] - m[1][0]*m[2][3]*m[3][1]
               - m[2][0]*m[1][1]*m[3][3] + m[2][0]*m[1][3]*m[3][1]
               + m[3][0]*m[1][1]*m[2][3] - m[3][0]*m[1][3]*m[2][1];

    inv[3][0] = -m[1][0]*m[2][1]*m[3][2] + m[1][0]*m[2][2]*m[3][1]
               + m[2][0]*m[1][1]*m[3][2] - m[2][0]*m[1][2]*m[3][1]
               - m[3][0]*m[1][1]*m[2][2] + m[3][0]*m[1][2]*m[2][1];

    inv[0][1] = -m[0][1]*m[2][2]*m[3][3] + m[0][1]*m[2][3]*m[3][2]
               + m[2][1]*m[0][2]*m[3][3] - m[2][1]*m[0][3]*m[3][2]
               - m[3][1]*m[0][2]*m[2][3] + m[3][1]*m[0][3]*m[2][2];

    inv[1][1] =  m[0][0]*m[2][2]*m[3][3] - m[0][0]*m[2][3]*m[3][2]
               - m[2][0]*m[0][2]*m[3][3] + m[2][0]*m[0][3]*m[3][2]
               + m[3][0]*m[0][2]*m[2][3] - m[3][0]*m[0][3]*m[2][2];

    inv[2][1] = -m[0][0]*m[2][1]*m[3][3] + m[0][0]*m[2][3]*m[3][1]
               + m[2][0]*m[0][1]*m[3][3] - m[2][0]*m[0][3]*m[3][1]
               - m[3][0]*m[0][1]*m[2][3] + m[3][0]*m[0][3]*m[2][1];

    inv[3][1] =  m[0][0]*m[2][1]*m[3][2] - m[0][0]*m[2][2]*m[3][1]
               - m[2][0]*m[0][1]*m[3][2] + m[2][0]*m[0][2]*m[3][1]
               + m[3][0]*m[0][1]*m[2][2] - m[3][0]*m[0][2]*m[2][1];

    inv[0][2] =  m[0][1]*m[1][2]*m[3][3] - m[0][1]*m[1][3]*m[3][2]
               - m[1][1]*m[0][2]*m[3][3] + m[1][1]*m[0][3]*m[3][2]
               + m[3][1]*m[0][2]*m[1][3] - m[3][1]*m[0][3]*m[1][2];

    inv[1][2] = -m[0][0]*m[1][2]*m[3][3] + m[0][0]*m[1][3]*m[3][2]
               + m[1][0]*m[0][2]*m[3][3] - m[1][0]*m[0][3]*m[3][2]
               - m[3][0]*m[0][2]*m[1][3] + m[3][0]*m[0][3]*m[1][2];

    inv[2][2] =  m[0][0]*m[1][1]*m[3][3] - m[0][0]*m[1][3]*m[3][1]
               - m[1][0]*m[0][1]*m[3][3] + m[1][0]*m[0][3]*m[3][1]
               + m[3][0]*m[0][1]*m[1][3] - m[3][0]*m[0][3]*m[1][1];

    inv[3][2] = -m[0][0]*m[1][1]*m[3][2] + m[0][0]*m[1][2]*m[3][1]
               + m[1][0]*m[0][1]*m[3][2] - m[1][0]*m[0][2]*m[3][1]
               - m[3][0]*m[0][1]*m[1][2] + m[3][0]*m[0][2]*m[1][1];

    inv[0][3] = -m[0][1]*m[1][2]*m[2][3] + m[0][1]*m[1][3]*m[2][2]
               + m[1][1]*m[0][2]*m[2][3] - m[1][1]*m[0][3]*m[2][2]
               - m[2][1]*m[0][2]*m[1][3] + m[2][1]*m[0][3]*m[1][2];

    inv[1][3] =  m[0][0]*m[1][2]*m[2][3] - m[0][0]*m[1][3]*m[2][2]
               - m[1][0]*m[0][2]*m[2][3] + m[1][0]*m[0][3]*m[2][2]
               + m[2][0]*m[0][2]*m[1][3] - m[2][0]*m[0][3]*m[1][2];

    inv[2][3] = -m[0][0]*m[1][1]*m[2][3] + m[0][0]*m[1][3]*m[2][1]
               + m[1][0]*m[0][1]*m[2][3] - m[1][0]*m[0][3]*m[2][1]
               - m[2][0]*m[0][1]*m[1][3] + m[2][0]*m[0][3]*m[1][1];

    inv[3][3] =  m[0][0]*m[1][1]*m[2][2] - m[0][0]*m[1][2]*m[2][1]
               - m[1][0]*m[0][1]*m[2][2] + m[1][0]*m[0][2]*m[2][1]
               + m[2][0]*m[0][1]*m[1][2] - m[2][0]*m[0][2]*m[1][1];

    float det =
        m[0][0] * inv[0][0] +
        m[0][1] * inv[1][0] +
        m[0][2] * inv[2][0] +
        m[0][3] * inv[3][0];

    return inv / det;
}


float3 DepthToWorldPositionViewProj(float2 screenUV, float depth, float nearPlane, float farPlane, float3 position, int faceIndex)
{
    float4x4 viewMatrix = float4x4(
        1.0, 0.0, 0.0, -position.x,
        0.0, 1.0, 0.0, -position.y,
        0.0, 0.0, -1.0, position.z,
        0, 0, 0, 1.0
    );

    switch (faceIndex)
    {
    case 1:
        viewMatrix = mul(pointRotXNegMatrix, viewMatrix);
        break;
    case 2:
        viewMatrix = mul(pointRotZNegMatrix, viewMatrix);
        break;
    case 3:
        viewMatrix = mul(pointRotXPlusMatrix, viewMatrix);
        break;
    case 4:
        viewMatrix = mul(pointRotYNegMatrix, viewMatrix);
        break;
    case 5:
        viewMatrix = mul(pointRotYPlusMatrix, viewMatrix);
        break;
    default: break;
    }

    float deltaZ = farPlane - nearPlane;

    float4x4 pointProjMatrix = float4x4(
    1, 0, 0, 0,
    0, 1, 0, 0,
    0, 0, -(farPlane + nearPlane) / deltaZ, -(2 * farPlane * nearPlane) / deltaZ,
    0, 0, -1, 0
    );
    
    float4x4 projViewMatrix = mul(pointProjMatrix, viewMatrix);

    projViewMatrix = InverseMatrixCM(projViewMatrix);
    
    float2 ndcXY = screenUV * 2.0 - 1;
    float worldZDistance = 0.1 + (depth * (100 - 0.1));
    float ndcZ = (worldZDistance - 0.1) * 2.0f / (100 - 0.1) - 1.0f;
    ndcZ *= -1;
    float4 ndcPos = float4(ndcXY.x, ndcXY.y, ndcZ, 1.0);
    float4 worldPos = mul(projViewMatrix, ndcPos);
    worldPos.xyz /= worldPos.w;

    return worldPos.xyz;
}

float2 GetLightUV(LightInformation lightInformation, float3 worldPos)
{
    float4 lightSpace = mul(lightInformation.lightMatrix, float4(worldPos.x, worldPos.y, worldPos.z, 1));
    float3 lightUv = lightSpace.rgb / lightSpace.a;
    lightUv *= 0.5;
    lightUv += 0.5;
    return lightUv;
}












