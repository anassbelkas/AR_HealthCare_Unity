Shader "Needle/AR Simulation/ShadowCatcher"
{
    Properties
    {
        [Toggle(_AR_SIMULATION_URP)] _ENABLE_AR_SIMULATION_URP ("Universal Renderpipeline", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
        }
        LOD 300

        Pass
        {
            Name "AR Shadow Matte"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            
            // custom AR Desktop
            #pragma shader_feature_local _AR_SIMULATION_URP

            // -------------------------------------
            // Material Keywords
            //#pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _ALPHAPREMULTIPLY_ON
    
            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
    
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
    
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing


            #define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
    
            #pragma vertex HiddenVertex
            #pragma fragment HiddenFragment
            
            #if _AR_SIMULATION_URP
            #include "ARShadowCatcherURP.cginc"
            #else
            #include "ARShadowCatcherBuiltIn.cginc"
            #endif // end urp
            
            
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Hidden/InternalErrorShader"
}