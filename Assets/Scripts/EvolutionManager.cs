using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;


[System.Serializable]
public class StageSeries       // A stage series describes several stages that have the exact same settings. This is a UI edtior sugar so that I dont have to deal with lists that are 200 long
{
    public int        numberOfStagesInTheSeries = 1;
    public ScaleStage seriesSetting;

}



public class EvolutionManager : MonoBehaviour
{

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Public
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    [Header("Image")]
    [Tooltip("Put here the image you want to have painted. " +
        "Make sure it is not bigger than 1080p pixels and is square.")]
    public  Texture               ImageToReproduce;                          // This image is used for the evolution algo and is the ground truth

    [Tooltip("If it is a black and white picture, tick this. " +
        "It helps the algo narrow down the search and "        +
        "not worry about color")]
    public  bool                  blackAnwWhite;                             // wether the image should be painted in black and white. Would be easier to paint

    [Header("Stages")]
    [Tooltip("Drag and drop one of the available settings in the " +
        "setting folders or create your own")]
    public  SearchSettings        run_settings;                              // Holds all the actual settings concerning the stages

    [Header("Soft References")]
    public   Compute_Shaders      compute_shaders;                           // All the boiler plate code, binding and references for the compute shaders. Look in the comments in the struct for more info 

    [Header("Debug")]
    [Tooltip("This shows the current area the algo is focusing on" +
        "White indicates the focus areas")]
    public RenderTexture          current_search_domain_visualisation;

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // Private
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private ScaleStage[]          stages;                                    // The actual list of stages is created from the stage series and setting and is saved in this array
    private RenderTexture         current_background;                        // This is the texture used to clear the background with. Each stage updates its newest advances in to this once iti s deintialized
    private uint                  generation_identifier = 0;                 // This number specifies how many generations have already gone by. 
    private uint                  current_stage;                             // The stage wwhich is currently being worked on

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // START
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    void Start()
    {


        if (ImageToReproduce.width > 1024 || ImageToReproduce.height > 1024) Debug.LogError("image provided is bigger than 1024. This probabaly not what you want");

        current_search_domain_visualisation = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height, 0);
        

        // ____________________________________________________________________________________________________
        // Construct the scale stages
        List<ScaleStage> temp = new List<ScaleStage>();

        for(int i =0; i < run_settings.stagesSeries.Length; i++)
        {
            for(int j = 0; j< run_settings.stagesSeries[i].numberOfStagesInTheSeries; j++)
            {
                ScaleStage ss = new ScaleStage(run_settings.runGlobalSettings, run_settings.stagesSeries[i]);
                temp.Add(ss);
            }
        }

        stages = temp.ToArray();

        // ____________________________________________________________________________________________________
        // Camera Initialisation
        Camera main_cam = Camera.main;
        if (!main_cam) Debug.LogError("Main Camera not found, add a camera to " +
            "the scene and add the main camera tag to it");

        main_cam.orthographic     = true;                                                                     // Make sure the camera is ortho. Perspecitve camera has a transformation matrix which will screw with everything
        main_cam.aspect           = (float)ImageToReproduce.width/ (float)ImageToReproduce.height;
        main_cam.orthographicSize = 1;
        // ____________________________________________________________________________________________________
        // Command Buffer initialization

        compute_shaders.Construct_Computes();                                                                 // This sets up all the book keeping needed forthe shaders across different stages such as finding the kernel handels

        current_background = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height, 0);           // The current background is used to clear the render target of the camera after each drawing. Each stage paints on top of results of the previous stage
        current_background.Create();
        Graphics.Blit(Texture2D.whiteTexture, current_background);


        // ____________________________________________________________________________________________________
        // Stage initialisation

        current_stage = (uint) stages.Length - 1;                                                             // Starting from the last stage and counting down.
        stages[current_stage].initialise_stage(ImageToReproduce, 
            current_background, compute_shaders, blackAnwWhite, current_stage, this);
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // UPTADE
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void Update()
    {

        generation_identifier++;
        bool isStuckInLocalMinima = stages[current_stage].update_stage(generation_identifier);                // It switchs from one stage to the next, if the current stage is stuck in local minima 

        //isStuckInLocalMinima = false;

        if ((Input.GetKeyDown(KeyCode.N) || isStuckInLocalMinima) && current_stage>0)
        {
            stages[current_stage].deinitialize_stage(ref current_background);
            current_stage--;
            stages[current_stage].initialise_stage(ImageToReproduce, current_background, compute_shaders, blackAnwWhite, current_stage, this);

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
