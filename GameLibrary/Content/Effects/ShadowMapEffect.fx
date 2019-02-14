//-----------------------------------------------------------------------------
// ShadowMapEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"


DECLARE_TEXTURE_ARRAY(Texture, 0);


BEGIN_CONSTANTS


MATRIX_CONSTANTS

    float4x4 WorldViewProj        _vs(c15)     _cb(c0);

END_CONSTANTS


//#include "Structures.fxh"
//#include "Common.fxh"
//#include "Lighting.fxh"

struct VSInput
{
    float4 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};

struct VSOutput
{
    float4 PositionPS : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

// Vertex shader
VSOutput VSBasicTx(VSInput vin)
{
    VSOutput vout;
    vout.PositionPS = mul(vin.Position, WorldViewProj);
    vout.TexCoord = vin.TexCoord;
    return vout;
}

float4 PSBasicTx(VSOutput pin) : SV_Target0
{
    // target space is 2x2, tex coord is 1x1
    pin.TexCoord *= 2.0f;
    int2 ic = floor(pin.TexCoord);
    int textureIndex = ic.y * 2 + ic.x;
    pin.TexCoord -= ic;
    return SAMPLE_TEXTURE_ARRAY(Texture, float3(pin.TexCoord, textureIndex));
}

TECHNIQUE(ShadowMapEffect, VSBasicTx, PSBasicTx);
