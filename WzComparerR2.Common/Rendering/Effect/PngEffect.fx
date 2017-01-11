#include "Macros.fxh"

DECLARE_TEXTURE(Texture, 0);

BEGIN_CONSTANTS

  float4 mixedColor;
  float clipAlpha;

  MATRIX_CONSTANTS

END_CONSTANTS

struct VSOutput
{
	float4 position		: SV_Position;
	float4 color		: COLOR0;
	float2 texCoord		: TEXCOORD0;
};

float4 RGBAtoNonPremultiplied(float4 input)
{
	if (input.a <= 1) {
		input.rgb /= input.a;
	}
	return input;
}

float4 Blend(float4 background, float4 premultipliedColor)
{
	return float4(background.rgb * (1 - premultipliedColor.a) + premultipliedColor.rgb, 1);
}

float4 PS(VSOutput input) : SV_Target0
{
	float4 color = SAMPLE_TEXTURE(Texture, input.texCoord) * input.color;
	return RGBAtoNonPremultiplied(color);
}

float4 PS_AlphaTest(VSOutput input) : SV_Target0
{
	float4 color = SAMPLE_TEXTURE(Texture, input.texCoord) * input.color;
	clip(color.a <= clipAlpha ? -1 : 1);
	return Blend(mixedColor, color);
}

TECHNIQUE_SB(tech0, PS);
TECHNIQUE_SB(tech1, PS_AlphaTest);
