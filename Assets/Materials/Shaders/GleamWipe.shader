Shader "Custom/GleamWipeAdditive"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}    // Sample existing texture
        _GleamColor("Gleam Color", Color) = (1,1,1,1)
        _GleamWidth("Gleam Width", Range(0.01, 0.5)) = 0.1
        _GleamSpeed("Gleam Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _GleamColor;
            float _GleamWidth;
            float _GleamSpeed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the object's original texture
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // Move the gleam diagonally
                float gleamPosition = IN.uv.x + IN.uv.y + _Time.y * _GleamSpeed;
                gleamPosition = frac(gleamPosition);

                // Create gleam mask
                float gleamMask = smoothstep(0.5 - _GleamWidth, 0.5, gleamPosition) * (1.0 - smoothstep(0.5, 0.5 + _GleamWidth, gleamPosition));

                // Add gleam on top
                float4 gleam = _GleamColor * gleamMask;

                return baseColor + gleam;
            }
            ENDHLSL
        }
    }
}
