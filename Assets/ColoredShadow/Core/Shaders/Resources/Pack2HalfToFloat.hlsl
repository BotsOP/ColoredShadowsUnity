#ifndef HALF_TO_FLOAT_INCLUDED
#define HALF_TO_FLOAT_INCLUDED

void Pack2HalfToFloat_float(float2 v, out float result)
{
    uint low  = f32tof16(v.x);     // float → half bits
    uint high = f32tof16(v.y);
    uint packed = (high << 16) | (low & 0xFFFF);
    result = asfloat(packed);       // reinterpret bits as float
}

void Pack2HalfToFloat_half(float2 v, out float result)
{
    uint low  = f32tof16(v.x);     // float → half bits
    uint high = f32tof16(v.y);
    uint packed = (high << 16) | (low & 0xFFFF);
    result = asfloat(packed);       // reinterpret bits as float
}

void UnpackFloatTo2Half_float(float packed, out float2 result)
{
    uint bits = asuint(packed);
    float x = f16tof32( bits & 0xFFFF );
    float y = f16tof32( bits >> 16 );
    result = float2(x, y);
}

void UnpackFloatTo2Half_half(float packed, out float2 result)
{
    uint bits = asuint(packed);
    float x = f16tof32( bits & 0xFFFF );
    float y = f16tof32( bits >> 16 );
    result = float2(x, y);
}

#endif
