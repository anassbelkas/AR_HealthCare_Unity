 Shader "Needle/AR Simulation/Lightbake" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _LightingMap("Lighting", 2D) = "black" {}
        // _AlbedoIntensity("Albedo Intensity", Range(0,1)) = 1
        // _LightIntensity("Lighting Intensity", Range(0,1)) = 1
		// [Toggle] _ARSIMULATION_CLIPPING ("View Clipping", Float) = 1.0
    }
     
    HLSLINCLUDE
     
    sampler2D _MainTex;
    sampler2D _LightingMap;
    float4 _MainTex_ST;
    float _LightIntensity;
    float _AlbedoIntensity;
    float4 _Color;

	float4x4 _ARSimulation_WorldToEnvironmentBound[20];

	inline bool IsInBounds(float3 obj, float3 boundCenter, float3 boundExtends)
	{
		return 	obj.x >= boundCenter.x - boundExtends.x && obj.x <= boundCenter.x + boundExtends.x &&
				obj.y >= boundCenter.y - boundExtends.y && obj.y <= boundCenter.y + boundExtends.y &&
				obj.z >= boundCenter.z - boundExtends.z && obj.z <= boundCenter.z + boundExtends.z;
	}

	inline bool IsInBounds(float3 pos)
	{
		[unroll]
		for(int i = 0; i < 20; i++)
		{	
			float3 local = mul(_ARSimulation_WorldToEnvironmentBound[i], float4(pos,1)).xyz;
			if(local.x == 0 && local.y == 0 && local.z == 0) continue;
			if(local.x > -.5 && local.x < .5 && local.y > -.5 && local.y < .5 && local.z > -.5 && local.z < .5) 
				return true;
		}
		return false;
	}

    inline float CalculateClip(float3 world, float3 object, float3 camObject, float3 viewDir)
    {
		if(IsInBounds(world)) return 1;

    	//obj.y = cam.y = .1;
    	float dw = 1 - dot(viewDir, object) * 3;
    	// return dw;
		const float floor = 1 - object.y * .01;
    	dw *= saturate(1-floor);
		// return dw;
    	// return dw;
    	float inside = length(camObject) * .2;
    	inside = saturate(inside);
    	//return inside;
    	return lerp(.01, dw, inside);
    }

    inline float4 GetOutputColor(float2 uv)
    {
        float4 output = float4(0, 0, 0, 1);
        output.rgb = lerp(float3(1,1,1), tex2D(_MainTex, uv.xy).rgb, _AlbedoIntensity);

        float3 emission = tex2D(_LightingMap, uv.xy).rgb;
        emission = lerp(dot(emission, float3(0.3, 0.59, 0.11)), emission, _AlbedoIntensity);
        output.rgb = output.rgb * (1 - _LightIntensity) + output.rgb * (emission * _LightIntensity);

        return output * _Color;
    }
     
    ENDHLSL
     
    SubShader
    {
        Tags {
            "RenderType" = "Opaque" 
        }
		
        Pass
        {
            Cull Back
            Fog { Mode Off }
            AlphaTest Off
            Blend Off
			Offset 2,0
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			//#pragma multi_compile __ _ARSIMULATION_CLIPPING_ON
            #include "UnityCG.cginc" 
			#include "ARSimulationHelpers.cginc"
            ENDHLSL
        }

		Pass {
			Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
			Cull Back
			Offset 2,0

			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			//#pragma multi_compile __ _ARSIMULATION_CLIPPING_ON
            #include "UnityCG.cginc"
			#include "ARSimulationHelpers.cginc"
            ENDHLSL 
		}
    }

    Fallback Off
}
