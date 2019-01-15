//-----------------------------------------------------------------------------
// VoxelShadowEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"

BEGIN_CONSTANTS

MATRIX_CONSTANTS

    float4x4 WorldViewProj          _vs(c15)          _cb(c0);

END_CONSTANTS


struct VSInput
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
};

struct VSShadowOutput
{
    float4 PositionPS     : POSITION;
    float2 Depth     : TEXCOORD0;
};

VSShadowOutput VSShadow(VSInput vin)
{
    VSShadowOutput vout;
    
	vout.PositionPS = mul(vin.Position, WorldViewProj);
	//vout.Depth = vout.PositionPS.zw;
    
    return vout;
}

float PSShadow(VSShadowOutput pin) : SV_Target0
{ 
    //float depth = pin.Depth.x / pin.Depth.y;
    //return depth;//float4(depth, depth, depth, depth);
    return pin.PositionPS.z / pin.PositionPS.w;
}

TECHNIQUE(ShadowEffect, VSShadow, PSShadow );
