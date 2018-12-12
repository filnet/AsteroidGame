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
};

struct VSShadowOutput
{
    float4 PositionPS     : POSITION;
    float4 Position2D     : TEXCOORD0;
};

VSShadowOutput VSShadow(VSInput vin)
{
    VSShadowOutput vout;
    
	vout.PositionPS = mul(vin.Position, WorldViewProj);
	vout.Position2D = vout.PositionPS;
    
    return vout;
}

float4 PSShadow(VSShadowOutput pin) : SV_Target0
{ 
	float depth = pin.Position2D.z / pin.Position2D.w;
    return float4(depth, depth, depth, depth);
}

TECHNIQUE(ShadowEffect, VSShadow, PSShadow );
