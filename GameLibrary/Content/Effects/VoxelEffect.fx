//-----------------------------------------------------------------------------
// VoxelEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

// make VS happy...
#define SM4 1

#include "Macros.fxh"

DECLARE_TEXTURE_ARRAY(Texture, 0);

DECLARE_TEXTURE(WireframeTexture, 1);

DECLARE_TEXTURE_ARRAY(ShadowMapTexture, 2);

SamplerComparisonState ShadowSampler : register(s2);

BEGIN_CONSTANTS

    float4 AmbientColor             _vs(c0)  _ps(c1)  _cb(c0);
    float4 DiffuseColor             _vs(c1)  _ps(c2)  _cb(c1);
    float3 EmissiveColor            _vs(c2)  _ps(c3)  _cb(c3);
    float3 SpecularColor            _vs(c3)  _ps(c4)  _cb(c3);
    float  SpecularPower            _vs(c4)  _ps(c5)  _cb(c3.w);

    float3 DirLight0Direction       _vs(c5)  _ps(c6)  _cb(c4);
    float3 DirLight0DiffuseColor    _vs(c6)  _ps(c7)  _cb(c5);
    float3 DirLight0SpecularColor   _vs(c7)  _ps(c8)  _cb(c6);

    float3 DirLight1Direction       _vs(c8)  _ps(c9)  _cb(c7);
    float3 DirLight1DiffuseColor    _vs(c9)  _ps(c10) _cb(c8);
    float3 DirLight1SpecularColor   _vs(c10) _ps(c11) _cb(c9);

    float3 DirLight2Direction       _vs(c11) _ps(c12) _cb(c10);
    float3 DirLight2DiffuseColor    _vs(c12) _ps(c13) _cb(c11);
    float3 DirLight2SpecularColor   _vs(c13) _ps(c14) _cb(c12);

    float3 EyePosition              _vs(c14) _ps(c15) _cb(c13);

    float3 FogColor                          _ps(c0)  _cb(c14);
    float4 FogVector                _vs(c15)          _cb(c15);

    float4x4 World                  _vs(c19)          _cb(c16);
    float3x3 WorldInverseTranspose  _vs(c23)          _cb(c19);

	int CascadeCount = 4;
    float4x4 LightView;
    float4x4 LightViews[4];
    float4 SplitDistances[8];
    float4 SplitOffsets[8];
    float4 SplitScales[8];

	int WireframeMode;
	bool VisualizeSplits;


MATRIX_CONSTANTS

    float4x4 WorldView          _vs(c15)          _cb(c0);
    float4x4 WorldViewProj          _vs(c15)          _cb(c0);

END_CONSTANTS

#include "VoxelStructures.fxh"
#include "Common.fxh"
#include "Lighting.fxh"

// Ambient occlusion.

static float4 ambientOcclusionCurve = float4(0.2f, 0.6f, 0.8f, 1.0f);
//static float4 ambientOcclusionCurve = float4(0.0f, 0.33f, 0.66f, 1.0f);
//static float4 ambientOcclusionCurve = float4(0.0f, 0.0f, 0.0f, 1.0f);

float4 ComputeAmbientOcclusionFactors(int aoBits)
{
    return float4(
		ambientOcclusionCurve[aoBits & 3],
		ambientOcclusionCurve[(aoBits >> 2) & 3],
		ambientOcclusionCurve[(aoBits >> 4) & 3],
		ambientOcclusionCurve[(aoBits >> 6) & 3]);
}

// https://thebookofshaders.com/05/
float SampleAmbientOcclusionFactors(float4 factors, float2 texCoord)
{
   // TODO optimize lerps away if all factors are the same
    float f1 = lerp(factors[0], factors[2], texCoord.x);
    float f2 = lerp(factors[1], factors[3], texCoord.x);
    float f = lerp(f1, f2, texCoord.y);
    //return f > 0.5 ? f : 0;
	return f;
}

float ComputeWFTexCoord(float x, int w)
{
	switch(WireframeMode)
	{
    case 1: // QUAD
		return x ? 1 : -1;
    case 2: // FACE
		int b = 2 * w - 1;
		return x ? 1 : -b;
    default:
        return 0;
	}
}

#define SetVoxelVSOutputParams \
    vout.TexCoord = vin.TexCoord; \
	vout.TextureIndex = vin.TextureIndex; \
	vout.AmbientOcclusionFactors = ComputeAmbientOcclusionFactors(vin.TextureIndex[1]); \
	vout.WF1TexCoord = ComputeWFTexCoord(vin.TexCoord.x, vin.TextureIndex[2]); \
	vout.WF2TexCoord = ComputeWFTexCoord(vin.TexCoord.y, vin.TextureIndex[3]); \
	vout.DepthVS = mul(vin.Position, WorldView).z; \
    vout.PositionWS = mul(vin.Position, World);

	//vout.PositionLightVS = mul(mul(vin.Position, World), LightView);
    //vout.DepthVS = vout.PositionCS.w; \

static const float4 SplitColors[8] = 
{
    float4 ( 1.5f, 0.0f, 0.0f, 1.0f ),
    float4 ( 0.0f, 1.5f, 0.0f, 1.0f ),
    float4 ( 0.0f, 0.0f, 5.5f, 1.0f ),
    float4 ( 1.5f, 0.0f, 5.5f, 1.0f ),
    float4 ( 1.5f, 1.5f, 0.0f, 1.0f ),
    float4 ( 1.0f, 1.0f, 1.0f, 1.0f ),
    float4 ( 0.0f, 1.0f, 5.5f, 1.0f ),
    float4 ( 0.5f, 3.5f, 0.75f, 1.0f )
};

// Vertex shader: basic.
VSOutput VSBasic(VSInput vin)
{
    VSOutput vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParams;
    
    return vout;
}


// Vertex shader: no fog.
VSOutputNoFog VSBasicNoFog(VSInput vin)
{
    VSOutputNoFog vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParamsNoFog;
    
    return vout;
}


// Vertex shader: vertex color.
VSOutput VSBasicVc(VSInputVc vin)
{
    VSOutput vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParams;
    
    vout.Diffuse *= vin.Color;
    
    return vout;
}


// Vertex shader: vertex color, no fog.
VSOutputNoFog VSBasicVcNoFog(VSInputVc vin)
{
    VSOutputNoFog vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParamsNoFog;
    
    vout.Diffuse *= vin.Color;
    
    return vout;
}


// Vertex shader: texture.
VSOutputTx VSBasicTx(VSInputTx vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParams;
    
	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: texture, no fog.
VSOutputTxNoFog VSBasicTxNoFog(VSInputTx vin)
{
    VSOutputTxNoFog vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParamsNoFog;
    
	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: texture + vertex color.
VSOutputTx VSBasicTxVc(VSInputTxVc vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParams;
    
    vout.Diffuse *= vin.Color;

	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: texture + vertex color, no fog.
VSOutputTxNoFog VSBasicTxVcNoFog(VSInputTxVc vin)
{
    VSOutputTxNoFog vout;
    
    CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
    SetCommonVSOutputParamsNoFog;
    
    vout.Diffuse *= vin.Color;

	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: vertex lighting.
VSOutput VSBasicVertexLighting(VSInputNm vin)
{
    VSOutput vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 3);
    SetCommonVSOutputParams;
    
    return vout;
}


// Vertex shader: vertex lighting + vertex color.
VSOutput VSBasicVertexLightingVc(VSInputNmVc vin)
{
    VSOutput vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 3);
    SetCommonVSOutputParams;
    
    vout.Diffuse *= vin.Color;
    
    return vout;
}


// Vertex shader: vertex lighting + texture.
VSOutputTx VSBasicVertexLightingTx(VSInputNmTx vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 3);
    SetCommonVSOutputParams;
    
	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: vertex lighting + texture + vertex color.
VSOutputTx VSBasicVertexLightingTxVc(VSInputNmTxVc vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 3);
    SetCommonVSOutputParams;
    
    vout.Diffuse *= vin.Color;
	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: one light.
VSOutput VSBasicOneLight(VSInputNm vin)
{
    VSOutput vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 1);
    SetCommonVSOutputParams;
    
    return vout;
}


// Vertex shader: one light + vertex color.
VSOutput VSBasicOneLightVc(VSInputNmVc vin)
{
    VSOutput vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 1);
    SetCommonVSOutputParams;
    
    vout.Diffuse *= vin.Color;
    
    return vout;
}


// Vertex shader: one light + texture.
VSOutputTx VSBasicOneLightTx(VSInputNmTx vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 1);
    SetCommonVSOutputParams;
    
	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: one light + texture + vertex color.
VSOutputTx VSBasicOneLightTxVc(VSInputNmTxVc vin)
{
    VSOutputTx vout;
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 1);
    SetCommonVSOutputParams;
    
    vout.Diffuse *= vin.Color;

	SetVoxelVSOutputParams;

    return vout;
}


// Vertex shader: pixel lighting.
VSOutputPixelLighting VSBasicPixelLighting(VSInputNm vin)
{
    VSOutputPixelLighting vout;
    
    CommonVSOutputPixelLighting cout = ComputeCommonVSOutputPixelLighting(vin.Position, vin.Normal);
    SetCommonVSOutputParamsPixelLighting;

    vout.Diffuse = float4(1, 1, 1, DiffuseColor.a);
    
    return vout;
}


// Vertex shader: pixel lighting + vertex color.
VSOutputPixelLighting VSBasicPixelLightingVc(VSInputNmVc vin)
{
    VSOutputPixelLighting vout;
    
    CommonVSOutputPixelLighting cout = ComputeCommonVSOutputPixelLighting(vin.Position, vin.Normal);
    SetCommonVSOutputParamsPixelLighting;
    
    vout.Diffuse.rgb = vin.Color.rgb;
    vout.Diffuse.a = vin.Color.a * DiffuseColor.a;
    
    return vout;
}


// Vertex shader: pixel lighting + texture.
VSOutputPixelLightingTx VSBasicPixelLightingTx(VSInputNmTx vin)
{
    VSOutputPixelLightingTx vout;
    
    CommonVSOutputPixelLighting cout = ComputeCommonVSOutputPixelLighting(vin.Position, vin.Normal);
    SetCommonVSOutputParamsPixelLighting;
    
    vout.Diffuse = float4(1, 1, 1, DiffuseColor.a);
    vout.TexCoord = vin.TexCoord;
    vout.TextureIndex = vin.TextureIndex;
    vout.AmbientOcclusionFactors = ComputeAmbientOcclusionFactors(vin.TextureIndex[1]);

    return vout;
}


// Vertex shader: pixel lighting + texture + vertex color.
VSOutputPixelLightingTx VSBasicPixelLightingTxVc(VSInputNmTxVc vin)
{
    VSOutputPixelLightingTx vout;
    
    CommonVSOutputPixelLighting cout = ComputeCommonVSOutputPixelLighting(vin.Position, vin.Normal);
    SetCommonVSOutputParamsPixelLighting;
    
    vout.Diffuse.rgb = vin.Color.rgb;
    vout.Diffuse.a = vin.Color.a * DiffuseColor.a;
    vout.TexCoord = vin.TexCoord;
    vout.TextureIndex = vin.TextureIndex;
    vout.AmbientOcclusionFactors = ComputeAmbientOcclusionFactors(vin.TextureIndex[1]);

    return vout;
}

// Pixel shader: basic.
float4 PSBasic(VSOutput pin) : SV_Target0
{
    float4 color = pin.Diffuse;
    
    ApplyFog(color, pin.Specular.w);
    
    return color;
}


// Pixel shader: no fog.
float4 PSBasicNoFog(VSOutputNoFog pin) : SV_Target0
{
    return pin.Diffuse;
}


// Pixel shader: texture.
float4 PSBasicTx(VSOutputTx pin) : SV_Target0
{
    float4 color = SAMPLE_TEXTURE_ARRAY(Texture, float3(pin.TexCoord, pin.TextureIndex[0])) * pin.Diffuse;
    
    ApplyFog(color, pin.Specular.w);
    
    return color;
}


// Pixel shader: texture, no fog.
float4 PSBasicTxNoFog(VSOutputTxNoFog pin) : SV_Target0
{
    return SAMPLE_TEXTURE_ARRAY(Texture, float3(pin.TexCoord, pin.TextureIndex[0])) * pin.Diffuse;
}


// Pixel shader: vertex lighting.
float4 PSBasicVertexLighting(VSOutput pin) : SV_Target0
{
    float4 color = pin.Diffuse;

    AddSpecular(color, pin.Specular.rgb);
    ApplyFog(color, pin.Specular.w);
    
    return color;
}


// Pixel shader: vertex lighting, no fog.
float4 PSBasicVertexLightingNoFog(VSOutput pin) : SV_Target0
{
    float4 color = pin.Diffuse;
    
    AddSpecular(color, pin.Specular.rgb);
    
    return color;
}


// see https://en.wikipedia.org/wiki/Alpha_compositing;
float4 blend(float4 src, float4 dst)
{
    float4 o;
    o.a = src.a + dst.a * (1 - src.a);
    o.rgb = (src.rgb * src.a + dst.rgb * dst.a * (1 - src.a)) / o.a;
    return o;
}

float4 blendWF(float4 src, float4 dst)
{
    float4 o;
	//o = src * dst;
	//o = lerp(dst, src, src.a);
    o = blend(src, dst);
	//o = dst * float4(1 - src.a, 1 - src.a, 1 - src.a, 1 - src.a);
	//o = src.a > 0 ? src : dst;
    return o;
}

// Calculates the shadow term using PCF
/*
    float CalcShadowTermPCF(float light_space_depth, float ndotl, float2 shadow_coord)
    {
        float shadow_term = 0;

       //float2 v_lerps = frac(ShadowMapSize * shadow_coord);

        float variableBias = clamp(0.001 * tan(acos(ndotl)), 0, DepthBias);

    	//safe to assume it's a square
        float size = 1 / ShadowMapSize.x;
    	
        float samples[4];
        samples[0] = (light_space_depth - variableBias < ShadowMap.Sample(ShadowMapSampler, shadow_coord).r);
        samples[1] = (light_space_depth - variableBias < ShadowMap.Sample(ShadowMapSampler, shadow_coord + float2(size, 0)).r);
        samples[2] = (light_space_depth - variableBias < ShadowMap.Sample(ShadowMapSampler, shadow_coord + float2(0, size)).r);
        samples[3] = (light_space_depth - variableBias < ShadowMap.Sample(ShadowMapSampler, shadow_coord + float2(size, size)).r);

        shadow_term = (samples[0] + samples[1] + samples[2] + samples[3]) / 4.0;
    	//shadow_term = lerp(lerp(samples[0],samples[1],v_lerps.x),lerp(samples[2],samples[3],v_lerps.x),v_lerps.y);

        return shadow_term;
    }
*/

float ComputerPercentageLit(int splitIndex, float4 shadowTexCoord, float dist)
{
	float shadowFactor = ShadowMapTexture.SampleCmpLevelZero(ShadowSampler, float3(shadowTexCoord.xy, splitIndex), dist);
	//float shadowDistance = SAMPLE_TEXTURE(ShadowMapTexture, shadowTexCoord).x;
	return shadowFactor;
}

// Pixel shader: vertex lighting + texture, no fog.
// https://github.com/walbourn/directx-sdk-samples/blob/master/CascadedShadowMaps11/RenderCascadeScene.hlsl
// https://github.com/walbourn/directx-sdk-samples/blob/master/CascadedShadowMaps11/CascadedShadowsManager.cpp
float4 PSBasicVertexLightingTxNoFog(VSOutputTx pin) : SV_Target0
{
	// TODO light is wrongly calculated
	// main problem is that :
	// - facing light + in shadow = ambient * diffuse * shadow
	// - not facing light + in shadow = ambient * shadow (= darker than above case but should not)
	// for surfaces not facing the light the shadow is accounted for twice
	// should both be = ambient
	// and a surface that does not face the light should be as dark even if the shadow is not taken into account
	// i.e. get rid of "visibility" and, instead, add diffuse if not facing light *or* in 

    //float4 color = float4(1,1,1,1);
    float4 color = SAMPLE_TEXTURE_ARRAY(Texture, float3(pin.TexCoord.x, -pin.TexCoord.y, pin.TextureIndex[0])); //* pin.Diffuse;

	// FIXME do we need to handle ambient occlusion when in the shadow or facing away from light ?
	color *= SampleAmbientOcclusionFactors(pin.AmbientOcclusionFactors, pin.TexCoord);

	if (WireframeMode != 0) {
		float4 wfColor1 = SAMPLE_TEXTURE(WireframeTexture, pin.WF1TexCoord);
		color = blendWF(wfColor1, color);
		float4 wfColor2 = SAMPLE_TEXTURE(WireframeTexture, pin.WF2TexCoord);
		color = blendWF(wfColor2, color);
	}

	// shadow cascade
	
	// Interval-based cascade selection uses a vector comparison and a dot product to determine the correct cacade.
	// The CascadeCount specifies the number of cascades.
	// The m_fCascadeFrustumsEyeSpaceDepths_data constrains the view frustum partitions.
	// After the comparison, the fComparison contains a value of 1 where the current pixel is larger than the barrier, and a value of 0 when the current cascade is smaller.
	// A dot product sums these values into an array index.

	float4 depthVS = float4(-pin.DepthVS, -pin.DepthVS, -pin.DepthVS, -pin.DepthVS);
	float4 splitDistances = float4(SplitDistances[0].x, SplitDistances[1].x, SplitDistances[2].x, SplitDistances[3].x);
	float4 comparison = (depthVS > splitDistances);
	float index = dot(float4( CascadeCount > 0, CascadeCount > 1, CascadeCount > 2, CascadeCount > 3), comparison);

    index = min(index, CascadeCount - 1);
    int splitIndex = (int)index;

	// FIXME pin.PositionWS is actually in WS
	float4 positionLightVS = mul(pin.PositionWS, LightViews[splitIndex]);
	float4 shadowTexCoord = positionLightVS * SplitScales[splitIndex];
	shadowTexCoord += SplitOffsets[splitIndex];
	float lightDistance = shadowTexCoord.z;

	//float4 PositionLS = mul(pin.PositionWS, LightView);
	//float2 shadowTexCoord = mad(float2(0.5f, -0.5f), PositionLS.xy / PositionLS.w, float2(0.5f, 0.5f));
    //float lightDistance = PositionLS.z / PositionLS.w;
	
	// shadow
	float shadowFactor = ComputerPercentageLit(splitIndex, shadowTexCoord, lightDistance);

	// pixel color
    float4 cout = color * AmbientColor;

    if (shadowFactor)
    {
        cout += color * pin.Diffuse * shadowFactor;
        AddSpecular(cout, pin.Specular.rgb);
    }

	if (VisualizeSplits)
	{
		cout *= SplitColors[splitIndex];
	}

    return cout;
}

// Pixel shader: vertex lighting + texture.
float4 PSBasicVertexLightingTx(VSOutputTx pin) : SV_Target0
{
    float4 color = PSBasicVertexLightingTxNoFog(pin);

    ApplyFog(color, pin.Specular.w);
    
    return color;
}

// Pixel shader: pixel lighting.
float4 PSBasicPixelLighting(VSOutputPixelLighting pin) : SV_Target0
{
    float4 color = pin.Diffuse;

    float3 eyeVector = normalize(EyePosition - pin.PositionWS.xyz);
    float3 worldNormal = normalize(pin.NormalWS);
    
    ColorPair lightResult = ComputeLights(eyeVector, worldNormal, 3);

    color.rgb *= lightResult.Diffuse;
    
    AddSpecular(color, lightResult.Specular);
    ApplyFog(color, pin.PositionWS.w);
    
    return color;
}


// Pixel shader: pixel lighting + texture.
float4 PSBasicPixelLightingTx(VSOutputPixelLightingTx pin) : SV_Target0
{
    float4 color = SAMPLE_TEXTURE_ARRAY(Texture, float3(pin.TexCoord, pin.TextureIndex[0]));
   
    float3 eyeVector = normalize(EyePosition - pin.PositionWS.xyz);
    float3 worldNormal = normalize(pin.NormalWS);
    
    ColorPair lightResult = ComputeLights(eyeVector, worldNormal, 3);
    
    float4 cout = color * AmbientColor;
	
    // FIXME seems wrong : why multiply by color.rgb ?
    cout.rgb += color.rgb * pin.Diffuse.rgb * lightResult.Diffuse;

    //cout.rgb *= lightResult.Diffuse;

	//cout.a = 1;

    AddSpecular(cout, lightResult.Specular);
    ApplyFog(cout, pin.PositionWS.w);
    
	//cout.a = 0.5;

    return cout;
}


// NOTE: The order of the techniques here are
// defined to match the indexing in VoxelEffect.cs.

TECHNIQUE(BasicEffect, VSBasic, PSBasic);
TECHNIQUE(BasicEffect_NoFog, VSBasicNoFog, PSBasicNoFog);
TECHNIQUE(BasicEffect_VertexColor, VSBasicVc, PSBasic);
TECHNIQUE(BasicEffect_VertexColor_NoFog, VSBasicVcNoFog, PSBasicNoFog);
TECHNIQUE(BasicEffect_Texture, VSBasicTx, PSBasicTx);
TECHNIQUE(BasicEffect_Texture_NoFog, VSBasicTxNoFog, PSBasicTxNoFog);
TECHNIQUE(BasicEffect_Texture_VertexColor, VSBasicTxVc, PSBasicTx);
TECHNIQUE(BasicEffect_Texture_VertexColor_NoFog, VSBasicTxVcNoFog, PSBasicTxNoFog);

TECHNIQUE(BasicEffect_VertexLighting, VSBasicVertexLighting, PSBasicVertexLighting);
TECHNIQUE(BasicEffect_VertexLighting_NoFog, VSBasicVertexLighting, PSBasicVertexLightingNoFog);
TECHNIQUE(BasicEffect_VertexLighting_VertexColor, VSBasicVertexLightingVc, PSBasicVertexLighting);
TECHNIQUE(BasicEffect_VertexLighting_VertexColor_NoFog, VSBasicVertexLightingVc, PSBasicVertexLightingNoFog);
TECHNIQUE(BasicEffect_VertexLighting_Texture, VSBasicVertexLightingTx, PSBasicVertexLightingTx);
TECHNIQUE(BasicEffect_VertexLighting_Texture_NoFog, VSBasicVertexLightingTx, PSBasicVertexLightingTxNoFog);
TECHNIQUE(BasicEffect_VertexLighting_Texture_VertexColor, VSBasicVertexLightingTxVc, PSBasicVertexLightingTx);
TECHNIQUE(BasicEffect_VertexLighting_Texture_VertexColor_NoFog, VSBasicVertexLightingTxVc, PSBasicVertexLightingTxNoFog);

TECHNIQUE(BasicEffect_OneLight, VSBasicOneLight, PSBasicVertexLighting);
TECHNIQUE(BasicEffect_OneLight_NoFog, VSBasicOneLight, PSBasicVertexLightingNoFog);
TECHNIQUE(BasicEffect_OneLight_VertexColor, VSBasicOneLightVc, PSBasicVertexLighting);
TECHNIQUE(BasicEffect_OneLight_VertexColor_NoFog, VSBasicOneLightVc, PSBasicVertexLightingNoFog);
TECHNIQUE(BasicEffect_OneLight_Texture, VSBasicOneLightTx, PSBasicVertexLightingTx);
TECHNIQUE(BasicEffect_OneLight_Texture_NoFog, VSBasicOneLightTx, PSBasicVertexLightingTxNoFog);
TECHNIQUE(BasicEffect_OneLight_Texture_VertexColor, VSBasicOneLightTxVc, PSBasicVertexLightingTx);
TECHNIQUE(BasicEffect_OneLight_Texture_VertexColor_NoFog, VSBasicOneLightTxVc, PSBasicVertexLightingTxNoFog);

TECHNIQUE(BasicEffect_PixelLighting, VSBasicPixelLighting, PSBasicPixelLighting);
TECHNIQUE(BasicEffect_PixelLighting_NoFog, VSBasicPixelLighting, PSBasicPixelLighting);
TECHNIQUE(BasicEffect_PixelLighting_VertexColor, VSBasicPixelLightingVc, PSBasicPixelLighting);
TECHNIQUE(BasicEffect_PixelLighting_VertexColor_NoFog, VSBasicPixelLightingVc, PSBasicPixelLighting);
TECHNIQUE(BasicEffect_PixelLighting_Texture, VSBasicPixelLightingTx, PSBasicPixelLightingTx);
TECHNIQUE(BasicEffect_PixelLighting_Texture_NoFog, VSBasicPixelLightingTx, PSBasicPixelLightingTx);
TECHNIQUE(BasicEffect_PixelLighting_Texture_VertexColor, VSBasicPixelLightingTxVc, PSBasicPixelLightingTx);
TECHNIQUE(BasicEffect_PixelLighting_Texture_VertexColor_NoFog, VSBasicPixelLightingTxVc, PSBasicPixelLightingTx);
