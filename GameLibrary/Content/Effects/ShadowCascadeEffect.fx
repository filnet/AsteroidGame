//-----------------------------------------------------------------------------
// ShadowCascadeEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"

BEGIN_CONSTANTS

MATRIX_CONSTANTS

    float4x4 WorldViewProj          _vs(c15)          _cb(c0);

END_CONSTANTS

// see https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch10.html

struct VSInput
{
    float4 Position : POSITION;
    //float3 Normal : NORMAL;
    uint Instance : SV_InstanceID;
};

struct VSOutput
{
    float4 PositionPS     : POSITION;
    uint SplitIndex : TEXTURE0;
};

VSOutput VSShadow(VSInput vin)
{
    VSOutput vout;

    vout.SplitIndex = vin.Instance;

    vout.PositionPS = mul(vin.Position, WorldViewProj);
    vout.PositionPS.z += 0.002f;

    return vout;
}

//TECHNIQUE(ShadowCascadeEffect, VSShadow, PSShadow);

technique Render
{
    pass P0
    {
        VertexShader = compile vs_5_0 VSShadow();
    }
}
