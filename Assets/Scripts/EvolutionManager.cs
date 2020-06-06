using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EvolutionManager : MonoBehaviour
{

    public int populationPoolNumber;
    public int maximumNumberOfBrushStrokes;

    Genes[] PopulationMemberOne;

    // Start is called before the first frame update
    void Start()
    {
        PopulationMemberOne = new Genes[maximumNumberOfBrushStrokes];
        CPUSystems.InitatePopulationMember(ref PopulationMemberOne);


    }

    // Update is called once per frame
    void Update()
    {
        
    }






    string GenesToString(Genes g)
    {
        return string.Format("position: ({0}, {1}), rotation: {2}, scale: ({3}, {4}), color: ({5}, {6}, {7}), textureID: {8}",
                              g.position_X, g.position_Y, g.z_Rotation, g.scale_X, g.scale_Y, 
                              g.color_r, g.color_g, g.color_b, g.texture_ID);
    }
}
