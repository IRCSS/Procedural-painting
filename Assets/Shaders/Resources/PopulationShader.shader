Shader "Unlit/PopulationShader"
{

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        
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
                float3 color  : TEXCOORD1;
                int    id     : TEXCOORD2;
            };


            struct Genes
            {
                float2 position;       // screen space 0-1
                float  z_Rotation;     // 0 to tau
                float2 scale;          // scale in quad aligned space, will be clamped
                float3 color;          // the colors of each stroke
                int    texture_ID;     // such a waste of bit space. I only need 2 bits, but paying the cost 8 + padding
            };  // Struct size 8 *4 bytes + 1  * 4 = 36 bytes


            sampler2D               _MainTex;
            StructuredBuffer<Genes> _population_pool;
            uint                    _memember_begin_stride;

            v2f vert (uint id : SV_VertexID)
            {

                uint   vertexCase = fmod(id, 6);
                
                float3 vertexPos  = float3(0.,0.,0.);
                float2 v2_1       = float2(0.,0.);   // generic name. Now it is uv, I will use the register for other things later
                


                [branch] switch (vertexCase) {
                case 0: vertexPos = float3(-0.5, 0.5, 0.5); v2_1 = float2(0.,1.);break; // upper left  of the Quad
                case 1: vertexPos = float3(-0.5,-0.5, 0.5); v2_1 = float2(0.,0.);break;	// lower left  of the Quad
                case 2: vertexPos = float3( 0.5,-0.5, 0.5); v2_1 = float2(1.,0.);break;	// lower right of the Quad
                case 3: vertexPos = float3( 0.5,-0.5, 0.5); v2_1 = float2(1.,0.);break;	// lower right of the Quad
                case 4: vertexPos = float3( 0.5, 0.5, 0.5); v2_1 = float2(1.,1.);break;	// upper right of the Quad
                case 5: vertexPos = float3(-0.5, 0.5, 0.5); v2_1 = float2(0.,1.);break;	// upper left  of the Quad
                }

                v2f o;
                o.uv     = v2_1;
                
                vertexCase      = floor(id/6); // Calculating the index fo sampling brushes_buffer. 
                Genes brushGene = _population_pool[_memember_begin_stride + vertexCase];
                
                o.color = brushGene.color;
                o.id    = brushGene.texture_ID;
                
                v2_1    = float2(sin(brushGene.z_Rotation), cos(brushGene.z_Rotation));
                
                float4x4 brushModelMat =  
                {
                    v2_1.y * brushGene.scale.x,  -v2_1.x * brushGene.scale.y,     0., brushGene.position.x,
                    v2_1.x * brushGene.scale.x,   v2_1.y * brushGene.scale.y,     0., brushGene.position.y,
                                            0.,                          0.,      1.,                   0.,
                                            0.,                          0.,      0.,                   1.
                };


                vertexPos = mul(brushModelMat,  float4(vertexPos.xyz, 1.));

                //o.vertex  = mul(UNITY_MATRIX_P, float4(vertexPos.xyz, 1.));
                  o.vertex  = float4(vertexPos.xyz,1.);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col     = tex2D(_MainTex, i.uv);
                int4   check   = int4(i.id == 0, i.id == 1, i.id == 2, i.id == 3);
                col.a   = col.r * check.x + col.g * check.y + col.b * check.z + col.a * check.w;
                col.xyz = i.color.xyz;
                return col;
            }
            ENDCG
        }
    }
}
