Shader "Unlit/klimtShader"
{
    Properties
    {
        // Scale controls
        _SpiralScale ("Spiral Scale", Range(0.1, 5.0)) = 0.3
        _SquareSize ("Square Size", Range(0.01, 1.0)) = 0.05
        _SwirlIntensity ("Swirl Intensity", Range(0.1, 3.0)) = 2.5
        
        // Tree of Life color palette
        _GoldColor ("Rich Gold", Color) = (0.8, 0.6, 0.1, 1)
        _CopperColor ("Copper", Color) = (0.7, 0.4, 0.2, 1)
        _DeepGreen ("Deep Green", Color) = (0.1, 0.3, 0.1, 1)
        _BrownColor ("Rich Brown", Color) = (0.3, 0.2, 0.1, 1)
        _CreamColor ("Cream", Color) = (0.9, 0.8, 0.6, 1)
        
        // Pattern controls
        _SpiralSpeed ("Spiral Animation Speed", Range(0, 2)) = 0.2
        _NoiseScale ("Organic Noise Scale", Range(1, 20)) = 12
        _PatternMix ("Pattern Mix", Range(0, 1)) = 0.6
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            float _SpiralScale, _SquareSize, _SwirlIntensity, _SpiralSpeed;
            float4 _GoldColor, _CopperColor, _DeepGreen, _BrownColor, _CreamColor;
            float _NoiseScale, _PatternMix;
            
            // Better noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Smooth noise with interpolation
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Create flowing spiral patterns like Tree of Life branches
            float2 spiralDistortion(float2 uv, float intensity)
            {
                float2 center = float2(0, 0);
                float2 delta = uv - center;
                float dist = length(delta);
                float angle = atan2(delta.y, delta.x);
                
                // Create the spiral effect with time animation
                float spiral = dist * 3.14159 * intensity + _Time.y * _SpiralSpeed;
                float spiralSin = sin(spiral) * 0.1 * intensity;
                float spiralCos = cos(spiral) * 0.1 * intensity;
                
                return uv + float2(spiralSin, spiralCos);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 baseUV = i.worldPos.xz * _SpiralScale;
                
                // ==============================================================================
                // TREE OF LIFE SPIRAL DISTORTION
                // ==============================================================================
                float2 spiralUV = spiralDistortion(baseUV, _SwirlIntensity);
                
                // ==============================================================================
                // GEOMETRIC SQUARES (like Klimt's geometric sections)
                // ==============================================================================
                float2 squareUV = spiralUV / _SquareSize;
                int2 squareGrid = floor(squareUV);
                bool isEvenSquare = (squareGrid.x + squareGrid.y) % 2 == 0;
                
                // Create different square sizes for variation
                float2 bigSquareUV = spiralUV / (_SquareSize * 2);
                int2 bigSquareGrid = floor(bigSquareUV);
                bool isBigSection = (bigSquareGrid.x + bigSquareGrid.y) % 3 == 0;
                
                // ==============================================================================
                // ORGANIC FLOWING PATTERNS (like the tree branches)
                // ==============================================================================
                float organicNoise1 = smoothNoise(spiralUV * _NoiseScale);
                float organicNoise2 = smoothNoise(spiralUV * _NoiseScale * 0.5);
                float organicNoise3 = smoothNoise(spiralUV * _NoiseScale * 2.0);
                
                // Combine noise for organic tree-like flow
                float organicPattern = (organicNoise1 * 0.5) + (organicNoise2 * 0.3) + (organicNoise3 * 0.2);
                
                // Create flowing lines like tree branches
                float branchPattern = sin(spiralUV.x * 10 + organicNoise1 * 5) * sin(spiralUV.y * 8 + organicNoise2 * 4);
                branchPattern = smoothstep(0.2, 0.8, abs(branchPattern));
                
                // ==============================================================================
                // TREE OF LIFE COLOR MIXING
                // ==============================================================================
                fixed4 baseColor;
                
                // Start with geometric pattern
                if (isBigSection)
                {
                    baseColor = isEvenSquare ? _GoldColor : _DeepGreen;
                }
                else
                {
                    baseColor = isEvenSquare ? _CopperColor : _BrownColor;
                }
                
                // Blend in organic tree patterns
                baseColor = lerp(baseColor, _CreamColor, branchPattern * 0.4);
                
                // Add organic noise variation
                if (organicPattern > 0.6)
                {
                    baseColor = lerp(baseColor, _GoldColor, 0.3);
                }
                else if (organicPattern < 0.3)
                {
                    baseColor = lerp(baseColor, _DeepGreen, 0.2);
                }
                
                // Final organic texture overlay
                float organicOverlay = smoothNoise(spiralUV * 15) * 0.1;
                baseColor.rgb += organicOverlay;
                
                // Mix between geometric and organic based on _PatternMix
                float geometricInfluence = 1.0 - _PatternMix;
                float organicInfluence = _PatternMix;
                
                return baseColor;
            }
            ENDCG
        }
    }
}