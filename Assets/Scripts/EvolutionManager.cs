using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;




public class EvolutionManager : MonoBehaviour
{

    [Header("Image")]
    public  Texture                ImageToReproduce;                          // This image is used for the evolution algo and is the ground truth

    [Header("Stages")]
    public ScaleStage              one_stage;

    [Header("Soft References")]
    public   Compute_Shaders       compute_shaders;                           // All the boiler plate code, binding and references for the compute shaders. Look in the comments in the struct for more info 
    private uint generation_identifier = 0;                                   // This number specifies how many generations have already gone by. 

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // START
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    void Start()
    {


        // ____________________________________________________________________________________________________
        // Camera Initialisation
        Camera main_cam = Camera.main;
        if (!main_cam) Debug.LogError("Main Camera not found, add a camera to " +
            "the scene and add the main camera tag to it");

        main_cam.orthographic    = true;
        main_cam.aspect          = (float)ImageToReproduce.width/ (float)ImageToReproduce.height;
       
        // ____________________________________________________________________________________________________
        // Command Buffer initialization

        compute_shaders.Construct_Computes();                                                                 // This sets up all the book keeping needed forthe shaders across different stages such as finding the kernel handels




        // ____________________________________________________________________________________________________
        // Stage initialisation

        one_stage.initialise_stage(ImageToReproduce, null, compute_shaders, 0);
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // UPTADE
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void Update()
    {

        generation_identifier++;
        one_stage.update_stage(generation_identifier);
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Destructor
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void OnDestroy()
    {
        one_stage.deinitialize_stage();
    }
}
