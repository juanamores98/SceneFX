/*
 * SceneFX Glow — original ReShade shader by juanamores98 (MIT-0).
 *
 * Single-pass ambient glow: bright-weighted samples of the back buffer are
 * accumulated and added back on top of the frame. Own tap layout and own
 * weighting; not derived from AmbientLight or any other third-party shader.
 *
 * Drop this file into reshade-shaders\Shaders\SceneFX\ and enable the
 * "SceneFXGlow" technique. Start with low strength: it saturates easily.
 */

#include "ReShade.fxh"

uniform float GlowStrength <
	ui_type = "slider";
	ui_min = 0.0; ui_max = 1.0; ui_step = 0.01;
	ui_label = "Glow strength";
> = 0.18f;

uniform float GlowThreshold <
	ui_type = "slider";
	ui_min = 0.0; ui_max = 1.0; ui_step = 0.01;
	ui_label = "Glow threshold";
	ui_tooltip = "Only samples brighter than this contribute.";
> = 0.62f;

uniform float GlowRadius <
	ui_type = "slider";
	ui_min = 1.0; ui_max = 12.0; ui_step = 0.5;
	ui_label = "Glow radius (pixels)";
> = 3.5f;

float3 SceneFXGlowPS(float4 vpos : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
	float3 base = tex2D(ReShade::BackBuffer, texcoord).rgb;

	// 12-tap ring layout: eight primary directions at r, four diagonals at 2r.
	float2 px = GlowRadius * ReShade::PixelSize;
	float2 taps[12] =
	{
		float2(1.0f, 0.0f), float2(-1.0f, 0.0f), float2(0.0f, 1.0f), float2(0.0f, -1.0f),
		float2(0.7f, 0.7f), float2(-0.7f, 0.7f), float2(0.7f, -0.7f), float2(-0.7f, -0.7f),
		float2(1.7f, 0.0f), float2(-1.7f, 0.0f), float2(0.0f, 1.7f), float2(0.0f, -1.7f)
	};

	float3 accumulated = float3(0.0f, 0.0f, 0.0f);
	float weightSum = 0.0f;

	for (int i = 0; i < 12; i++)
	{
		float3 s = tex2D(ReShade::BackBuffer, saturate(texcoord + taps[i] * px)).rgb;
		float luma = dot(s, float3(0.299f, 0.587f, 0.114f));
		float weight = saturate(luma - GlowThreshold);
		accumulated += s * weight;
		weightSum += weight;
	}

	float3 glow = weightSum > 0.0f ? accumulated / weightSum : float3(0.0f, 0.0f, 0.0f);
	return base + glow * GlowStrength;
}

technique SceneFXGlow <
	ui_label = "SceneFX Glow";
	ui_tooltip = "SceneFX v2 ambient glow. Keep strength low.";
>
{
	pass
	{
		VertexShader = PostProcessVS;
		PixelShader = SceneFXGlowPS;
	}
}
