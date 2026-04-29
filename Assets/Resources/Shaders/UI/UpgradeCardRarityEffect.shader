Shader "UI/Upgrade Card Rarity Effect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Rarity ("Rarity", Float) = 0
        _EffectIntensity ("Effect Intensity", Range(0, 2)) = 0.5
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.5
        _PrimaryColor ("Primary Color", Color) = (1,1,1,1)
        _SecondaryColor ("Secondary Color", Color) = (0.2,0.2,0.2,1)
        _AccentColor ("Accent Color", Color) = (1,1,1,1)
        _FlowSpeed ("Flow Speed", Range(0, 4)) = 0.45
        _BorderWidth ("Border Width", Range(0.01, 0.3)) = 0.075
        _BorderGlow ("Border Glow", Range(0, 3)) = 0.45
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 0.9
        _LayerRole ("Layer Role", Range(0, 3)) = 0
        _AlphaScale ("Alpha Scale", Range(0, 1)) = 1
        _PatternIntensity ("Surface Detail", Range(0, 1.5)) = 0.18
        _SweepIntensity ("Sweep Intensity", Range(0, 2)) = 0.35
        _PulseIntensity ("Pulse Intensity", Range(0, 2)) = 0.25

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Rarity;
            float _EffectIntensity;
            float _GlowIntensity;
            fixed4 _PrimaryColor;
            fixed4 _SecondaryColor;
            fixed4 _AccentColor;
            float _FlowSpeed;
            float _BorderWidth;
            float _BorderGlow;
            float _PulseSpeed;
            float _LayerRole;
            float _AlphaScale;
            float _PatternIntensity;
            float _SweepIntensity;
            float _PulseIntensity;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            inline float RoleMask(float role, float expectedRole)
            {
                return 1.0 - step(0.5, abs(role - expectedRole));
            }

            inline float SoftBand(float value, float center, float width, float softness)
            {
                float distanceToCenter = abs(value - center);
                return 1.0 - smoothstep(width, width + softness, distanceToCenter);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 spriteColor = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float2 uv = input.texcoord;
                float2 center = uv - 0.5;
                float edge = max(abs(center.x), abs(center.y)) * 2.0;
                float radial = saturate(1.0 - length(center) * 1.55);
                float effect = clamp(_EffectIntensity, 0.0, 1.6);
                float glow = max(0.0, _GlowIntensity);
                float borderGlow = max(0.0, _BorderGlow);
                float time = _Time.y;

                float verticalGradient = saturate(0.18 + uv.y * 0.72 + radial * 0.22);
                float diagonalGradient = saturate((uv.x + uv.y) * 0.5);
                fixed3 surfaceColor = lerp(_SecondaryColor.rgb, _PrimaryColor.rgb, verticalGradient);
                surfaceColor = lerp(surfaceColor, _AccentColor.rgb, saturate(diagonalGradient * 0.08 * effect));

                float waveA = sin((uv.x * 1.45 + uv.y * 0.75 + time * _FlowSpeed * 0.10) * 6.28318);
                float waveB = sin((uv.x * -0.65 + uv.y * 1.35 - time * _FlowSpeed * 0.08) * 6.28318);
                float ambientDetail = saturate((waveA * 0.5 + waveB * 0.5 + 1.0) * 0.5);
                ambientDetail = pow(ambientDetail, 2.8) * saturate(_PatternIntensity);

                float borderWidth = max(0.01, _BorderWidth);
                float borderOuter = smoothstep(1.0 - borderWidth - 0.035, 1.0 - borderWidth, edge);
                float borderCutoff = 1.0 - smoothstep(0.985, 1.0, edge);
                float borderRing = saturate(borderOuter * borderCutoff);
                float innerLine = smoothstep(1.0 - borderWidth * 2.7, 1.0 - borderWidth * 2.0, edge)
                    * (1.0 - smoothstep(1.0 - borderWidth * 1.55, 1.0 - borderWidth * 1.05, edge));
                innerLine = saturate(innerLine);

                float sweepPosition = frac(time * _FlowSpeed * 0.18 + _Rarity * 0.09);
                float sweepAxis = saturate((uv.x + uv.y) * 0.5);
                float sweep = SoftBand(sweepAxis, sweepPosition, 0.055, 0.11) * _SweepIntensity;
                float borderSweep = sweep * saturate(borderRing + innerLine * 0.55);

                float pulseWave = saturate(sin(time * _PulseSpeed * 6.28318) * 0.5 + 0.5);
                float pulse = smoothstep(0.35, 1.0, pulseWave) * _PulseIntensity;
                float cornerGlow = smoothstep(0.75, 1.0, abs(center.x) * 2.0)
                    * smoothstep(0.75, 1.0, abs(center.y) * 2.0);
                float edgeHalo = smoothstep(0.56, 1.0, edge) * (1.0 - smoothstep(0.98, 1.0, edge));

                float role = floor(_LayerRole + 0.5);
                float cardRole = RoleMask(role, 0.0);
                float backgroundRole = RoleMask(role, 1.0);
                float borderRole = RoleMask(role, 2.0);
                float glowRole = RoleMask(role, 3.0);
                float layerAlpha = saturate(_AlphaScale) * spriteColor.a;

                fixed3 cardColor = lerp(spriteColor.rgb, spriteColor.rgb * (surfaceColor * 0.74 + 0.26), saturate(effect * 0.18));
                fixed3 backgroundColor = surfaceColor
                    + _AccentColor.rgb * ambientDetail * 0.22
                    + _PrimaryColor.rgb * radial * 0.08;
                fixed3 borderColor = lerp(_PrimaryColor.rgb, _AccentColor.rgb, saturate(borderSweep + pulse * 0.22));
                borderColor += _AccentColor.rgb * (innerLine * (0.08 + borderGlow * 0.05) + pulse * cornerGlow * 0.18);
                fixed3 glowColor = lerp(_PrimaryColor.rgb, _AccentColor.rgb, 0.62)
                    + _AccentColor.rgb * (pulse * 0.16 + sweep * 0.06);

                fixed4 color;
                color.rgb =
                    cardColor * cardRole +
                    backgroundColor * backgroundRole +
                    borderColor * borderRole +
                    glowColor * glowRole;
                color.a =
                    layerAlpha * cardRole +
                    layerAlpha * saturate(0.09 + ambientDetail * 0.14 + radial * 0.04 + effect * 0.035) * backgroundRole +
                    layerAlpha * saturate(borderRing * (0.62 + borderGlow * 0.12) + innerLine * 0.2 + borderSweep * 0.22 + pulse * cornerGlow * 0.12) * borderRole +
                    layerAlpha * saturate((edgeHalo * 0.14 + cornerGlow * 0.08 + borderSweep * 0.05 + pulse * 0.07) * glow * (0.28 + effect * 0.2 + borderGlow * 0.08)) * glowRole;

                #ifdef UNITY_UI_CLIP_RECT
                color *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
