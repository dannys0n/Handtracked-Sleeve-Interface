Shader "ForearmLab/Projected SDF Text"
{
    Properties
    {
        _MainTex("Font Atlas",2D)="white"{}
        _FaceColor("Face",Color)=(1,1,1,1)
        _FaceDilate("Dilate",Range(-1,1))=0
        _GradientScale("Gradient Scale",Float)=5
        _TextureWidth("Atlas Width",Float)=1024
        _TextureHeight("Atlas Height",Float)=1024
        _WeightNormal("Weight Normal",Float)=0
        _WeightBold("Weight Bold",Float)=.5
        _ScaleRatioA("Scale Ratio",Float)=1
        _CullMode("Cull",Float)=2
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Cull [_CullMode]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SleeveDeformation.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            half4 _FaceColor;float _FaceDilate;
            struct Attributes{float4 positionOS:POSITION;half4 color:COLOR;float4 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input)
            {
                Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformWorldToHClip(SleeveDeformWorld(TransformObjectToWorld(input.positionOS.xyz)));
                output.uv=input.uv.xy;output.color=input.color*_FaceColor;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float distance=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;
                float width=max(fwidth(distance),.001);
                half alpha=smoothstep(.5-_FaceDilate*.5-width,.5-_FaceDilate*.5+width,distance);
                return half4(input.color.rgb,input.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
