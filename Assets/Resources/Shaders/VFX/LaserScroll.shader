Shader "Survivors/VFX/Laser Scroll"
{
    Properties
    {
        _MainTex ("Laser Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _ScrollSpeed ("Scroll Speed", Float) = 2.5
        _Intensity ("Intensity", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "LaserScroll"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ScrollSpeed;
            float _Intensity;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.uv.x += _Time.y * _ScrollSpeed;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                color.rgb *= _Intensity;
                return color;
            }
            ENDCG
        }
    }

    FallBack Off
}
