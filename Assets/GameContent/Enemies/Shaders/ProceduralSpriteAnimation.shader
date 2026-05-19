Shader "Survivors/Procedural Sprite Animation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _Squash ("Squash", Range(-1, 1)) = 0
        _Stretch ("Stretch", Range(-1, 1)) = 0
        _VerticalOffset ("Vertical Offset", Float) = 0
        _HueShift ("Hue Shift", Range(-1, 1)) = 0
        _GlowColor ("Glow Color", Color) = (0,0.9,1,1)
        _GlowAmount ("Glow Amount", Range(0, 4)) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _FlashColor;
            fixed4 _GlowColor;
            fixed _FlashAmount;
            fixed _DissolveAmount;
            half _Squash;
            half _Stretch;
            half _VerticalOffset;
            half _HueShift;
            half _GlowAmount;

            float3 RgbToHsv(float3 color)
            {
                float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(color.bg, k.wz), float4(color.gb, k.xy), step(color.b, color.g));
                float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HsvToRgb(float3 color)
            {
                float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(color.xxx + k.xyz) * 6.0 - k.www);
                return color.z * lerp(k.xxx, saturate(p - k.xxx), color.y);
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                float4 vertex = input.vertex;
                float squashInfluence = (_Squash - _Stretch) * 0.7;
                float squashScale = max(0.35, 1.0 - squashInfluence);
                float stretchScale = max(0.35, 1.0 + squashInfluence);

                vertex.x *= stretchScale;
                vertex.y *= squashScale;
                vertex.y += _VerticalOffset;

                output.vertex = UnityObjectToClipPos(vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                output.localPos = input.texcoord;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.texcoord) * input.color;
                fixed alpha = color.a;
                fixed dissolveLine = step(_DissolveAmount, input.texcoord.y);
                alpha *= dissolveLine;

                float3 hsv = RgbToHsv(color.rgb);
                hsv.x = frac(hsv.x + _HueShift);
                color.rgb = HsvToRgb(hsv);
                color.rgb = lerp(color.rgb, _FlashColor.rgb, saturate(_FlashAmount));
                float glowStrength = saturate(_GlowAmount) * alpha;
                color.rgb = lerp(color.rgb, saturate(color.rgb + _GlowColor.rgb * 0.18), glowStrength);
                color.a = alpha;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
