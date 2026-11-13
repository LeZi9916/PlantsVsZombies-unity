Shader "PlantsVsZombies/SpriteBatch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _ZWrite ("Depth write", Float) = 0
        [HideInInspector] _SrcBlend ("Source blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination blend", Float) = 10
        [HideInInspector] _SrcBlendAlpha ("Source alpha blend", Float) = 1
        [HideInInspector] _DstBlendAlpha ("Destination alpha blend", Float) = 10
    }
    SubShader
    {
        Tags { "RenderPipeline"="PvZRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "PvZSpriteBatch"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            HLSLPROGRAM
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment
            #include "Assets/Scripts/Rendering/Pipeline/ShaderLibrary/PvZPipelineCore.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 effects : TEXCOORD1;
                float4 highlight : TEXCOORD2;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 effects : TEXCOORD1;
                half3 highlight : TEXCOORD2;
            };
            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            CBUFFER_START(UnityPerMaterial)
                float _ZWrite;
                float _SrcBlend;
                float _DstBlend;
                float _SrcBlendAlpha;
                float _DstBlendAlpha;
            CBUFFER_END
            Varyings SpriteVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                output.effects = input.effects;
                output.highlight = input.highlight.rgb;
                return output;
            }
            half4 SpriteFragment(Varyings input) : SV_Target
            {
                half4 source = _MainTex.Sample(sampler_MainTex, input.uv);
                float mode = input.effects.z;
                if (mode > 0.5 && mode < 1.5)
                {
                    return half4(source.rgb * saturate(input.highlight), source.a * input.color.a);
                }

                half4 pixel = source * input.color;
                if (mode < 0.5)
                {
                    half gray = dot(pixel.rgb, half3(0.299, 0.587, 0.114));
                    pixel.rgb = lerp(pixel.rgb, gray.xxx, saturate(input.effects.x));
                    pixel.rgb = saturate(pixel.rgb * (1 + input.effects.y));
                    if (input.effects.w > 0.5)
                    {
                        // Keep the raw source hue and add before premultiplication. Saturating
                        // this sum would incorrectly darken translucent highlighted edges.
                        pixel.rgb += source.rgb * saturate(input.highlight);
                    }

                    pixel.a = saturate(pixel.a);
                    pixel.rgb *= pixel.a;
                }

                return pixel;
            }
            ENDHLSL
        }
    }
}
