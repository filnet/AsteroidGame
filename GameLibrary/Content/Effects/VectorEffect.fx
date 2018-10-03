//-----------------------------------------------------------------------------
// BasicEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"


BEGIN_CONSTANTS

    float4x4 World                  _vs(c19)          _cb(c15);

MATRIX_CONSTANTS

    float4x4 WorldViewProj          _vs(c15)          _cb(c0);

END_CONSTANTS


#include "Structures.fxh"
//#include "Common.fxh"

// Vertex shader: basic.
VSOutputBasic VSBasic(VSInputVc vin)
{
    VSOutputBasic vout;
    
	vout.Diffuse = vin.Color;
	vout.PositionPS = mul(vin.Position, WorldViewProj);
    
    return vout;
}



// Pixel shader: basic.
float4 PSBasic(PSInput pin) : SV_Target0
{
    return pin.Diffuse;
}


Technique VectorEffect
{
    Pass
    {
        VertexShader = compile vs_2_0 VSBasic();
        PixelShader  = compile ps_2_0 PSBasic();
    }
}
