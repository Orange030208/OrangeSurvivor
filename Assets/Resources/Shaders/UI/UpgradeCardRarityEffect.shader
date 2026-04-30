Shader "UI/Upgrade Card Rarity Effect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Rarity ("Rarity", Float) = 0
        _EffectIntensity ("Effect Intensity", Range(0, 2)) = 0.5
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 0.5
        _PrimaryColor ("Primary Color", Color) = (1,1,1,1)
        _SecondaryColor ("Secondary Color", Color) = (0.2,0.2,0.2,1)
        _AccentColor ("Accent Color", Color) = (1,1,1,1)
        _FlowSpeed ("Flow Speed", Range(0, 6)) = 0.45
        _BorderWidth ("Border Width", Range(0.01, 0.35)) = 0.075
        _BorderGlow ("Border Glow", Range(0, 4)) = 0.45
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 0.9
        _LayerRole ("Layer Role", Range(0, 3)) = 0
        _AlphaScale ("Alpha Scale", Range(0, 1)) = 1
        _PatternIntensity ("Surface Detail", Range(0, 2)) = 0.18
        _SweepIntensity ("Sweep Intensity", Range(0, 3)) = 0.35
        _PulseIntensity ("Pulse Intensity", Range(0, 3)) = 0.25
        _InteractionBrightness ("Interaction Brightness", Range(0, 3)) = 1
        _InteractionFlowMultiplier ("Interaction Flow Multiplier", Range(0, 4)) = 1
        _InteractionGlowMultiplier ("Interaction Glow Multiplier", Range(0, 4)) = 1
        _SelectedAmount ("Selected Amount", Range(0, 1)) = 0

        _ShapeMaskTex ("Shape Hex Mask", 2D) = "white" {}
        _FlowTex ("Border Flow Brushed Texture", 2D) = "white" {}
        _NoiseTex ("Seamless Cloud Noise", 2D) = "gray" {}
        _LinearMaskTex ("Linear Width Mask", 2D) = "white" {}
        _RadialMaskTex ("Radial Glow Mask", 2D) = "white" {}
        _NoiseDistort ("Noise Distort", Range(0, 0.08)) = 0.018
        _InnerDim ("Inner Dim", Range(0, 1)) = 0.34

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
            sampler2D _ShapeMaskTex;
            sampler2D _FlowTex;
            sampler2D _NoiseTex;
            sampler2D _LinearMaskTex;
            sampler2D _RadialMaskTex;
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
            float _InteractionBrightness;
            float _InteractionFlowMultiplier;
            float _InteractionGlowMultiplier;
            float _SelectedAmount;
            float _NoiseDistort;
            float _InnerDim;

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

            inline float HexDistance(float2 uv)
            {
                float2 hexPoint = abs(uv - 0.5) * 2.0;
                return max(hexPoint.x * 0.8660254 + hexPoint.y * 0.5, hexPoint.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 spriteColor = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float2 uv = input.texcoord;
                float time = _Time.y;
                float effect = clamp(_EffectIntensity, 0.0, 2.0);
                float brightness = max(0.0, _InteractionBrightness);
                float flowMultiplier = max(0.0, _InteractionFlowMultiplier);
                float glowMultiplier = max(0.0, _InteractionGlowMultiplier);

                float shapeMask = saturate(tex2D(_ShapeMaskTex, uv).a);
                shapeMask = max(shapeMask, saturate(tex2D(_ShapeMaskTex, uv).r));
                float linearMask = saturate(tex2D(_LinearMaskTex, uv).r);
                float radialMask = saturate(tex2D(_RadialMaskTex, uv).r);

                float hexDistance = HexDistance(uv);
                float borderWidth = max(0.01, _BorderWidth * lerp(0.82, 1.12, linearMask));
                float outerShape = 1.0 - smoothstep(0.99, 1.015, hexDistance);
                float innerShape = 1.0 - smoothstep(0.99 - borderWidth, 1.015 - borderWidth, hexDistance);
                float borderRing = saturate(outerShape - innerShape);
                float innerLine = saturate((1.0 - smoothstep(0.99 - borderWidth * 2.2, 1.015 - borderWidth * 2.2, hexDistance)) - innerShape);
                float edgeHalo = saturate(smoothstep(0.64, 0.99, hexDistance) * outerShape);
                float fresnel = pow(saturate(hexDistance), 3.2) * outerShape;

                float scrollSpeed = _FlowSpeed * flowMultiplier;
                float2 noiseUvA = uv * 1.45 + float2(time * 0.025, -time * 0.018);
                float2 noiseUvB = uv * 2.2 + float2(-time * 0.016, time * 0.022);
                float noiseA = tex2D(_NoiseTex, noiseUvA).r;
                float noiseB = tex2D(_NoiseTex, noiseUvB).r;
                float noise = (noiseA + noiseB) * 0.5;
                float2 distortion = (float2(noiseA, noiseB) - 0.5) * _NoiseDistort * (0.6 + effect);
                float2 flowUv = uv + distortion + float2(time * scrollSpeed * 0.16, -time * scrollSpeed * 0.09);
                float flowTex = tex2D(_FlowTex, flowUv * float2(1.25, 1.0)).r;
                float flowStrand = pow(saturate(flowTex), 1.75);

                float sweepAxis = saturate(uv.x * 0.64 + uv.y * 0.36 + noise * 0.08);
                float sweepPosition = frac(time * scrollSpeed * 0.22 + _Rarity * 0.13);
                float sweep = SoftBand(sweepAxis, sweepPosition, 0.055, 0.13) * _SweepIntensity;
                float reverseSweep = SoftBand(1.0 - sweepAxis, frac(sweepPosition + 0.43), 0.035, 0.09) * _SelectedAmount;
                float pulseWave = saturate(sin(time * _PulseSpeed * 6.28318) * 0.5 + 0.5);
                float pulse = smoothstep(0.28, 1.0, pulseWave) * _PulseIntensity;

                float ambientDetail = pow(saturate(noise), 2.6) * _PatternIntensity;
                float verticalEnergy = saturate(0.15 + uv.y * 0.74 + radialMask * 0.35);
                fixed3 surfaceColor = lerp(_SecondaryColor.rgb, _PrimaryColor.rgb, verticalEnergy);
                surfaceColor = lerp(surfaceColor, _AccentColor.rgb, saturate((flowStrand * 0.12 + sweep * 0.1) * effect));

                float borderEnergy = saturate(
                    borderRing * (0.45 + flowStrand * 0.75) +
                    innerLine * 0.3 +
                    sweep * 0.7 +
                    reverseSweep * 0.55 +
                    pulse * 0.25 +
                    _SelectedAmount * 0.22);
                float glowEnergy = saturate(
                    edgeHalo * (0.25 + fresnel * 1.35) +
                    borderEnergy * 0.35 +
                    radialMask * _SelectedAmount * 0.18 +
                    pulse * 0.12);

                fixed3 cardColor = lerp(spriteColor.rgb, spriteColor.rgb * (surfaceColor * 0.72 + 0.28), saturate(effect * 0.22));
                fixed3 backgroundColor = surfaceColor * (0.42 + effect * 0.12)
                    + _AccentColor.rgb * ambientDetail * 0.26
                    + _PrimaryColor.rgb * radialMask * 0.08;
                fixed3 borderColor = lerp(_PrimaryColor.rgb, _AccentColor.rgb, saturate(flowStrand * 0.45 + sweep + reverseSweep + pulse * 0.25));
                borderColor *= (0.78 + borderEnergy * (1.25 + _BorderGlow * 0.18));
                fixed3 glowColor = lerp(_PrimaryColor.rgb, _AccentColor.rgb, 0.68)
                    * (0.5 + glowEnergy * (1.6 + _GlowIntensity * 0.25));

                float smoothShape = saturate(lerp(outerShape, outerShape * shapeMask, 0.18));
                float role = floor(_LayerRole + 0.5);
                float cardRole = RoleMask(role, 0.0);
                float backgroundRole = RoleMask(role, 1.0);
                float borderRole = RoleMask(role, 2.0);
                float glowRole = RoleMask(role, 3.0);
                float effectLayer = saturate(backgroundRole + borderRole + glowRole);
                float sourceAlpha = lerp(spriteColor.a, 1.0, effectLayer);
                float layerAlpha = saturate(_AlphaScale) * sourceAlpha * smoothShape;

                fixed4 color;
                color.rgb =
                    cardColor * cardRole +
                    backgroundColor * backgroundRole +
                    borderColor * borderRole +
                    glowColor * glowRole;
                color.rgb *= brightness;
                color.a =
                    layerAlpha * cardRole +
                    layerAlpha * saturate((0.07 + ambientDetail * 0.18 + radialMask * 0.07 + effect * 0.05) * (1.0 - _InnerDim * 0.35)) * backgroundRole +
                    layerAlpha * saturate(borderEnergy * (0.62 + _BorderGlow * 0.18) + fresnel * 0.16 + _SelectedAmount * 0.16) * borderRole +
                    layerAlpha * saturate(glowEnergy * _GlowIntensity * glowMultiplier * (0.24 + effect * 0.18 + _BorderGlow * 0.08)) * glowRole;

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
