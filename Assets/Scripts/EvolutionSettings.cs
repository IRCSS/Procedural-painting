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
    

    public Evolution_Settings()                                                  // Default construcotr
    {
        populationPoolNumber          = 64      ;
        maximumNumberOfBrushStrokes   = 64      ;
        mutationChance                = 0.015f  ;
        brushSizeLowerBound           = 0.25f   ;
        brushSizeHigherBound          = 4.0f    ;
    }

    public Evolution_Settings(Evolution_Settings other)                          // Copy Constructor
    {
        populationPoolNumber          = other.populationPoolNumber         ;
        maximumNumberOfBrushStrokes   = other.maximumNumberOfBrushStrokes  ;
        brushTexture                  = other.brushTexture                 ;
        mutationChance                = other.mutationChance               ;
        brushSizeLowerBound           = other.brushSizeLowerBound          ;
        brushSizeHigherBound          = other.brushSizeHigherBound         ;
    }

}

// ____________________________________________________________________________________________________
// Settings of the scale space, gaussian and sobel filter
[System.Serializable]
public class Scale_Settings
{
    public float       sigma                         = 0.1f         ;            // standard deviation of the gaussian filter, which controls the weight of each sample in the averaging// standard deviation of the gaussian filter
    public int         gaussian_kernel_size          = 16           ;            // large kernel size create better quality and more blur with cost of performance. bigger kernel -> more samples 
    public int         sobel_step_size               = 1            ;            // how large is the step to left and right for samples use to measure image gradient
    public float       position_domain_threshold     = 0.1f         ;
    public bool        apply_mask                    = true         ;
    public Scale_Settings()                          
     {
         sigma                         = 0.1f    ;
         gaussian_kernel_size          = 16      ;
         sobel_step_size               = 1       ;
         position_domain_threshold     = 0.1f    ;
         apply_mask                    = true    ;
     }

   public Scale_Settings(Scale_Settings other)                                   // Copy Constructor
    {
        sigma                         = other.sigma                    ;
        gaussian_kernel_size          = other.gaussian_kernel_size     ;
        sobel_step_size               = other.sobel_step_size          ;
        position_domain_threshold     = other.position_domain_threshold;
        apply_mask                    = other.apply_mask               ;
    }
}

// ____________________________________________________________________________________________________
// Fittness Function settings
[System.Serializable]
public class Fitness_Settings
{
    public  float      colorTotalWeight              = 1.0f         ;            // This is multiplied with all hsv channels fitness calculaiton and decides how much weight the entire color part of the fitness has. Can be any value, it will then be weighted based on how much the gradient is weighted
    public  float      gradientTotalWeight           = 1.0f         ;            // How much should the gradient of the pixel be weighted for the total weight. Can be any positive value, will be normalized based on how hight the other values are
    public  float      hueWeight                     = 0.333f       ;            // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      satWeight                     = 0.333f       ;            // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      valWeight                     = 0.333f       ;            // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float      fitnessPowFactor              = 2.0f         ;            // Controls how aggersivly the fitness function should favour those in the population pool which are fitter. Higher value for this means faster descend to local minima, but also a possibility of getting stuck there

    public Fitness_Settings()
    {
        colorTotalWeight              = 1.0f     ;
        gradientTotalWeight           = 1.0f     ;
        hueWeight                     = 0.333f   ;
        satWeight                     = 0.333f   ;
        valWeight                     = 0.333f   ;
        fitnessPowFactor              = 2.0f     ;
    }

    public Fitness_Settings(Fitness_Settings other)
    {
        colorTotalWeight              = other.colorTotalWeight      ;
        gradientTotalWeight           = other.gradientTotalWeight   ;
        hueWeight                     = other.hueWeight             ;
        satWeight                     = other.satWeight             ;
        valWeight                     = other.valWeight             ;
        fitnessPowFactor              = other.fitnessPowFactor      ;
    }
}