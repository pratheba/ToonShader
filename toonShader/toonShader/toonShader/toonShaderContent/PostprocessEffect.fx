//-----------------------------------------------------------------------------
// PostprocessEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------


float EdgeWidth = 1;
float EdgeIntensity = 1;


float NormalThreshold = 0.5;
float DepthThreshold = 0.1;


float NormalSensitivity = 1;
float DepthSensitivity = 10;

float SketchThreshold = 0.1;
float SketchBrightness = 0.333;

float2 SketchJitter;
float2 ScreenResolution;

texture2D SceneTexture;
texture2D NormalDepthTexture;
texture2D SketchTexture;


sampler SceneSampler;
sampler NormalDepthSampler;
sampler SketchSampler;


float4 PixelShaderFunction(float2 texCoord : TEXCOORD0, uniform bool applyEdgeDetect,
                                                        uniform bool applySketch,
                                                        uniform bool sketchInColor) : COLOR0
{
     // Look up the original color from the main scene.
    float3 scene = tex2D(SceneSampler, texCoord);
    
    // Apply the sketch effect?
    if (applySketch)
    {
        // Adjust the scene color to remove very dark values and increase the contrast.
        float3 saturatedScene = saturate((scene - SketchThreshold) * 2);
        
        // Look up into the sketch pattern overlay texture.
        float3 sketchPattern = tex2D(SketchSampler, texCoord + SketchJitter);
    
        float3 negativeSketch = (1 - saturatedScene) * (1 - sketchPattern);
        
        // Convert the result into a positive color space greyscale value.
        float sketchResult = dot(1 - negativeSketch, SketchBrightness);
        
        // Apply the sketch result to the main scene color.
        if (sketchInColor)
            scene *= sketchResult;
        else
            scene = sketchResult;
    }
    
    // Apply the edge detection filter?
    if (applyEdgeDetect)
    {
        // Look up four values from the normal/depth texture, offset along the
        // four diagonals from the pixel we are currently shading.
        float2 edgeOffset = EdgeWidth / ScreenResolution;
        
        float4 n1 = tex2D(NormalDepthSampler, texCoord + float2(-1, -1) * edgeOffset);
        float4 n2 = tex2D(NormalDepthSampler, texCoord + float2( 1,  1) * edgeOffset);
        float4 n3 = tex2D(NormalDepthSampler, texCoord + float2(-1,  1) * edgeOffset);
        float4 n4 = tex2D(NormalDepthSampler, texCoord + float2( 1, -1) * edgeOffset);

        // Work out how much the normal and depth values are changing.
        float4 diagonalDelta = abs(n1 - n2) + abs(n3 - n4);

        float normalDelta = dot(diagonalDelta.xyz, 1);
        float depthDelta = diagonalDelta.w;
        
        // Filter out very small changes, in order to produce nice clean results.
        normalDelta = saturate((normalDelta - NormalThreshold) * NormalSensitivity);
        depthDelta = saturate((depthDelta - DepthThreshold) * DepthSensitivity);

        // Does this pixel lie on an edge?
        float edgeAmount = saturate(normalDelta + depthDelta) * EdgeIntensity;
        
        // Apply the edge detection result to the main scene color.
        scene *= (1 - edgeAmount);
    }

    return float4(scene, 1);
}


	// Compile the pixel shader for doing edge detection without any sketch effect.
technique EdgeDetect
{
    pass P0
    {
		 MinFilter[0] = LINEAR;
         MagFilter[0] = LINEAR;
	 	 AddressU[0] = CLAMP;
		 AddressV[0] = CLAMP;
		 MinFilter[1] = LINEAR;
         MagFilter[1] = LINEAR;
	 	 AddressU[1] = CLAMP;
		 AddressV[1] = CLAMP;
		 AddressU[2] = Wrap;
		 AddressV[2] = Wrap;
		 Texture[0] = <SceneTexture>;
		 Texture[1] = <NormalDepthTexture>;
		 Texture[2] = <SketchTexture>;
        PixelShader = compile ps_2_0 PixelShaderFunction(true, false, false);
    }
}

// Compile the pixel shader for doing edge detection with a monochrome sketch effect.
technique EdgeDetectMonoSketch
{
    pass P0
    {
		 MinFilter[0] = LINEAR;
         MagFilter[0] = LINEAR;
	 	 AddressU[0] = CLAMP;
		 AddressV[0] = CLAMP;
		 MinFilter[1] = LINEAR;
         MagFilter[1] = LINEAR;
	 	 AddressU[1] = CLAMP;
		 AddressV[1] = CLAMP;
		 AddressU[2] = Wrap;
		 AddressV[2] = Wrap;
		 Texture[0] = <SceneTexture>;
		 Texture[1] = <NormalDepthTexture>;
		 Texture[2] = <SketchTexture>;
        PixelShader = compile ps_2_0 PixelShaderFunction(true, true, false);
    }
}

// Compile the pixel shader for doing edge detection with a colored sketch effect.
technique EdgeDetectColorSketch
{
    pass P0
    {
		 MinFilter[0] = LINEAR;
         MagFilter[0] = LINEAR;
	 	 AddressU[0] = CLAMP;
		 AddressV[0] = CLAMP;
		 MinFilter[1] = LINEAR;
         MagFilter[1] = LINEAR;
	 	 AddressU[1] = CLAMP;
		 AddressV[1] = CLAMP;
		 AddressU[2] = Wrap;
		 AddressV[2] = Wrap;
		 Texture[0] = <SceneTexture>;
		 Texture[1] = <NormalDepthTexture>;
		 Texture[2] = <SketchTexture>;
        PixelShader = compile ps_2_0 PixelShaderFunction(true, true, true);
    }
}

// Compile the pixel shader for doing a monochrome sketch effect without edge detection.
technique MonoSketch
{
    pass P0
    {
		 MinFilter[0] = LINEAR;
         MagFilter[0] = LINEAR;
	 	 AddressU[0] = CLAMP;
		 AddressV[0] = CLAMP;
		 MinFilter[1] = LINEAR;
         MagFilter[1] = LINEAR;
	 	 AddressU[1] = CLAMP;
		 AddressV[1] = CLAMP;
		 AddressU[2] = Wrap;
		 AddressV[2] = Wrap;
		 Texture[0] = <SceneTexture>;
		 Texture[1] = <NormalDepthTexture>;
		 Texture[2] = <SketchTexture>;
        PixelShader = compile ps_2_0 PixelShaderFunction(false, true, false);
    }
}

// Compile the pixel shader for doing a colored sketch effect without edge detection.
technique ColorSketch
{
    pass P0
    {
		 MinFilter[0] = LINEAR;
         MagFilter[0] = LINEAR;
	 	 AddressU[0] = CLAMP;
		 AddressV[0] = CLAMP;
		 MinFilter[1] = LINEAR;
         MagFilter[1] = LINEAR;
	 	 AddressU[1] = CLAMP;
		 AddressV[1] = CLAMP;
		 AddressU[2] = Wrap;
		 AddressV[2] = Wrap;
		 Texture[0] = <SceneTexture>;
		 Texture[1] = <NormalDepthTexture>;
		 Texture[2] = <SketchTexture>;
        PixelShader = compile ps_2_0 PixelShaderFunction(false, true, true);
    }
}


