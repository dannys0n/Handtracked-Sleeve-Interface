Shader "ForearmLab/Projected Volume"
{
    Properties
    {
        [MainColor]_BaseColor("Tint",Color)=(.1,.65,.9,1)
        [MainTexture]_BaseMap("Base Texture",2D)="white"{}
        _Smoothness("Smoothness",Range(0,1))=.5
        _EmissionColor("Emission",Color)=(0,0,0,0)
        _Unlit("Unlit",Float)=0
    }
    SubShader
    {
        Tags{"Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SleeveDeformation.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            half4 _BaseColor,_EmissionColor;float4 _BaseMap_ST;float _Unlit,_Smoothness;
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input)
            {
                Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.world=SleeveDeformWorld(TransformObjectToWorld(input.positionOS.xyz));
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);output.color=input.color;
                output.positionCS=TransformWorldToHClip(output.world);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normal=normalize(cross(ddy(input.world),ddx(input.world)));
                Light light=GetMainLight();
                half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb*input.color.rgb;
                half3 diffuse=SampleSH(normal)+light.color*saturate(dot(normal,light.direction));
                float3 view=normalize(GetWorldSpaceViewDir(input.world));
                float specular=pow(saturate(dot(normal,normalize(light.direction+view))),lerp(8,128,_Smoothness))*.15;
                half3 lit=albedo*max(diffuse,.15)+light.color*specular+_EmissionColor.rgb;
                return half4(lerp(lit,albedo+_EmissionColor.rgb,saturate(_Unlit)),_BaseColor.a*input.color.a);
            }
            ENDHLSL
        }
    }
}
