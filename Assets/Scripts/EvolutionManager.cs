using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EvolutionManager : MonoBehaviour
{

    public int     populationPoolNumber;
    public int     maximumNumberOfBrushStrokes;

    public Texture brushTexture;  
                   
    Genes[]        PopulationMemberOne;
                   
                   
    Material       rendering_material;
                   
    ComputeBuffer  points_buffer;

    // Start is called before the first frame update
    void Start()
    {
        PopulationMemberOne = new Genes[maximumNumberOfBrushStrokes];
        CPUSystems.InitatePopulationMember(ref PopulationMemberOne);


        rendering_material = new Material(Shader.Find("Unlit/PopulationShader"));
        if (!rendering_material) Debug.LogError("Couldnt find the population shader");

        rendering_material.SetTexture("_MainTex", brushTexture);

        points_buffer = new ComputeBuffer(maximumNumberOfBrushStrokes, sizeof(float) * 8 + sizeof(int)*1);
        points_buffer.SetData(PopulationMemberOne);
        rendering_material.SetBuffer("Brushes_Buffer", points_buffer);


    }

    private void OnDestroy()
    {
        points_buffer.Release();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRenderObject()
    {
        rendering_material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, maximumNumberOfBrushStrokes * 6, 1);
    }




        string GenesToString(Genes g)
    {
        return string.Format("position: ({0}, {1}), rotation: {2}, scale: ({3}, {4}), color: ({5}, {6}, {7}), textureID: {8}",
                              g.position_X, g.position_Y, g.z_Rotation, g.scale_X, g.scale_Y, 
                              g.color_r, g.color_g, g.color_b, g.texture_ID);
    }
}
