/*
 * SceneFX Filmic — original ReShade shader by juanamores98 (MIT-0).
 *
 * Full-screen color grade that mirrors the SceneFX/LumenFX v2 look:
 * warmth (opposing R/B gain), saturation around luma, an S-curve contrast
 * with a soft shoulder, and a gentle gamma trim. The curves are the mod's
 * own; nothing here is derived from any third-party shader.
 *
 * Drop this file into reshade-shaders\Shaders\SceneFX\ and enable the
 * "SceneFXFilmic" technique.
 */

#include "ReShade.fxh"

uniform float Contrast <
	ui_type = "slider";
	ui_min = -1.0; ui_max = 1.0; ui_step = 0.01;
	ui_label = "Contrast";
	ui_tooltip = "S-curve expansion around mid gray with a soft shoulder.";
> = 0.30f;

uniform float Warmth <
	ui_type = "slider";
	ui_min = -1.0; ui_max = 1.0; ui_step = 0.01;
	ui_label = "Warmth";
	ui_tooltip = "Opposing red/blue gain. Negative cools the scene, positive warms it.";
> = 0.15f;

uniform float Saturation <
	ui_type = "slider";
	ui_min = 0.0; ui_max = 2.0; ui_step = 0.01;
	ui_label = "Saturation";
	ui_tooltip = "Distance from luma. 1.0 keeps the game as-is.";
> = 1.05f;

uniform float Brightness <
	ui_type = "slider";
	ui_min = -0.5; ui_max = 0.5; ui_step = 0.01;
	ui_label = "Brightness";
	ui_tooltip = "Additive trim applied before the contrast curve.";
> = 0.0f;

float3 SceneFXFilmicPS(float4 vpos : SV_Position, float2 texcoord : TEXCOORD) : SV_Target
{
	float3 c = tex2D(ReShade::BackBuffer, texcoord).rgb;

	// Brightness trim.
	c = saturate(c + Brightness);

	// Warmth: opposing red/blue channel gain.
	c.r *= 1.0f + 0.18f * Warmth;
	c.b *= 1.0f - 0.18f * Warmth;

	// Saturation around luma.
	float luma = dot(c, float3(0.299f, 0.587f, 0.114f));
	c = lerp(float3(luma, luma, luma), c, Saturation);

	// Contrast: symmetric expansion around mid gray...
	c = saturate(0.5f + (c - 0.5f) * (1.0f + Contrast));

	// ...with a soft shoulder above 0.8 so highlights roll off
	// instead of clipping hard.
	if (c.r > 0.8f)
	{
		float over = (c.r - 0.8f) / 0.2f;
		c.r = 0.8f + 0.2f * (1.0f - (1.0f - over) * (1.0f - over));
	}
	if (c.g > 0.8f)
	{
		float over = (c.g - 0.8f) / 0.2f;
		c.g = 0.8f + 0.2f * (1.0f - (1.0f - over) * (1.0f - over));
	}
	if (c.b > 0.8f)
	{
		float over = (c.b - 0.8f) / 0.2f;
		c.b = 0.8f + 0.2f * (1.0f - (1.0f - over) * (1.0f - over));
	}

	return c;
}

technique SceneFXFilmic <
	ui_label = "SceneFX Filmic";
	ui_tooltip = "SceneFX v2 full-screen grade: warmth, saturation, soft-shoulder contrast.";
>
{
	pass
	{
		VertexShader = PostProcessVS;
		PixelShader = SceneFXFilmicPS;
	}
}
