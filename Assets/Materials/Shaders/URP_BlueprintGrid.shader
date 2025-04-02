Shader "URP/BlueprintGrid"
{
    Properties
    {
        _ColorTopLeft ("Top Left Background Color", Color) = (0.05, 0.2, 0.6, 1)
        _ColorBottomRight ("Bottom Right Background Color", Color) = (0.02, 0.1, 0.4, 1)
        _GridColor ("Grid Line Color", Color) = (1,1,1,1)
        _GridSize ("Grid Size", Float) = 10
        _LineThickness ("Line Thickness", Float) = 0.02
        _ThickLineMultiplier ("Thick Line Multiplier", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldUV : TEXCOORD1;
            };

            float4 _ColorTopLeft;
            float4 _ColorBottomRight;
            float4 _GridColor;
            float _GridSize;
            float _LineThickness;
            float _ThickLineMultiplier;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv * _GridSize;
                OUT.worldUV = IN.uv; // Preserve original UVs for background gradient
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Compute background gradient
                float4 bgColor = lerp(_ColorTopLeft, _ColorBottomRight, (IN.worldUV.x + IN.worldUV.y) * 0.5);

                // Grid calculation
                float2 grid = frac(IN.uv);
                float2 gridStep = fmod(floor(IN.uv), 10.0); // Detect multiples of 10

                // Adjust thickness for every 10th line
                float currentThickness = _LineThickness;
                if (gridStep.x == 0 || gridStep.y == 0) 
                {
                    currentThickness *= _ThickLineMultiplier;
                }

                float minDist = min(grid.x, grid.y);
                float fade = smoothstep(currentThickness, 0.0, minDist);

                // Mix grid and background (invert fade so lines remain grid color)
                return lerp(_GridColor, bgColor, fade);
            }
            ENDHLSL
        }
    }
}
