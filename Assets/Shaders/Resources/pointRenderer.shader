
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
            };

            uniform StructuredBuffer<float2> buffer;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(buffer[id].xy, 0., 1.));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               
                return float4(0.6,0.8,0.1,1.);
            }
            ENDCG
        }
    }
}
