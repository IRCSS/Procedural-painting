using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ScaleStage
{









    // ____________________________________________________________________________________________________
    // Private

    private Texture         ImageToReproduce;                        // This image is used for the evolution algo and is the ground truth. Passed on from outside
    private RenderTexture   clear_base;                              // This is the image used to clear the render target. After the first stage, this would be the basis which the second stage would paint on
    private Compute_Shaders compute_shaders;


    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Constructor
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void initialise_stage(Texture ImageToReproduce, RenderTexture clear_with_base, Compute_Shaders shaders)
    {
        compute_shaders = shaders;
    }



    public void deinitialize_stage()
    {

    }
}
