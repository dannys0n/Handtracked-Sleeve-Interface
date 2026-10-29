Shader "ForearmLab/Projected UI"
{
    Properties
    {
        [PerRendererData]_MainTex("Texture",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
        Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Back ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SleeveDeformation.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            half4 _Color,_TextureSampleAdd;float4 _ClipRect;
            struct A{float4 vertex:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 vertex:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A input){V o;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=TransformWorldToHClip(SleeveDeformWorld(TransformObjectToWorld(input.vertex.xyz)));o.color=input.color*_Color;o.uv=input.uv;o.local=input.vertex.xy;return o;}
            half4 Frag(V input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color=(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)+_TextureSampleAdd)*input.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=step(_ClipRect.x,input.local.x)*step(_ClipRect.y,input.local.y)*step(input.local.x,_ClipRect.z)*step(input.local.y,_ClipRect.w);
                #endif
                return color;
            }
            ENDHLSL
        }
    }
}
