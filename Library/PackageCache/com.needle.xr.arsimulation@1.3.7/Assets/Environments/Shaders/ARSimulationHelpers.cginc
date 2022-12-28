// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes {
    float4 positionOS : POSITION;
    float3 normal : NORMAL;
    float4 uv : TEXCOORD0;
};

struct Varyings {
    float4 positionHCS  : SV_POSITION;
    float4 uv : TEXCOORD0;
			
    float3 world : TEXCOORD1;
    float3 camObject : TEXCOORD2;
    float3 object : TEXCOORD3;
    float3 viewDir : TEXCOORD4;
};            

Varyings vert(Attributes IN) {
    Varyings OUT;
    OUT.positionHCS = UnityObjectToClipPos(IN.positionOS.xyz);
    OUT.uv = IN.uv;
    //OUT.normal = IN.normal;

    OUT.world = mul(unity_ObjectToWorld, float4(IN.positionOS.xyz,1)).xyz;
    OUT.object = IN.positionOS.xyz;
    float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - OUT.world);
    // viewDir -= OUT.world;
    // float3 viewDir_Local = TransformWorldToObject(float4(viewDir, 1)).xyz;;
    OUT.viewDir = viewDir;// TransformWorldToObject(float4(viewDir, 1)).xyz;
    OUT.camObject = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
    //if(CalculateClip(world, OUT.object, OUT.camObject) < 0)
    //	OUT.positionHCS = 0.f/0.f;
    return OUT;
}

half4 frag(Varyings IN) : SV_Target {
    #ifdef _ARSIMULATION_CLIPPING_ON
    clip(CalculateClip(IN.world, IN.object, IN.camObject, IN.viewDir));
    #endif
    return GetOutputColor(IN.uv.xy);
}