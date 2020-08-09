using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GlobalSettings                                                                 // Holds settings that are true for all stages. In construction of each stage, this settings are copied over as local stage settings
{
    // ____________________________________________________________________________________________________
    // Member Values

    [Tooltip("Used as the brush strokes. 4 brush textures packed in " +
        "one texture R, G, B and A channel each hold a texutre.")]
    public Texture     brushTexture;                   
    
    [Tooltip("This is the probablity of an individual gene mutating " +
        "Higher mutation chance might mean faster results. But also " +
        "More unstable/ chaotic paintings. ")]
    public  float      mutationChance                = 0.015f    ;    
    
    [Tooltip("How important is it for the AI to get the "  +
        "value/luminacity/darkness of the image. This is " +
        "weighted against the color weight")]
    public  float      LuminacityWeight              = 1f        ;     
    
    [Tooltip("How important is it for the AI to get the " +
        "hue and saturation of the image. This is "       +
        "weighted against the Luminacity Wieght")]
    public  float      ColorWeight                   = 1f        ;      
    
    [Tooltip("Controls how aggersivly the fitness function should "   +
        "favour those in the population pool which are fitter. "      +
        "Higher value for this means faster descend to local minima," +
        " but also a possibility of getting stuck there")]
    public  float      fitnessPowFactor              = 8.0f      ;

    // ____________________________________________________________________________________________________
    // Constructors

    public GlobalSettings()                                                                 // Defaul Constructor
    {
        mutationChance        = 0.015f    ;
        LuminacityWeight      = 1f        ;   
        ColorWeight           = 1f        ;   
        fitnessPowFactor      = 8.0f      ;   
    }

     public GlobalSettings(GlobalSettings other)                                            // Copy Constructor
    {
        mutationChance        = other.mutationChance       ;
        LuminacityWeight      = other.LuminacityWeight     ;   
        ColorWeight           = other.ColorWeight          ;   
        fitnessPowFactor      = other.fitnessPowFactor     ;   
    }
}

// ========================================================================================================

[System.Serializable]
public class PerStageSettings                                              // Holds the per stage settings. based on each of these settings, a certain number of stages are created
{
    // ____________________________________________________________________________________________________
    // Member Values
    [Tooltip("How many copies of this stages do you want? "      +
        "Higher number means more brush strokes are added with " +
        "these settings one after another. If you put 5 here "   +
        "for example, you will have 5 stages coming after"       +
        "another in the search")]

    public uint        numberOfStagesInTheSeries     = 1;
    [Tooltip("Number of population in the gentic algo. In this case"  +
        "This is the number of parallel attempts the AI is going to " +
        "do to paint this scene. larger population pool could lead "  +
        "to reducing number of generations required to get to the "   +
        "answer however increases the memory overhead")]

    public uint        populationPoolNumber          = 128;
    [Tooltip("this controls how many brush strokes can be "       +
        "used to replicate the image per population, per stage. " +
        "In genetic algo terms, tis is how many genes a population has")]
    public  uint       maximumNumberOfBrushStrokes   = 32     ;         
    
    [Tooltip("lowest possible brush size that can appear")]
    public  float      brushSizeLowerBound           = 0.25f  ;

    [Tooltip(" biggest possible brush size that can appear. " +
        "This value changes as more brushes are laid down. "  +
        "First bigger brush strokes are layed donw and on "   +
        "top of that smaller ones")]
    public  float      brushSizeHigherBound          = 4.0f   ;

    [Tooltip("You can provide the stage with a costume mask "   +
        "which highlights where you want the algo to focus on " +
        "For example, if you want to have highdetails on the "  +
        "eys, you can make a mask where everything is black"    +
        "except the eyes. The algo will focus on painting"      +
        "only the eyes. ")]
    public  Texture    costume_mask                           ;

    [Tooltip("standard deviation of the gaussian filter, " +
        "which controls the weight of each sample "        +
        "in the averaging. You can think of this as "      +
        "the blur factor. Higher blur means focus on "     +
        "stronger features which survive the blur")]
    public  float      sigma                         = 16f    ;            

    [Tooltip("large kernel size create better quality and more" +
        " blur with cost of performance. "                      +
        "bigger kernel -> more samples ")]
    public  int        gaussian_kernel_size          = 16     ;

    [Tooltip("how large is the step to left and right "     +
        "for samples use to measure image gradient. This"   +
        "Should scale together with the gaussian steps. "   +
        "If you want to focus on bigger features, increase" +
        "the gaussian kernel and sobel step together. ")]
    public  int        sobel_step_size               = 1      ;

    [Tooltip("After the gaussian+sobel "                            +
        "this threshold is applied to focus on finer details."      +
        "Higher values means it will only focus on painting strong" +
        "edges. Set to -1 if you want this stage to focus on everything")]
    public  float      position_domain_threshold     = 0.1f   ;

    [Tooltip("Wether it should constrain the painting "   +
        "to only a specific region. This mask can also "  +
        "be automaticly generated by the Gaussian/ Sobel" +
        "or a hand painted mask passed on by user")]
    public  bool       apply_mask                    = true   ;

    // ____________________________________________________________________________________________________
    // Constructors

    public PerStageSettings()                                                              // Defaul Constructor
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

     public PerStageSettings(PerStageSettings other)                                       // Copy Constructor
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

// ========================================================================================================
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SearchSettings", order = 1)]
public class SearchSettings : ScriptableObject                                          // This asset contains all the settings specific to this run
{
    public GlobalSettings     runGlobalSettings   = new GlobalSettings();               // Settings that are applied to all the stages created 
    public PerStageSettings[] stagesSeries        = new PerStageSettings[1];            // Settings that differ from stage to stage
}
