Shader "Custom/ColorSwappableTex"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _SwapMap("Color Swap Map", 2D) = "white" {}
        _SwapIdx("Swap Index", Int) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SamplerState basemap_point_clamp_sampler;
            
            TEXTURE2D(_SwapMap);
            half4 _SwapMap_TexelSize;
            SamplerState swapmap_point_clamp_sampler;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                int _SwapIdx;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, basemap_point_clamp_sampler, IN.uv);
                half4 colorScaled = color * (half)255.0;
                uint3 color32 = uint3((uint)round(colorScaled.x), (uint)round(colorScaled.y), (uint)round(colorScaled.z));
                uint colorVal = (color32.r << 16) | (color32.g << 8) | color32.b;
                uint colorMod = colorVal % (uint)_SwapMap_TexelSize.w;
                half2 sampleUV = half2((half)_SwapIdx + (half)0.5, (half)colorMod + half(0.5)) * _SwapMap_TexelSize.xy;
                half3 mappedColor = SAMPLE_TEXTURE2D(_SwapMap, swapmap_point_clamp_sampler, sampleUV);
                //half4 outColor = half4((half)colorMod / 3.0, (half)colorMod / 3.0, (half)colorMod / 3.0, color.a);
                return half4(mappedColor, color.a);
            }
            ENDHLSL
        }
    }
}
