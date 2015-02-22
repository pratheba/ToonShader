//-----------------------------------------------------------------------------
// CartoonEffect.fx
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

// The main texture applied to the object, and a sampler for reading it.


struct Light 
{
    float4 color;
    float3 position;
    float3 direction;
    float falloff;
    float range;
    float attenuation0;
    float attenuation1;
    float attenuation2;
    float innerConeAngle;
    float outerConeAngle;
    int type; // 0: directional; 1: point; 2: spot;
    
};
Light lights[12];
Light light;
int numberOfLights;


//shared scene parameters
shared float4x4 viewProjection;
shared float3 cameraPosition;
shared float4 ambientLightColor;


//the world paramter is not shared because it will
//change on every Draw() call
float4x4 world;
float4x4 worldForNormal;


//these material paramters are set per effect instance
float4 emissiveColor;
float4 diffuseColor;
float4 normalMapColor;
float4 specularColor;
float specularPower;
float specularIntensity;

sampler toonSampler;

//texture parameters can be used to set states in the 
//effect state pass code
texture2D toonTexture;

// Settings controlling the Toon lighting technique.
float ToonThresholds[2] = { 0.8, 0.4 };
float ToonBrightnessLevels[3] = { 1.3, 0.9, 0.5 };

// Is texturing enabled?
 bool TextureEnabled;


// Output structure for the vertex shader that applies lighting.
struct LightingVertexShaderOutputwithTexture
{
    float4 Position : POSITION;
    float2 TextureCoordinate : TEXCOORD0;
    float LightAmount : TEXCOORD1;
	float3 WorldPosition :TEXCOORD2;
	float3 WorldNormal : TEXCOORD3;
};


// Input structure for the Lambert and Toon pixel shaders.
struct LightingPixelShaderInputwithTexture
{
    float2 TextureCoordinate : TEXCOORD0;
    float LightAmount : TEXCOORD1;
	float3 WorldPosition : TEXCOORD2;
	float3 WorldNormal : TEXCOORD3;
};


// Vertex shader shared between the Lambert and Toon lighting techniques.
LightingVertexShaderOutputwithTexture LightingVertexShader(
                       float3 position : POSITION,
					   float lightAmount : TEXCOORD1,
					   float3 normal : NORMAL,
					   float2 texCoord : TEXCOORD0)
{
    LightingVertexShaderOutputwithTexture output;

	// generate the world-view-projection matrix
	float4x4 wvp = mul(world, viewProjection);

    // Apply camera matrices to the input position.
    output.Position = mul(float4(position, 1.0), wvp);
    
    // Calculate the worldNormal and the Position
    output.WorldNormal = mul(normal, worldForNormal);
	output.WorldNormal = normalize(output.WorldNormal);

	float4 worldPosition = mul(float4(position, 1.0), world);
	output.WorldPosition = worldPosition / worldPosition.w;

	// copy the tex coords to the interpolator
	output.TextureCoordinate = texCoord;
	
	 // Compute the overall lighting brightness.
	float3 lightVector = -light.direction;
    float3 directionToLight = normalize(lightVector);
    output.LightAmount = saturate( dot(directionToLight, output.WorldNormal));
    
    return output;
}

float4 CalculateSingleDirectionalLight(Light light, float3 worldPosition, float3 worldNormal, 
									  float4 compoundDiffuseColor )
{
     float3 lightVector = -light.direction;
     float3 directionToLight = normalize(lightVector);
     
     float diffuseIntensity = saturate( dot(directionToLight, worldNormal));
     float4 diffuse = diffuseIntensity * light.color * compoundDiffuseColor;
     
     return  diffuse;
}

// Pixel shader applies a simple Lambert shading algorithm.
float4 LambertPixelShader(LightingPixelShaderInputwithTexture input) : COLOR0
{
    float4 color = TextureEnabled? tex2D(toonSampler, input.TextureCoordinate) : 0;
    
    color.rgb *= input.LightAmount * light.color + ambientLightColor;
    
    return color;

   /* float4 compoundDiffuseColor = diffuseColor;
	compoundDiffuseColor *=  tex2D(toonSampler, input.TextureCoordinate) ;
	
    float4 color = 0;
	color += ambientLightColor + (input.LightAmount * light.color * compoundDiffuseColor);
	
	color.a = diffuseColor.a;
	return color;
	*/
}


// Pixel shader applies a cartoon shading algorithm.
float4 ToonPixelShader(LightingPixelShaderInputwithTexture input) : COLOR0
{
   
	float4 color =  TextureEnabled? tex2D(toonSampler, input.TextureCoordinate) : 0;
    
    float Toonlight;

    if (input.LightAmount > ToonThresholds[0])
        Toonlight = ToonBrightnessLevels[0];
    else if (input.LightAmount > ToonThresholds[1])
        Toonlight = ToonBrightnessLevels[1];
    else
        Toonlight = ToonBrightnessLevels[2];
                
	
	color.rgb *=   Toonlight;
    color.a = diffuseColor.a;
    
    return color;
}


// Output structure for the vertex shader that renders normal and depth information.
struct NormalDepthVertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD0;
};


// Alternative vertex shader outputs normal and depth values, which are then
// used as an input for the edge detection filter in PostprocessEffect.fx.
NormalDepthVertexShaderOutput NormalDepthVertexShader(
								float3 position : POSITION,
								float3 normal : NORMAL
								)
{
    NormalDepthVertexShaderOutput output;

	float4x4 wvp = mul(world, viewProjection);
    // Apply camera matrices to the input position.
    output.Position = mul(float4(position, 1.0), wvp);
    
    output.WorldNormal = mul(normal, worldForNormal);
	output.WorldNormal = normalize(output.WorldNormal);

	float4 WorldPosition = mul(float4(position,1.0), world);
	output.WorldPosition = WorldPosition / WorldPosition.w;

    // The output color holds the normal, scaled to fit into a 0 to 1 range.
    output.Color.rgb = (output.WorldNormal + 1) / 2;

    // The output alpha holds the depth, scaled to fit into a 0 to 1 range.
    output.Color.a = output.Position.z / output.Position.w;
    
    return output;    
}


// Simple pixel shader for rendering the normal and depth information.
float4 NormalDepthPixelShader(float4 color : COLOR0) : COLOR0
{
    return color;
}


// Technique draws the object using banded cartoon shading.
technique Toon
{
    pass P0
	{
       //set sampler states
        MagFilter[0] = LINEAR;
        MinFilter[0] = LINEAR;
        MipFilter[0] = LINEAR;
        AddressU[0] = CLAMP;
        AddressV[0] = CLAMP;
        MagFilter[1] = LINEAR;
        MinFilter[1] = LINEAR;
        MipFilter[1] = LINEAR;
        AddressU[1] = CLAMP;
        AddressV[1] = CLAMP;
        
        //set texture states (notice the '<' , '>' brackets)
        //as the texture state assigns a reference
        Texture[0] = <toonTexture>;

    
        VertexShader = compile vs_2_0 LightingVertexShader();
        PixelShader = compile ps_2_0 ToonPixelShader();
    }
}

// Technique draws the object using smooth Lambert shading.
technique Lambert
{
    pass P0
    {
		//set sampler states
        MagFilter[0] = LINEAR;
        MinFilter[0] = LINEAR;
        MipFilter[0] = LINEAR;
        AddressU[0] = CLAMP;
        AddressV[0] = CLAMP;
        MagFilter[1] = LINEAR;
        MinFilter[1] = LINEAR;
        MipFilter[1] = LINEAR;
        AddressU[1] = CLAMP;
        AddressV[1] = CLAMP;
        
        //set texture states (notice the '<' , '>' brackets)
        //as the texture state assigns a reference
        Texture[0] = <toonTexture>;

        VertexShader = compile vs_2_0 LightingVertexShader();
        PixelShader = compile ps_2_0 LambertPixelShader();
    }
}




// Technique draws the object as normal and depth values.
technique NormalDepth
{
    pass P0
    {
        VertexShader = compile vs_2_0 NormalDepthVertexShader();
        PixelShader = compile ps_2_0 NormalDepthPixelShader();
    }
}
