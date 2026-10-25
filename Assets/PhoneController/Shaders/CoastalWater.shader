Shader "FlyHigh/Coastal Water"
{
    Properties
    {
        _ShallowColor("Shallow turquoise", Color) = (0.16,0.71,0.70,1)
        _DeepColor("Deep ocean", Color) = (0.045,0.30,0.48,1)
        _FoamColor("Sea foam", Color) = (0.85,0.96,0.92,1)
        _HorizonColor("Coastal horizon haze", Color) = (0.66,0.83,0.87,1)
        _ShoreZ("Shoreline position", Float) = 108
        _IslandMode("Surround island", Float) = 0
        _IslandRect("Island centre XZ and half size", Vector) = (0,0,60,75)
        _WaveScale("Wave strength", Range(0,1)) = 0.28
        _RippleBrightness("Ripple brightness", Range(0,0.05)) = 0.022
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "CoastalWater"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _FoamColor, _HorizonColor;
                float4 _IslandRect;
                float _ShoreZ, _WaveScale, _IslandMode, _RippleBrightness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half fog : TEXCOORD1; };
            float Shore(float x) { return _ShoreZ + 2.8*sin(x*0.035) + 1.2*sin(x*0.082); }
            float CoastDepth(float3 p)
            {
                float2 q=abs(p.xz-_IslandRect.xy)-_IslandRect.zw;
                float islandDistance=length(max(q,0))+min(max(q.x,q.y),0);
                return lerp(p.z-Shore(p.x),islandDistance-33,_IslandMode);
            }
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float fade = saturate(CoastDepth(p)*0.07);
                p.y += (sin(p.x*0.18 + p.z*0.25 + _Time.y*0.85)*0.55
                       + sin(p.z*0.39-p.x*0.12-_Time.y*1.1)*0.45)*_WaveScale*fade;
                o.positionWS = p; o.positionCS = TransformWorldToHClip(p);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float x=i.positionWS.x, z=i.positionWS.z, t=_Time.y;
                float depth=max(0,CoastDepth(i.positionWS)-2.0);
                float blend=1-exp(-depth*0.018);
                half3 color=lerp(_ShallowColor.rgb,_DeepColor.rgb,blend);
                float ripple=sin(z*1.25+x*0.24+t*1.3)*sin(x*0.52-z*0.31-t*0.7);
                float rippleFade=1-smoothstep(70,260,distance(i.positionWS,_WorldSpaceCameraPos));
                color += ripple*_RippleBrightness*rippleFade;
                float surf=sin(depth*1.05-t*1.5+sin(x*0.16)*0.5);
                float crest=smoothstep(0.80,0.98,surf)*exp(-depth*0.17);
                color=lerp(color,_FoamColor.rgb,crest*0.65);
                float3 n=normalize(float3(-0.04*cos(x*0.18+z*0.25+t*0.85),1,
                                         -0.05*cos(z*0.39-x*0.12-t*1.1)));
                Light sun=GetMainLight();
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float sparkle=pow(saturate(dot(n,normalize(sun.direction+view))),180);
                color += sun.color*sparkle*0.20;
                // Keep the ocean horizon soft even when the Scene view hides global fog.
                float horizon=smoothstep(180,620,distance(i.positionWS,_WorldSpaceCameraPos));
                color=lerp(color,_HorizonColor.rgb,horizon);
                half fog=InitializeInputDataFog(float4(i.positionWS,1),i.fog);
                return half4(MixFog(color,fog),1);
            }
            ENDHLSL
        }
    }
}
