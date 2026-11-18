Shader "PlantsVsZombies/SpriteInstanced"
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
            Name "PvZSpriteInstanced"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment
            #include "Assets/Scripts/Rendering/Pipeline/ShaderLibrary/PvZPipelineCore.hlsl"
            #include "Assets/Scripts/Rendering/Pipeline/ShaderLibrary/PvZSpriteColor.hlsl"
            struct InstanceData
            {
                uint vertexOffset;
                uint vertexCount;
                uint textureSlot;
                float depth;
                float4 color;
                float4 effects;
                float4 highlight;
            };
            StructuredBuffer<float4> _PvZVertices;
            StructuredBuffer<InstanceData> _PvZInstances;
            uint _PvZInstanceStart;
            int _PvZRunGeneration;
            int _PvZSceneGeneration;
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 effects : TEXCOORD1;
                half3 highlight : TEXCOORD2;
                nointerpolation uint textureSlot : TEXCOORD3;
                nointerpolation uint valid : TEXCOORD4;
            };
            Texture2D _PvZTexture0;
            SamplerState sampler_PvZTexture0;
            Texture2D _PvZTexture1;
            SamplerState sampler_PvZTexture1;
            Texture2D _PvZTexture2;
            SamplerState sampler_PvZTexture2;
            Texture2D _PvZTexture3;
            SamplerState sampler_PvZTexture3;
            Texture2D _PvZTexture4;
            SamplerState sampler_PvZTexture4;
            Texture2D _PvZTexture5;
            SamplerState sampler_PvZTexture5;
            Texture2D _PvZTexture6;
            SamplerState sampler_PvZTexture6;
            Texture2D _PvZTexture7;
            SamplerState sampler_PvZTexture7;
            CBUFFER_START(UnityPerMaterial)
                float _ZWrite;
                float _SrcBlend;
                float _DstBlend;
                float _SrcBlendAlpha;
                float _DstBlendAlpha;
            CBUFFER_END
            Varyings SpriteVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                if (_PvZRunGeneration != _PvZSceneGeneration)
                {
                    output.positionCS = float4(0, 0, -2, 1);
                    return output;
                }
                InstanceData instance = _PvZInstances[_PvZInstanceStart + instanceID];
                float4 vertex = _PvZVertices[instance.vertexOffset + min(vertexID, instance.vertexCount - 1)];
                output.positionCS = mul(UNITY_MATRIX_VP, float4(vertex.x, -vertex.y, instance.depth, 1));
                output.uv = vertex.zw;
                output.color = instance.color;
                output.effects = instance.effects;
                output.highlight = instance.highlight.rgb;
                output.textureSlot = instance.textureSlot;
                output.valid = 1;
                return output;
            }
            half4 SampleBank(uint slot, float2 uv)
            {
                float2 gradientX = ddx(uv);
                float2 gradientY = ddy(uv);
                switch (slot)
                {
                    case 0: return _PvZTexture0.SampleGrad(sampler_PvZTexture0, uv, gradientX, gradientY);
                    case 1: return _PvZTexture1.SampleGrad(sampler_PvZTexture1, uv, gradientX, gradientY);
                    case 2: return _PvZTexture2.SampleGrad(sampler_PvZTexture2, uv, gradientX, gradientY);
                    case 3: return _PvZTexture3.SampleGrad(sampler_PvZTexture3, uv, gradientX, gradientY);
                    case 4: return _PvZTexture4.SampleGrad(sampler_PvZTexture4, uv, gradientX, gradientY);
                    case 5: return _PvZTexture5.SampleGrad(sampler_PvZTexture5, uv, gradientX, gradientY);
                    case 6: return _PvZTexture6.SampleGrad(sampler_PvZTexture6, uv, gradientX, gradientY);
                    case 7: return _PvZTexture7.SampleGrad(sampler_PvZTexture7, uv, gradientX, gradientY);
                    default: return half4(0, 0, 0, 0);
                }
            }
            half4 SpriteFragment(Varyings input) : SV_Target
            {
                if (input.valid == 0)
                {
                    discard;
                }
                half4 source = SampleBank(input.textureSlot, input.uv);
                float mode = input.effects.z;
                if (mode > 0.5 && mode < 1.5)
                {
                    return half4(source.rgb * saturate(input.highlight), source.a * input.color.a);
                }

                // Original I, Zombie FILTER_EFFECT_WHITE preserves only the source alpha.
                if (mode > 3.5)
                {
                    half alpha = source.a * input.color.a;
                    return half4(input.color.rgb * alpha, alpha);
                }

                bool imitation = mode < 0.5 && input.effects.x > 1.5;
                if (imitation)
                {
                    source.rgb = ApplyImitaterFilter(source.rgb, input.effects.x);
                }
                half4 pixel = source * input.color;
                if (mode < 0.5)
                {
                    if (!imitation)
                    {
                        half gray = dot(pixel.rgb, half3(0.299, 0.587, 0.114));
                        pixel.rgb = lerp(pixel.rgb, gray.xxx, saturate(input.effects.x));
                    }
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
