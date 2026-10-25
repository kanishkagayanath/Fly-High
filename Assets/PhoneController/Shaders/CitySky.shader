Shader "FlyHigh/City Sky"
{
    Properties
    {
        _Zenith("Sky overhead",Color)=(0.12,0.42,0.72,1)
        _Horizon("Horizon haze",Color)=(0.7,0.83,0.88,1)
        _SunColor("Sun colour",Color)=(1,0.85,0.6,1)
        _SunDirection("Sun direction",Vector)=(0,1,0,0)
        _MoonDirection("Moon direction",Vector)=(0,1,0,0)
        _Night("Night amount",Range(0,1))=0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith,_Horizon,_SunColor;
                float4 _SunDirection,_MoonDirection;
                float _Night;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.direction=TransformObjectToWorldDir(v.positionOS.xyz);return o;
            }
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            half4 Frag(Varyings i):SV_Target
            {
                float3 d=normalize(i.direction);
                float height=pow(saturate(d.y),0.6);
                half3 color=lerp(_Horizon.rgb,_Zenith.rgb,height);
                color*=lerp(0.65,1,smoothstep(-0.3,0.02,d.y));
                float sun=saturate(dot(d,normalize(_SunDirection.xyz)));
                color+=_SunColor.rgb*(smoothstep(0.99955,0.99985,sun)*3+pow(sun,80)*0.25)*(1-_Night);
                float moon=saturate(dot(d,normalize(_MoonDirection.xyz)));
                color+=half3(0.65,0.78,1)*(smoothstep(0.9995,0.99985,moon)*1.5+pow(moon,130)*0.13)*_Night;
                float2 uv=float2(atan2(d.z,d.x)*0.15915494+0.5,asin(clamp(d.y,-1,1))*0.31830989+0.5)*float2(480,240);
                float random=Hash(floor(uv));
                float star=(1-smoothstep(0.02,0.12,length(frac(uv)-0.5)))*step(0.9965,random)*smoothstep(0.03,0.25,d.y);
                color+=star*_Night*half3(0.8,0.88,1)*1.6;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
