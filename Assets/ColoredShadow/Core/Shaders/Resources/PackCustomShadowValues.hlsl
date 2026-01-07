#ifndef CUSTOM_SHADOW_PACKER_INCLUDED
#define CUSTOM_SHADOW_PACKER_INCLUDED

void PackCustomShadowValues_float(float shadowID, float blur, float shadowUVPosX, float shadowUVPosY, float shadowUVSize, out uint2 output)
{
    // Convert normalized [0,1] values to integer bit ranges
    uint shadowID_bits = uint(shadowID);      // 10 bits (0-1023)
    uint blur_bits = uint(blur * 63.0);                // 6 bits (0-63)
    uint shadowUVSize_bits = min(uint(shadowUVSize * 65535.0), 65535);           // 16 bits (0-65535)

    // Pack into two 32-bit uints
    uint packed1 = (shadowID_bits << 22) | (blur_bits << 16) | shadowUVSize_bits;

    uint shadowUVPosX_bits  = shadowUVPosX * 65535;     // float → half bits
    // uint shadowUVPosX_bits  = min(65535, uint(shadowUVPosX * 65535));     // float → half bits
    uint shadowUVPosY_bits = min(65535, uint(shadowUVPosY * 65535));
    uint packed2 = shadowUVPosX_bits & 65535;
    // uint packed2 = (shadowUVPosY_bits << 16) | shadowUVPosX_bits;
    
    // Convert uint to float for output
    output.x = (packed1);
    output.y = (packed2);
}

void UnpackCustomShadowValues_float(uint2 input, out float shadowID, out float blur, out float shadowUVPosX, out float shadowUVPosY, out float shadowUVSize)
{
    // Convert float back to uint
    uint packed1 = (input.x);
    uint packed2 = (input.y);
    
    // Unpack first uint (32 bits)
    uint shadowID_bits = (packed1 >> 22);      // Extract 10 bits (mask: 1023)
    uint blur_bits = (packed1 >> 16) & 0x3F;           // Extract 6 bits (mask: 63)
    uint shadowUVSize_bits = packed1 & 0xFFFF;          // Extract 16 bits (mask: 65535)
    
    // Convert back to normalized [0,1] values
    shadowID = float(shadowID_bits);
    blur = float(blur_bits) / 63.0;
    shadowUVSize = float(shadowUVSize_bits) / 65535.0;
    shadowUVPosY = 1;
    // shadowUVPosY = float(packed2 >> 16) / 65535.0;
    shadowUVPosX = float(packed2) / 65535;
    // shadowUVPosX = float(packed2 & 65535) / 65535.0;
}


uint GetShadowID(float2 input)
{
    uint packed1 = (input.x);
    return (packed1 >> 22) & 0x3FF;
}

float2 SetBlur(float2 input, float blur)
{
    uint packed1 = (input.x);
    uint blurBits = uint(blur * 63);
    uint mask = 4128768; //00000000001111110000000000000000
    packed1 &= ~mask;
    return float2((packed1 | (blurBits << 16)), input.y);
}

float2 SetDepth(float2 input, float depth)
{
    uint packed1 = (input.x);
    uint depthBits = uint(depth * 65535);
    // uint mask = 65535; //00000000000000001111111111111111
    // packed1 |= ~mask;
    return float2((packed1 | depthBits), input.y);
}

void PackCustomShadowValues_half(float shadowID, float blur, float shadowUVPosX, float shadowUVPosY, float shadowUVSize, out float2 output)
{
    // Convert normalized [0,1] values to integer bit ranges
    uint shadowID_bits = uint(shadowID);      // 10 bits (0-1023)
    uint blur_bits = uint(blur * 63.0);                // 6 bits (0-63)
    uint shadowUVSize_bits = min(uint(shadowUVSize * 65535.0), 65535);           // 16 bits (0-65535)

    // Pack into two 32-bit uints
    uint packed1 = (shadowID_bits << 22) | (blur_bits << 16) | shadowUVSize_bits;

    uint shadowUVPosX_bits  = min(65535, uint(shadowUVPosX * 65535.0));     // float → half bits
    uint shadowUVPosY_bits = min(65535, uint(shadowUVPosY * 65535.0));
    uint packed2 = (shadowUVPosX_bits << 16) | shadowUVPosY_bits;
    
    // Convert uint to float for output
    output.x = (packed1);
    output.y = (packed2);

    // output.z = 0;
    // output.w = 0;
}

void UnpackCustomShadowValues_half(float2 input, out float shadowID, out float blur, out float shadowUVPosX, out float shadowUVPosY, out float shadowUVSize)
{
    // Convert float back to uint
    uint packed1 = (input.x);
    uint packed2 = (input.y);
    
    // Unpack first uint (32 bits)
    uint shadowID_bits = (packed1 >> 22);      // Extract 10 bits (mask: 1023)
    uint blur_bits = (packed1 >> 16) & 0x3F;           // Extract 6 bits (mask: 63)
    uint shadowUVSize_bits = packed1 & 0xFFFF;          // Extract 16 bits (mask: 65535)
    
    // Convert back to normalized [0,1] values
    shadowID = float(shadowID_bits);
    blur = float(blur_bits) / 63.0;
    shadowUVSize = float(shadowUVSize_bits) / 65535.0;
    shadowUVPosX = float(packed2 >> 16) / 65535.0;
    // shadowUVPosX = floor(float(packed2 >> 16) / 65535.0 * 1023) / 1023;
    shadowUVPosY = float(packed2 & 65535) / 65535.0;
    // shadowUVPosY = floor(float(packed2 & 0xFFFF) / 65535.0 * 1023) / 1023;
}

#endif
