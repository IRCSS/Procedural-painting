Shader "Unlit/PopulationShader"
{

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (uint id : SV_VertexID)
            {

				uint   vertexCase = fmod(id, 6);

				float3 vertexPos  = float3(0.,0.,0.);
				float2 vertexUV   = float2(0.,0.);

				[branch] switch (vertexCase) {
				case 0: vertexPos = float3(-0.5, 0.5, 0.5); vertexUV = float2(0.,1.);break; // upper left  of the Quad
				case 1: vertexPos = float3(-0.5,-0.5, 0.5); vertexUV = float2(0.,0.);break;	// lower left  of the Quad
				case 2: vertexPos = float3( 0.5,-0.5, 0.5); vertexUV = float2(1.,0.);break;	// lower right of the Quad
				case 3: vertexPos = float3( 0.5,-0.5, 0.5); vertexUV = float2(1.,0.);break;	// lower right of the Quad
				case 4: vertexPos = float3( 0.5, 0.5, 0.5); vertexUV = float2(1.,1.);break;	// upper right of the Quad
				case 5: vertexPos = float3(-0.5, 0.5, 0.5); vertexUV = float2(0.,1.);break;	// upper left  of the Quad
				}

                v2f o;
                o.vertex = mul(UNITY_MATRIX_P, float4(vertexPos.xyz, 1.));
                o.uv     = vertexUV;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
