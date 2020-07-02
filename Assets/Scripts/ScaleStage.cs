using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ScaleStage
{

    [Header("Scale Settings")]
    public float       sigma                        = 4.0f;          // standard deviation of the gaussian filter, which controls the weight of each sample in the averaging// standard deviation of the gaussian filter
    public float       gaussian_kernel_size         = 16.0f;         // large kernel size create better quality and more blur with cost of performance. bigger kernel -> more samples 
    public int         sobel_step_size              = 1;             // how large is the step to left and right for samples use to measure image gradient

    [Header("Evelution Settings")]
    public  int        populationPoolNumber         = 64;            // larger population pool could lead to reducing number of generations required to get to the answer however increases the memory overhead
    public  int        maximumNumberOfBrushStrokes  = 64;            // this controls how many brush strokes can be used to replicate the image aka how many genes a population has
    public  Texture    brushTexture;                                 // four textures in one texture. R, G, B and A each hold a texture of its own

    public  bool       blackAnwWhite                = false;         // wether the image should be painted in black and white. Would be easier to paint
    public  float      mutationChance               = 0.01f;         // this is the probablity of an individual gene mutating 
    public  float      brushSizeLowerBound          = 0.1f;          // lowest possible brush size that can appear
    public  float      brushSizeHigherBound         = 1.0f;          // biggest possible brush size that can appear. This value changes as more brushes are laid down. First bigger brush strokes are layed donw and on top of that smaller ones

    public  float      hueWeight                    = 0.3333f;       // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      satWeight                    = 0.3333f;       // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      valWeight                    = 0.3333f;       // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      fitnessPowFactor             = 4;             // Controls how aggersivly the fitness function should favour those in the population pool which are fitter. Higher value for this means faster descend to local minima, but also a possibility of getting stuck there



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
