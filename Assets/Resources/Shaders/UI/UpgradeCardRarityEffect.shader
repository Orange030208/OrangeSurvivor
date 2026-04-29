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
        _PixelGrid ("Pixel Grid", Range(8, 128)) = 48
        _FlowSpeed ("Flow Speed", Range(0, 4)) = 0.9
        _BorderWidth ("Border Width", Range(0.01, 0.3)) = 0.08
        _BorderGlow ("Border Glow", Range(0, 3)) = 0.65
        _EnergyDensity ("Epic Energy Density", Range(2, 24)) = 12
        _PulseSpeed ("Legendary Pulse Speed", Range(0, 6)) = 1.4

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
            float _PixelGrid;
            float _FlowSpeed;
            float _BorderWidth;
            float _BorderGlow;
            float _EnergyDensity;
            float _PulseSpeed;

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

            inline float PixelStep(float value, float steps)
            {
                return floor(saturate(value) * steps) / max(1.0, steps - 1.0);
            }

            inline float HardBand(float value, float center, float width)
            {
                float halfWidth = max(0.001, width * 0.5);
                return step(center - halfWidth, value) * (1.0 - step(center + halfWidth, value));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 spriteColor = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float effect = saturate(_EffectIntensity);
                float glow = max(0.0, _GlowIntensity);
                float grid = max(8.0, _PixelGrid);
                float2 pixelUv = floor(input.texcoord * grid) / grid;

                float2 centeredUv = abs(pixelUv * 2.0 - 1.0);
                float edgeMask = saturate(max(centeredUv.x, centeredUv.y));
                float borderWidth = max(0.01, _BorderWidth);
                float border = step(1.0 - borderWidth, edgeMask);
                float innerBorder = step(1.0 - borderWidth * 1.75, edgeMask) * (1.0 - border);
                float cornerSpark = step(0.86, centeredUv.x) * step(0.86, centeredUv.y);

                float time = _Time.y;
                float diagonal = frac(pixelUv.x + pixelUv.y - time * _FlowSpeed * 0.16 + _Rarity * 0.13);
                float backgroundFlow = HardBand(diagonal, 0.18, 0.12) + HardBand(diagonal, 0.62, 0.07);
                backgroundFlow = PixelStep(backgroundFlow, 3.0);

                float edgeAxis = centeredUv.x > centeredUv.y ? pixelUv.y : pixelUv.x;
                float borderSweep = HardBand(frac(edgeAxis + time * _FlowSpeed * 0.36 + _Rarity * 0.11), 0.2, 0.13);
                borderSweep *= border;

                float epicMask = step(1.5, _Rarity);
                float legendaryMask = step(2.5, _Rarity);
                float energyWave = sin((pixelUv.x - pixelUv.y + time * 0.42) * _EnergyDensity);
                float energySteps = step(0.44, energyWave) * innerBorder * epicMask;

                float pulseRaw = sin(time * _PulseSpeed * 6.28318);
                float legendaryPulse = step(0.15, pulseRaw) * legendaryMask;
                float pulseBorder = (border + cornerSpark) * legendaryPulse;

                float shade = PixelStep(0.45 + backgroundFlow * 0.24 + edgeMask * 0.2, 5.0);
                fixed3 baseRgb = lerp(_SecondaryColor.rgb, _PrimaryColor.rgb, shade);
                fixed3 accentRgb = _AccentColor.rgb * (borderSweep + energySteps * 0.8 + pulseBorder);
                fixed3 glowRgb = _AccentColor.rgb * (border * _BorderGlow + cornerSpark * 0.35) * glow;

                fixed4 color = spriteColor;
                color.rgb = spriteColor.rgb * baseRgb;
                color.rgb = lerp(color.rgb, baseRgb + glowRgb + accentRgb, effect * 0.68);
                color.rgb += _AccentColor.rgb * backgroundFlow * effect * 0.12;
                color.a = spriteColor.a * saturate(0.52 + effect * 0.28 + border * glow * 0.18 + energySteps * 0.08 + pulseBorder * 0.14);

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
