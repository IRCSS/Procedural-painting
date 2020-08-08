using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GlobalSettings
{
    public Texture     brushTexture;                                          // four textures in one texture. R, G, B and A each hold a texture of its own
    public  float      mutationChance                = 0.015f    ;            // this is the probablity of an individual gene mutating 
    public  float      LuminacityWeight              = 1f        ;           
    public  float      ColorWeight                   = 1f        ;           
    public  float      fitnessPowFactor              = 8.0f      ;            // Controls how aggersivly the fitness function should favour those in the population pool which are fitter. Higher value for this means faster descend to local minima, but also a possibility of getting stuck there

    public GlobalSettings()
    {
        mutationChance        = 0.015f    ;
        LuminacityWeight      = 1f        ;   
        ColorWeight           = 1f        ;   
        fitnessPowFactor      = 8.0f      ;   
    }

     public GlobalSettings(GlobalSettings other)
    {
        mutationChance        = other.mutationChance       ;
        LuminacityWeight      = other.LuminacityWeight     ;   
        ColorWeight           = other.ColorWeight          ;   
        fitnessPowFactor      = other.fitnessPowFactor     ;   
    }
}

[System.Serializable]
public class PerStageSettings
{
    public  uint       numberOfStagesInTheSeries     = 1      ;
    public  uint       populationPoolNumber          = 128    ;            // larger population pool could lead to reducing number of generations required to get to the answer however increases the memory overhead
    public  uint       maximumNumberOfBrushStrokes   = 32     ;            // this controls how many brush strokes can be used to replicate the image aka how many genes a population has
    public  float      brushSizeLowerBound           = 0.25f  ;            // lowest possible brush size that can appear
    public  float      brushSizeHigherBound          = 4.0f   ;            // biggest possible brush size that can appear. This value changes as more brushes are laid down. First bigger brush strokes are layed donw and on top of that smaller ones
    public  Texture    costume_mask                           ;
    public  float      sigma                         = 16f    ;            // standard deviation of the gaussian filter, which controls the weight of each sample in the averaging// standard deviation of the gaussian filter
    public  int        gaussian_kernel_size          = 16     ;            // large kernel size create better quality and more blur with cost of performance. bigger kernel -> more samples 
    public  int        sobel_step_size               = 1      ;            // how large is the step to left and right for samples use to measure image gradient
    public  float      position_domain_threshold     = 0.1f   ;
    public  bool       apply_mask                    = true   ;
    

    public PerStageSettings()
    {
        numberOfStagesInTheSeries     = 1      ;
        populationPoolNumber          = 128    ;
        maximumNumberOfBrushStrokes   = 32     ;
        brushSizeLowerBound           = 0.25f  ;
        brushSizeHigherBound          = 4.0f   ;
        sigma                         = 16f    ; 
        gaussian_kernel_size          = 16     ; 
        sobel_step_size               = 1      ; 
        position_domain_threshold     = 0.1f   ;
        apply_mask                    = true   ;
    }

     public PerStageSettings(PerStageSettings other)
    {
        numberOfStagesInTheSeries     = other.numberOfStagesInTheSeries   ;
        populationPoolNumber          = other.populationPoolNumber        ;
        maximumNumberOfBrushStrokes   = other.maximumNumberOfBrushStrokes ;
        brushSizeLowerBound           = other.brushSizeLowerBound         ;
        brushSizeHigherBound          = other.brushSizeHigherBound        ;
        sigma                         = other.sigma                       ; 
        gaussian_kernel_size          = other.gaussian_kernel_size        ; 
        sobel_step_size               = other.sobel_step_size             ; 
        position_domain_threshold     = other.position_domain_threshold   ;
        apply_mask                    = other.apply_mask                  ;
    }

}


[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SearchSettings", order = 1)]
public class SearchSettings : ScriptableObject
{
    public GlobalSettings     runGlobalSettings   = new GlobalSettings();
    public PerStageSettings[] stagesSeries        = new PerStageSettings[1];
}
