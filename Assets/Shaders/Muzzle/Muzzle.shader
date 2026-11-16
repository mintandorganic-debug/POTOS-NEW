Shader "Custom/HDRP_MuzzleFlash"
{
    Properties
    {
        [Header(HDR Fire Colors)]
        [HDR] _WhiteCore ("White Hot Core (Ядро)", Color) = (24, 20, 12, 1)
        [HDR] _YellowMid ("Bright Yellow-Orange (Середина)", Color) = (15, 5.5, 0.5, 1)
        [HDR] _OrangeOuter ("Deep Fiery Orange (Огонь)", Color) = (9, 1.5, 0.08, 1)
        _SmokeColor ("Cooled Gunpowder Smoke (Серый дым)", Color) = (0.32, 0.30, 0.29, 0.75)

        [Header(Turbulence and Shape)]
        _NoiseScale ("Gas Billow Scale (Размер клубов)", Float) = 7.5
        _DetailScale ("Fine Tear Scale (Мелкие разрывы)", Float) = 18.0
        _WarpStrength ("Vortex Swirl (Закручивание клубов)", Range(0.0, 1.5)) = 0.6
        _FlowSpeed ("Gas Velocity Speed (Скорость потока)", Float) = 10.0
        _EdgeSharpness ("Flame Crispness (Четкость краев)", Range(0.05, 0.6)) = 0.28
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardOnly" }

            // Premultiplied Alpha: позволяет одновременно иметь яркий HDR-огонь И плотный серый дым!
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv : TEXCOORD0; // xy = UV, z = AgePercent (0..1), w = StableRandom.x
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WhiteCore;
                float4 _YellowMid;
                float4 _OrangeOuter;
                float4 _SmokeColor;
                float _NoiseScale;
                float _DetailScale;
                float _WarpStrength;
                float _FlowSpeed;
                float _EdgeSharpness;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 uv, int octaves)
            {
                float val = 0.0;
                float amp = 0.5;
                float2 shift = float2(37.2, 19.8);
                float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8);

                for (int i = 0; i < octaves; i++)
                {
                    val += amp * valueNoise(uv);
                    uv = mul(rot, uv) * 2.03 + shift;
                    amp *= 0.5;
                }
                return val;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionRWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionRWS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // t = 0 у среза дула, t = 1 впереди по направлению выстрела
                float t = input.uv.x;
                float side = (input.uv.y - 0.5) * 2.0; // -1..1 поперек (вертикаль)
                float age = saturate(input.uv.z);        // 0 = выстрел, 1 = дым рассеялся
                float seed = input.uv.w * 47.31;

                // --- ДВИЖЕНИЕ ТОЛЬКО ВПЕРЕД (НИКАКОГО ВОЗВРАТА В СТВОЛ) ---
                // 1. Облако вырывается из дула (0.12) и по инерции летит всё дальше вперед (0.82)
                float emerge = smoothstep(0.0, 0.18, age);
                float driftForward = pow(age, 0.75);
                float blastCenter = lerp(0.10, 0.82, driftForward);

                // 2. Высота: резко раскрывается в гриб (0.95) к 18% времени, затем вытягивается и сужается (0.28)
                float bloom = lerp(0.18, 0.96, sin(emerge * 1.5707963));
                float widthRadius = lerp(bloom, 0.28, smoothstep(0.18, 1.0, age));

                // 3. Длина: сначала компактный гриб (0.22), затем вытягивается вперед по ходу полета (0.52)
                float lengthRadius = lerp(0.20, 0.52, smoothstep(0.12, 0.9, age));

                float localT = (t - blastCenter) / lengthRadius;
                float taper = lerp(1.1, 0.7, saturate(localT * 0.5 + 0.5));
                float localS = side / max(widthRadius * taper, 0.02);

                float mainBody = saturate(1.0 - length(float2(localT, localS)));

                // Четкий ствол/ножка пламени из дула к грибу (соединяет дуло с облаком)
                float stemWidth = lerp(0.14, 0.34, saturate(t / max(blastCenter, 0.01)));
                float residualTail = saturate(1.0 - abs(side) / stemWidth)
                                   * smoothstep(0.0, 0.02, t)
                                   * smoothstep(blastCenter + 0.08, blastCenter * 0.5, t)
                                   * smoothstep(0.65, 0.0, age) * 1.15;

                float baseSilhouette = max(mainBody, residualTail);

                // --- ВИХРЕВАЯ ТУРБУЛЕНТНОСТЬ КЛУБОВ ---
                float stretchFactor = lerp(1.0, 0.5, age);
                float flowOffset = age * 5.0 + _Time.y * (_FlowSpeed * 0.2) + seed;
                float2 baseUV = float2(t * _NoiseScale * stretchFactor - flowOffset, side * _NoiseScale * 0.85 + seed * 0.5);

                float2 warp;
                warp.x = fbm(baseUV + float2(1.7, 9.2), 3) - 0.5;
                warp.y = fbm(baseUV + float2(8.3, 2.8), 3) - 0.5;

                float billowingNoise = fbm(baseUV + warp * _WarpStrength * 2.5, 4);
                float2 detailUV = float2(t * _DetailScale * stretchFactor - flowOffset * 1.3, side * _DetailScale + seed);
                float fineNoise = valueNoise(detailUV + warp * 1.5);
                float combinedNoise = lerp(billowingNoise, fineNoise, 0.3);

                float cloudDensity = baseSilhouette * (0.38 + combinedNoise * 1.3);

                // Облако не схлопывается назад, а плавно рассеивается по мере полета вперед
                float erosion = lerp(0.10, 0.48, pow(age, 1.4));
                float mask = smoothstep(erosion, erosion + _EdgeSharpness, cloudDensity);

                // --- ОСТЫВАНИЕ ОГНЯ В СЕРЫЙ ПОРОХОВОЙ ДЫМ ---
                // В первые 35% времени это яркий огонь, затем температура быстро падает в серый дым
                float cooling = smoothstep(0.55, 0.08, age);
                float heat = saturate((cloudDensity * 1.1 + mainBody * 0.4) * cooling);

                // Огненная палитра (от оранжевого к белому ядру)
                float3 fireRGB = lerp(_OrangeOuter.rgb, _YellowMid.rgb, smoothstep(0.15, 0.55, heat));
                fireRGB = lerp(fireRGB, _WhiteCore.rgb, smoothstep(0.55, 0.92, heat));

                // Смешиваем огонь с серым дымом по мере остывания (heat -> 0)
                float isFire = smoothstep(0.05, 0.35, heat);
                float3 finalColor = lerp(_SmokeColor.rgb, fireRGB, isFire);

                // Дым полупрозрачный, огонь яркий; в конце жизни плавно растворяется в воздухе
                float lifeFade = smoothstep(1.0, 0.65, age);
                float alpha = saturate(mask * lifeFade * lerp(_SmokeColor.a, 1.0, isFire) * input.color.a);

                // Premultiplied Alpha выход (RGB уже умножен на alpha)
                return float4(finalColor * alpha * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}