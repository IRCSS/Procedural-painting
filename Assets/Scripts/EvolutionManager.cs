using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;


[System.Serializable]
public class StageSeries
{
    public int numberOfStagesInTheSeries;
    public ScaleStage seriesSetting;
}


public class EvolutionManager : MonoBehaviour
{

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Public
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    [Header("Image")]
    public  Texture               ImageToReproduce;                          // This image is used for the evolution algo and is the ground truth
     public  bool                 blackAnwWhite;                             // wether the image should be painted in black and white. Would be easier to paint
    [Header("Stages")]
    public  StageSeries[]         series      = new StageSeries[1];          

    [Header("Soft References")]
    public   Compute_Shaders      compute_shaders;                           // All the boiler plate code, binding and references for the compute shaders. Look in the comments in the struct for more info 

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Private
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private ScaleStage[]          stages;
    private RenderTexture         current_background;                         // This is the texture used to clear the background with. Each stage updates its newest advances in to this once iti s deintialized
    private uint                  generation_identifier = 0;                 // This number specifies how many generations have already gone by. 
    private uint                  current_stage;

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // START
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    void Start()
    {


        // ____________________________________________________________________________________________________
        // Construct the scale stages
        List<ScaleStage> temp = new List<ScaleStage>();

        for(int i =0; i < series.Length; i++)
        {
            for(int j = 0; j< series[i].numberOfStagesInTheSeries; j++)
            {
                ScaleStage ss = new ScaleStage(series[i].seriesSetting);
                temp.Add(ss);
            }
        }

        stages = temp.ToArray();

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

        current_background = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height, 0);
        current_background.Create();
        Graphics.Blit(Texture2D.whiteTexture, current_background);


        // ____________________________________________________________________________________________________
        // Stage initialisation

        current_stage = (uint) stages.Length - 1;
        stages[current_stage].initialise_stage(ImageToReproduce, current_background, compute_shaders, blackAnwWhite, current_stage);
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // UPTADE
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void Update()
    {

        generation_identifier++;
        stages[current_stage].update_stage(generation_identifier);

        if (Input.GetKeyDown(KeyCode.N))
        {
            stages[current_stage].deinitialize_stage(ref current_background);
            current_stage--;
            stages[current_stage].initialise_stage(ImageToReproduce, current_background, compute_shaders, blackAnwWhite, current_stage);

        }

    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Destructor
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void OnDestroy()
    {
        stages[current_stage].deinitialize_stage(ref current_background);
    }
}
