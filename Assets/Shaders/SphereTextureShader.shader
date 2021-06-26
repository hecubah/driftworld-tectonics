Shader "Custom/SphereTextureShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Equirectangular Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.0
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" "DisableBatching" = "False" }
            LOD 200

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows vertex:vert

            #pragma target 3.0

            sampler2D _MainTex;

            struct Input
            {
                float3 vertex;
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            void vert(inout appdata_full v, out Input IN)
            {
                IN.vertex = v.vertex;
            }
            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // -0.5 to 0.5 range
                float phi = atan2(IN.vertex.z, IN.vertex.x) / (UNITY_PI * 2.0);

                // 0.0 to 1.0 range
                //float phi_frac = phi + 1.0f;//frac(phi);
                float phi_frac = frac(phi);

                // negate the y because acos(-1.0) = PI, acos(1.0) = 0.0
                float theta = acos(-normalize(IN.vertex.xyz).y) / UNITY_PI;

                // construct the uvs, selecting the phi to use based on the derivatives
                float2 uv = float2(
                    fwidth(phi) < fwidth(phi_frac) ? phi : phi_frac,
                    //phi,
                    theta // no special stuff needed for theta
                    );
                fixed4 c = tex2D(_MainTex, uv);
                o.Albedo = c.rgb;
                o.Smoothness = _Glossiness;
                o.Alpha = 1.0f;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
// Original idea and solution sent by bgolus

