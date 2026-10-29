Shader "ForearmLab/Outward Hologram Tile"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SleeveDeformation.hlsl"
            struct Attributes {float4 positionOS:POSITION;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformWorldToHClip(SleeveDeformWorld(TransformObjectToWorld(input.positionOS.xyz)));output.color=input.color*_Color;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);return input.color;}
            ENDHLSL
        }
    }
}
