Shader "Hidden/PvZReplica/CombatSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Grayscale ("Grayscale", Float) = 0
        _Additive ("Native textured hit flash", Float) = 0
        _NativeAddColor ("Native extra additive color", Color) = (0,0,0,0)
        _ProducerAdditive ("Native producer source brightness", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
        struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
        sampler2D _MainTex;
        float _Grayscale, _Additive, _ProducerAdditive;
        float4 _NativeAddColor;
        v2f vert(appdata input)
        {
            v2f output;
            output.position = UnityObjectToClipPos(input.vertex);
            output.uv = input.uv;
            output.color = input.color;
            return output;
        }
        fixed4 fragBase(v2f input) : SV_Target
        {
            fixed4 pixel = tex2D(_MainTex, input.uv) * input.color;
            fixed gray = dot(pixel.rgb, fixed3(0.299, 0.587, 0.114));
            pixel.rgb = lerp(pixel.rgb, gray.xxx, saturate(_Grayscale));
            pixel.rgb = saturate(pixel.rgb * (1 + _ProducerAdditive));
            return pixel;
        }
        fixed4 fragExtra(v2f input) : SV_Target
        {
            fixed4 pixel = tex2D(_MainTex, input.uv);
            // Original DRAW_MODE_ADDITIVE redraws the source texture in the extra color.
            // A flat white bias erases the dark line work and is not the native effect.
            pixel.rgb *= saturate(_NativeAddColor.rgb + _Additive.xxx);
            pixel.a *= input.color.a;
            return pixel;
        }
        fixed4 fragSpriteAdd(v2f input) : SV_Target
        { return tex2D(_MainTex, input.uv) * input.color; }
        fixed4 fragComposite(v2f input) : SV_Target
        { return tex2D(_MainTex, input.uv) * input.color; }
        ENDCG
        Pass
        {
            Name "NativeBase"
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBase
            ENDCG
        }
        Pass
        {
            Name "NativeExtraAdditive"
            Blend SrcAlpha One, Zero One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragExtra
            ENDCG
        }
        Pass
        {
            Name "NativeParticleAdditive"
            Blend SrcAlpha One, Zero One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragSpriteAdd
            ENDCG
        }
        Pass
        {
            Name "NativeSurfaceComposite"
            // Native surfaces contain premultiplied color from the base + ADD draws.
            // Preserve its color once, including additive particles on transparent pixels.
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragComposite
            ENDCG
        }
    }
}
