Shader "Unlit/CIELabBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "LabColorSpace.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Checking the range of the function and making sure it is within the expected range
                fixed4 col = tex2D(_MainTex, i.uv);
                col.xyz = rgb2lab(col.xyz);
                //float4 out_ = float4(0., 0., 0., 1.);
                //if (col.x < 0.0 || col.y < 0.0 || col.z < 0.0) out_.x = 1.;
                //if (col.x > 1.0 || col.y > 1.0 || col.z > 1.0) out_.y = 1.;

                return col;
            }
            ENDCG
        }
    }
}
