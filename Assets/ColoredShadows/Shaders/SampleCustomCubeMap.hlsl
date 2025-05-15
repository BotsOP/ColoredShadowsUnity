#ifndef SAMPLE_CUSTOM_CUBEMAP_INCLUDED
#define SAMPLE_CUSTOM_CUBEMAP_INCLUDED

// The function signature must match exactly what you define in the Custom Function node
void SampleCubeMap_float(float3 direction, out float index)
{
    index = 10;
    float forward = dot(direction, float3(0.0f, 0.0f, 1.0f));
    float right = dot(direction, float3(1.0f, 0.0f, 0.0f));
    float back = dot(direction, float3(0.0f, 0.0f, -1.0f));
    float left = dot(direction, float3(-1.0f, 0.0f, 0.0f));
    float down = dot(direction, float3(0.0f, -1.0f, 0.0f));
    float up = dot(direction, float3(0.0f, 1.0f, 0.0f));
    float closestDir = min(up,min(down,min(min(min(forward, right), back), left)));
    if (closestDir == forward)
    {
        index = 0;
    }
    if (closestDir == back)
    {
        index = 2;
    }
    if (closestDir == left)
    {
        index = 3;
    }
    if (closestDir == right)
    {
        index = 1;
    }
    if (closestDir == down)
    {
        index = 4;
    }
    if (closestDir == up)
    {
        index = 5;
    }
    // if (direction.x > -0.5 && direction.x < 0.5 && direction.y > -0.5 && direction.y < 0.5 && direction.z >= 0)
    // {
    //     index = 0;
    // }
    // if (direction.z > -0.5 && direction.z < 0.5 && direction.y > -0.5 && direction.y < 0.5 && direction.x >= 0)
    // {
    //     index = 1;
    // }
    // if (direction.x > -0.5 && direction.x < 0.5 && direction.y > -0.5 && direction.y < 0.5 && direction.z < 0)
    // {
    //     index = 2;
    // }
    // if (direction.z > -0.5 && direction.z < 0.5 && direction.y > -0.5 && direction.y < 0.5 && direction.x < 0)
    // {
    //     index = 3;
    // }
    // if (direction.z > -0.5 && direction.z < 0.5 && direction.x > -0.5 && direction.x < 0.5 && direction.y >= 0)
    // {
    //     index = 4;
    // }
    // if (direction.x > -0.5 && direction.x < 0.5 && direction.z > -0.5 && direction.z < 0.5 && direction.y < 0)
    // {
    //     index = 5;
    // }
}

// Half precision version for mobile platforms
// void ProceduralWave_half(half2 UV, half Time, half WaveCount, half WaveSpeed, half WaveAmplitude, out half3 Displacement, out half3 Normal)
// {
// }

#endif 