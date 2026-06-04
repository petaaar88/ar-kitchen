Shader "AR Kitchen/Feathered Plane"
{
    Properties
    {
        _BaseMap ("Dot Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 0.9)
        _FeatherWidth ("Feather Width (m)", Float) = 0.25
        // Plane-space bounding box (x,z min / x,z max), fed per-plane by ARPlaneFeather.
        _PlaneMin ("Plane Min XZ", Vector) = (-1, -1, 0, 0)
        _PlaneMax ("Plane Max XZ", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _FeatherWidth;
                float4 _PlaneMin;
                float4 _PlaneMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0; // tiled UV for dot sampling
                float2 planeXZ     : TEXCOORD1; // plane-space position in metres
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.planeXZ = IN.uv;                              // UV0 == (x, z) in metres
                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw; // tiled dots
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Distance (metres) to the nearest bounding-box edge of the plane.
                float dx = min(IN.planeXZ.x - _PlaneMin.x, _PlaneMax.x - IN.planeXZ.x);
                float dz = min(IN.planeXZ.y - _PlaneMin.y, _PlaneMax.y - IN.planeXZ.y);
                float edgeDist = min(dx, dz);

                // Fade the dots out within _FeatherWidth metres of any edge.
                col.a *= saturate(edgeDist / max(_FeatherWidth, 1e-4));
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
