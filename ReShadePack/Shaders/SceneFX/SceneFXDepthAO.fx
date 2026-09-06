/*
 * SceneFX Depth AO — original ReShade shader by juanamores98 (MIT-0).
 *
 * Screen-space halo ambient occlusion from the linearized depth buffer:
 * neighbors closer to the camera than the center pixel darken it. Own tap
 * layout and response; not derived from MXAO/qUINT or any other shader.
 *
 * Requires the game's depth buffer. If the effect looks inverted or noisy,
 * review ReShade's depth settings (DisplayDepth) before tuning here.
 */

#include "ReShade.fxh"

uniform float AOIntensity <
	ui_type = "slider";
	ui_min = 0.0; ui_max = 2.0; ui_step = 0.05;
	ui_label = "AO intensity";
> = 1.0f;

uniform float AORadius <
	ui_type = "slider";
	ui_min = 0.5; ui_max = 8.0; ui_step = 0.5;
	ui_label = "AO radius (pixels)";
> = 3.0f;

uniform float AOThreshold <
	ui_type = "slider";
	ui_min = 0.0; ui_max = 0.1; ui_step = 0.001;
	ui_label = "Depth threshold";
	ui_tooltip = "How much nearer a neighbor must be to count as occlusion.";
> = 0.004f;

float SceneFXDepthAOPS(float4 vpos : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
	float3 base = tex2D(ReShade::BackBuffer, texcoord).rgb;
	float center = ReShade::GetLinearizedDepth(texcoord);

	float2 px = AORadius * ReShade::PixelSize;
	float2 taps[8] =
	{
		float2(1.0f, 0.0f), float2(-1.0f, 0.0f), float2(0.0f, 1.0f), float2(0.0f, -1.0f),
		float2(0.7f, 0.7f), float2(-0.7f, 0.7f), float2(0.7f, -0.7f), float2(-0.7f, -0.7f)
	};

	float occlusion = 0.0f;
	for (int i = 0; i < 8; i++)
	{
		float neighbor = ReShade::GetLinearizedDepth(saturate(texcoord + taps[i] * px));
		float nearer = saturate((center - neighbor - AOThreshold) / (AOThreshold * 4.0f));
		occlusion += nearer;
	}

	float ao = 1.0f - saturate(occlusion / 8.0f) * saturate(AOIntensity);
	return base * ao;
}

technique SceneFXDepthAO <
	ui_label = "SceneFX Depth AO";
	ui_tooltip = "SceneFX v2 halo ambient occlusion. Needs a stable depth buffer.";
>
{
	pass
	{
		VertexShader = PostProcessVS;
		PixelShader = SceneFXDepthAOPS;
	}
}
