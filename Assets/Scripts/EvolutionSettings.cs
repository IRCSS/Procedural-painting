using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ----------------------------------------------------------------------------------------------------------------------------------------------------------------
// Setting Data Types
// ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    // I am using classes for these so that they can contain default values. You can think of these as a methodless data only structs

// ____________________________________________________________________________________________________
// Settings of poppulation pool, mutation and genes

[System.Serializable]
public class Evolution_Settings
{
    public  uint       populationPoolNumber          = 64           ;            // larger population pool could lead to reducing number of generations required to get to the answer however increases the memory overhead
    public  uint       maximumNumberOfBrushStrokes   = 64           ;            // this controls how many brush strokes can be used to replicate the image aka how many genes a population has
    public  Texture    brushTexture                                 ;            // four textures in one texture. R, G, B and A each hold a texture of its own

   
    public  float      mutationChance                = 0.015f       ;            // this is the probablity of an individual gene mutating 
    public  float      brushSizeLowerBound           = 0.25f        ;            // lowest possible brush size that can appear
    public  float      brushSizeHigherBound          = 4.0f         ;            // biggest possible brush size that can appear. This value changes as more brushes are laid down. First bigger brush strokes are layed donw and on top of that smaller ones
}

// ____________________________________________________________________________________________________
// Settings of the scale space, gaussian and sobel filter
[System.Serializable]
public class Scale_Settings
{
    public float       sigma                         = 0.1f         ;            // standard deviation of the gaussian filter, which controls the weight of each sample in the averaging// standard deviation of the gaussian filter
    public float       gaussian_kernel_size          = 16           ;            // large kernel size create better quality and more blur with cost of performance. bigger kernel -> more samples 
    public int         sobel_step_size               = 1            ;            // how large is the step to left and right for samples use to measure image gradient
}

// ____________________________________________________________________________________________________
// Fittness Function settings
[System.Serializable]
public class Fitness_Settings
{
    public  float      hueWeight                     = 0.333f       ;            // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      satWeight                     = 0.333f       ;            // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      valWeight                     = 0.333f       ;            // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      fitnessPowFactor              = 2.0f         ;            // Controls how aggersivly the fitness function should favour those in the population pool which are fitter. Higher value for this means faster descend to local minima, but also a possibility of getting stuck there
}