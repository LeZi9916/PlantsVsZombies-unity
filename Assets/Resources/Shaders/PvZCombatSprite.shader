Shader "PlantsVsZombies/CombatSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Grayscale ("Grayscale", Float) = 0
        _Additive ("Native textured hit flash", Float) = 0
        _NativeAddColor ("Native extra additive color", Color) = (0,0,0,0)
        _ProducerAdditive ("Native producer source brightness", Float) = 0
        [HideInInspector] _ClipRect ("Screen pixel clipping bounds", Vector) = (0,0,0,0)
        [HideInInspector] _ClipEnabled ("Screen pixel clipping enabled", Float) = 0
        [HideInInspector] _ZWrite ("Depth write", Float) = 0
        [HideInInspector] _ZTest ("Depth comparison", Float) = 4
        [HideInInspector] _RenderMode ("Render mode", Float) = 0
        [HideInInspector] _SrcBlend ("Source blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination blend", Float) = 10
        [HideInInspector] _SrcBlendAlpha ("Source alpha blend", Float) = 1
        [HideInInspector] _DstBlendAlpha ("Destination alpha blend", Float) = 10
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "PvZSprite"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            HLSLPROGRAM
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float2 screenPixel : TEXCOORD1;
            };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Grayscale;
                float _Additive;
                float _ProducerAdditive;
                float _RenderMode;
                float4 _NativeAddColor;
                float4 _ClipRect;
                float _ClipEnabled;
            CBUFFER_END
            Varyings SpriteVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.screenPixel = float2(input.positionOS.x, -input.positionOS.y);
                return output;
            }
            half4 SpriteFragment(Varyings input) : SV_Target
            {
                if (_ClipEnabled > 0.5)
                {
                    clip(input.screenPixel - _ClipRect.xy);
                    clip(_ClipRect.zw - input.screenPixel);
                }

                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (_RenderMode > 0.5 && _RenderMode < 1.5)
                {
                    source.rgb *= saturate(_NativeAddColor.rgb + _Additive.xxx);
                    source.a *= input.color.a;
                    return source;
                }
                half4 pixel = source * input.color;
                if (_RenderMode < 0.5)
                {
                    half gray = dot(pixel.rgb, half3(0.299, 0.587, 0.114));
                    pixel.rgb = lerp(pixel.rgb, gray.xxx, saturate(_Grayscale));
                    pixel.rgb = saturate(pixel.rgb * (1 + _ProducerAdditive));
                }
                return pixel;
            }
            ENDHLSL
        }
    }
}
