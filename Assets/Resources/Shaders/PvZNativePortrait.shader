Shader "Hidden/PvZReplica/NativePortrait"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Grayscale ("Grayscale", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float _Grayscale;
            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 pixel = tex2D(_MainTex, input.uv) * input.color;
                fixed gray = dot(pixel.rgb, fixed3(0.299, 0.587, 0.114));
                pixel.rgb = lerp(pixel.rgb, gray.xxx, saturate(_Grayscale));
                return pixel;
            }
            ENDCG
        }
    }
}
