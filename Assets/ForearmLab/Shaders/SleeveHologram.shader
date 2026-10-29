Shader "ForearmLab/Sleeve Hologram"
{
    Properties
    {
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,.18)
        _Scanlines("Scanline strength", Range(0, .3)) = .04
        [HideInInspector] _SleeveRadius("Radius", Float) = .04
        [HideInInspector] _SleeveLength("Length", Float) = .22
        [HideInInspector] _CloseAmount("Close", Range(0,1)) = 0
        _CreaseColor("Fold creases", Color) = (.4,.9,1,1)
        _CreaseStrength("Crease highlight", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SleeveDeformation.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float3 barycentric:TEXCOORD1; float3 foldedOS:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; float3 barycentric:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CreaseColor;
                float _Scanlines, _SleeveRadius, _SleeveLength, _CreaseStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                // Morph the round open tube into broad planar triangles between staggered rings.
                // foldedOS is barycentrically interpolated within each coarse triangular facet.
                output.positionWS=SleeveDeformWorld(TransformObjectToWorld(input.positionOS.xyz));
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.barycentric=input.barycentric;
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input, bool frontFace:SV_IsFrontFace):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half scan=1-_Scanlines*(.5+.5*sin(input.uv.y*300-_Time.y));
                float3 normal=normalize(cross(ddy(input.positionWS),ddx(input.positionWS)));
                half faceLight=.32+.68*abs(dot(normal,normalize(float3(.3,.7,.6))));
                float3 edge=smoothstep(0,fwidth(input.barycentric)*1.15,input.barycentric);
                half crease=1-min(edge.x,min(edge.y,edge.z));
                half foldVisible=smoothstep(0,.18,_CloseAmount);
                half3 baseTint=_BaseColor.rgb*lerp(scan,1,foldVisible)*lerp(1,faceLight,foldVisible);
                half3 tint=lerp(baseTint,_CreaseColor.rgb,crease*foldVisible*_CreaseStrength);
                half alpha=_BaseColor.a;
                return half4(tint,alpha);
            }
            ENDHLSL
        }
    }
}
