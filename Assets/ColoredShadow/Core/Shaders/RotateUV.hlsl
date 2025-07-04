void RotateUVsToFaceCamera_float(float2 uv, float3 worldPos, float3 cameraPos, float2 pivot, float2 tiling, out float2 output)
{
    // Step 1: Get the flat direction to camera (XZ plane)
    float2 toCamera = normalize(cameraPos.xz - worldPos.xz);

    // Step 2: Calculate angle between world forward (0, 1) and toCamera direction
    float angle = atan2(toCamera.x, toCamera.y); // Note: atan2(x, y) = angle from (0,1) to (x,y)

    // Step 3: Build 2D rotation matrix
    float cosA = cos(angle);
    float sinA = sin(angle);
    float2x2 rotationMatrix = float2x2(
        cosA, -sinA,
        sinA,  cosA
    );

    // Step 4: Rotate UVs around a pivot (e.g., center = 0.5,0.5 or mesh origin)
    float2 rotatedUV = mul(rotationMatrix, uv - pivot) + pivot;

    // Step 5: Apply tiling
    rotatedUV *= tiling;

    output = rotatedUV;
}
