// see https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch10.html

cbuffer ShaderData : register(b0)
{
    matrix WorldViewProjection;
};

struct VSOutput
{
    float4 PositionPS : SV_POSITION;
    uint SplitIndex : TEXTURE0;
};

struct GSOutput
{
    float4 PositionPS : SV_POSITION;
    uint RTIndex : SV_RenderTargetArrayIndex;
};

[maxvertexcount(3)]
void MainGS(triangle in VSOutput gin[3], inout TriangleStream<GSOutput> triStream)
{
/*
    GSOutput gout;

    gout.RTIndex = gin[0].SplitIndex;

    gout.PositionPS = gin[0].PositionPS;
    triStream.Append(gout);

    gout.PositionPS = gin[1].PositionPS;
    triStream.Append(gout);

    gout.PositionPS = gin[2].PositionPS;
    triStream.Append(gout);

    triStream.RestartStrip();
*/
}
