//-----------------------------------------------------------------------------
// ClippingEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#include "Macros.fxh"


BEGIN_CONSTANTS

    float4x4 World                  _vs(c19)          _cb(c15);

	float4 ClippingPlane1;
	float4 ClippingPlane2;
	float4 ClippingPlane3;
	float4 ClippingPlane4;

	float4 Color;

MATRIX_CONSTANTS

    float4x4 WorldViewProj          _vs(c15)          _cb(c0);

END_CONSTANTS


//#include "Structures.fxh"
//#include "Common.fxh"


struct VertexShaderInput
{
	float3 Position : POSITION0;
	float4 Color :  COLOR;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float4 Color : COLOR;
	float4 clipping :  TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position =  mul(float4(input.Position, 1), WorldViewProj);
	output.Color = input.Color * Color;
	output.clipping = 0;
	float4 v = mul(float4(input.Position, 1), World);
	//normalize(v);
	output.clipping.x = dot(v, ClippingPlane1) ;
	output.clipping.y = dot(v, ClippingPlane2) ;
	output.clipping.z = dot(v, ClippingPlane3) ;
	output.clipping.w = dot(v, ClippingPlane4) ;
	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	//if(isClip)
		clip(input.clipping.x);
		clip(input.clipping.y);
		clip(input.clipping.z);
		clip(input.clipping.w);

	return input.Color;
}

technique ClippingEffect
{
	pass Pass1
	{
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
	}
}