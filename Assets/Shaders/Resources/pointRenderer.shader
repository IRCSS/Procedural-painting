
Shader "Unlit/pointRenderer"
{
   
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
         ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            
            #pragma target   5.0  
            #pragma vertex   vert
            #pragma fragment frag


            #include "UnityCG.cginc"


            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : TEXCOORD0;
            };

            uniform StructuredBuffer<float2> points_buffer;



            uint triple32(uint x)
            {
                x ^= x >> 17;
                x *= 0xed5ad4bbU;
                x ^= x >> 11;
                x *= 0xac4c1b51U;
                x ^= x >> 15;
                x *= 0x31848babU;
                x ^= x >> 14;
                return x;
            }

            float wang_rnd(uint seed)
            {
                uint rndint = triple32(seed);
                return ((float)rndint) / float(0xFFFFFFFF);                                                       // 0xFFFFFFFF is max unsigned integer in hexa decimal
            }


            v2f vert (uint id : SV_VertexID)
            {
                v2f o;

                o.vertex =float4(points_buffer[id].xy, 0.5, 1.);
                //o.vertex = mul(UNITY_MATRIX_P, float4(buffer[id].xy, -0.5, 1.));
                o.color  = float4(wang_rnd(id), wang_rnd(id+651), wang_rnd(id+21), 1.);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               
                return i.color;
            }
            ENDCG
        }
    }
}
