#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
struct Attributes
{
    UNITY_VERTEX_INPUT_INSTANCE_ID
    float4 positionOS   : POSITION;
};
    
struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD1;
};
    
Varyings HiddenVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    
    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    
    return output;
}

half4 HiddenFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #else
    float4 shadowCoord = float4(0, 0, 0, 0);
    #endif
    
    
    Light mainLight = GetMainLight(shadowCoord);
    half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
    
    half3 attenuation = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, input.positionWS);
        attenuation += light.color * light.distanceAttenuation * light.shadowAttenuation;
    }
#endif

    //return half4(attenuatedLightColor, 1);
    half s = MainLightRealtimeShadow(shadowCoord);
    //return half4(0, 0, 0, 1-s);
    
    // return half4(0,0,0, 1 - sqrt(length(attenuation)));
    float w = 1 - attenuation.r;
    //w += s;
    //w = max(w, 0); // < prevent brightening of floor
    return half4(0, 0, 0, w);
}