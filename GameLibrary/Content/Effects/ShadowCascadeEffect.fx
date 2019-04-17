//-----------------------------------------------------------------------------
// ShadowCascadeEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"

BEGIN_CONSTANTS

MATRIX_CONSTANTS

    float4x4 World          _vs(c15)          _cb(c0);
    float4x4 ViewProjections[4]          _vs(c16)          _cb(c0);

END_CONSTANTS

// see https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch10.html

struct VSInput
{
    float4 Position : POSITION;
    //float3 Normal   : NORMAL;
    //float2 TexCoord : TEXCOORD;
	//uint Instance : SV_InstanceID;
};

struct VSOutput
{
    float4 PositionPS     : POSITION;
    //uint SplitIndex : TEXTURE0;
	uint RTIndex : SV_RenderTargetArrayIndex;
};

VSOutput VSShadow(VSInput vin, uint4 splitIndex : BLENDINDICES1)
{
    VSOutput vout;

    //vout.SplitIndex = splitIndex[0];
	//vout.SplitIndex = vin.Instance;
	vout.RTIndex = splitIndex[0];

    vout.PositionPS = mul(mul(vin.Position, World), ViewProjections[splitIndex[0]]);
    vout.PositionPS.z += 0.002f;

    return vout;
}

//TECHNIQUE(ShadowCascadeEffect, VSShadow, PSShadow);

technique ShadowRender
{
    pass
    {
        VertexShader = compile vs_5_0 VSShadow();
    }
}
