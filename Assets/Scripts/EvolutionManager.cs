using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

public class EvolutionManager : MonoBehaviour
{

    [Header("Image")]
    public  Texture                ImageToReproduce;                          // This image is used for the evolution algo and is the ground truth
    public  int                    O_scaleSpaceDepth;
    private RenderTexture[]        O_scales;
            
    [Header("Evelution Settings")]
    public  int                    populationPoolNumber;                      // larger population pool could lead to reducing number of generations required to get to the answer however increases the memory overhead
    public  int                    maximumNumberOfBrushStrokes;               // this controls how many brush strokes can be used to replicate the image aka how many genes a population has
    public  Texture                brushTexture;                              // four textures in one texture. R, G, B and A each hold a texture of its own

    [Header("Soft References")]
    public  ComputeShader          compute_fitness_function;                  // holds all code for the fitness function of the genetic evolution algo
    public  ComputeShader          compute_selection_functions;               // this file contains the compute kernels for the adjusting the fitness to accmulative weighted probablities, selecting parents, cross over as well as mutation

    [Header("Debug")]
    public Texture                 user_set_forged;                           // I used this to test if my fitness function works. In this texture I can insert a copy of the original which is slightly altered and see what value my fitness function gives me for that image

    private ComputeBuffer[]        population_genes;                          // the genes are kept alive on the GPU memory. Each compute buffer, holds a structured buffer which represents a population member
    private ComputeBuffer          per_pixel_fitnes_buffer;                   // holds the per pixel info on how close a pixel is to the solution. Reused for each population member
    private ComputeBuffer          per_row_sum_buffer;                        // This buffer is used to sum up all the pixel in a row. It has one entry per row, aka number of pixels in height. Reused for each population member
    private ComputeBuffer          population_pool_fitness_buffer;            // This array contains the fitness of each of the population pool members. One member per population member
    private ComputeBuffer          population_accumlative_prob_buffer;        // This buffer contains the result of transforming the fitness values to an wieghted accmulative probabilities form
    private ComputeBuffer          second_gen_parents_ids_buffer;             // a buffer of pairs of IDs. Each id refers to one of the parents which is used for the cross over algo. Papulated in Computeshader
    private PopulationMember[]     populations;                               // The cpu container which holds info about population
    private Material               rendering_material;                        // material used to actually render the brush strokes
                                   
                                   
    private CommandBuffer          effect_command_buffer;                     // this command buffer encapsulates everything that happens in the effect
    private RenderTexture          active_texture_target;                     // the population is renedred in this render texture, it is compared per pixel for fitness in compute later
    private RenderTexture          compute_forged_in_render_texture;
    private RenderTexture          debug_texture;                             // texture used to visualize the compute calclulations

    private Camera                 main_cam;

    private int per_pixel_fitness_kernel_handel;                              // Handels used to dispatch compute. This function calculatis fitness on level of pixel by comparing it to orginal
    private int sun_rows_kernel_handel;                                       // Handels used to dispatch compute. Sums up each pixel of a row to a single value. The result is an array of floats
    private int sun_column_kernel_handel;                                     // Handels used to dispatch compute. Sums up the sums of rows that are saved in a single column to one float.
    private int trans_fitness_to_prob_handel;                                 // Handel used to dispatch compute.  this is used to convert the fitness values which are already normalized to an accumaletive weighted probabilities for sampling 
    private int debug_hash_handel;                                            // Used for debuging how well the hash creation function is working
    private int parent_selection_handel;                                      // used for selecting a pair of parents for each second geneariton of population members  

    private int generation_identifier = 0;                                    // This number specifies how many generations have already gone by. 

    void Start()
    {

        // ____________________________________________________________________________________________________
        // Textures Initialization
        active_texture_target    = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height, 
            0, RenderTextureFormat.ARGB32);
        active_texture_target.Create();
        compute_forged_in_render_texture = new RenderTexture(active_texture_target);
        compute_forged_in_render_texture.Create();

        debug_texture = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height,
            0, RenderTextureFormat.ARGB32);
        debug_texture.enableRandomWrite = true;
        debug_texture.Create();

        // ____________________________________________________________________________________________________
        // Camera Initialisation
        main_cam = Camera.main;
        if (!main_cam) Debug.LogError("Main Camera not found, add a camera to " +
            "the scene and add the main camera tag to it");

        main_cam.orthographic    = true;
        main_cam.aspect          = (float)ImageToReproduce.width/ (float)ImageToReproduce.height;  
        main_cam.clearFlags      = CameraClearFlags.Color;
        main_cam.backgroundColor = Color.black;
        main_cam.targetTexture = active_texture_target;
        Screen.fullScreenMode = FullScreenMode.Windowed;

        Screen.SetResolution(ImageToReproduce.width, ImageToReproduce.height, false);
        // ____________________________________________________________________________________________________
        // Materials

        rendering_material = new Material(Shader.Find("Unlit/PopulationShader"));
        if (!rendering_material) Debug.LogError("Couldnt find the population shader");

        rendering_material.SetTexture("_MainTex", brushTexture);



        // ____________________________________________________________________________________________________
        // CPU Arrays initalization
        
        population_genes                   = new ComputeBuffer[populationPoolNumber]; 
        Genes[] initial_gene               = new Genes[maximumNumberOfBrushStrokes];
        populations                        = new PopulationMember[populationPoolNumber];

        // ____________________________________________________________________________________________________
        // Compute buffers Initialization
        int pixel_count_in_image           = active_texture_target.width * active_texture_target.height;
        per_pixel_fitnes_buffer            = new ComputeBuffer(pixel_count_in_image,           sizeof(float) * 4); 
        per_row_sum_buffer                 = new ComputeBuffer(active_texture_target.height,   sizeof(float)    );
        population_pool_fitness_buffer     = new ComputeBuffer(populationPoolNumber,           sizeof(float)    );
        population_accumlative_prob_buffer = new ComputeBuffer(populationPoolNumber,           sizeof(float)    );                            // You could combin this and the fitnes buffer together, I am keeping them seprated for the sake of debuging ease
        second_gen_parents_ids_buffer      = new ComputeBuffer(populationPoolNumber,           sizeof(int)   * 2);

        // ____________________________________________________________________________________________________
        // Command Buffer initialization
        per_pixel_fitness_kernel_handel    = compute_fitness_function.FindKernel("CS_Fitness_Per_Pixel");
        sun_rows_kernel_handel             = compute_fitness_function.FindKernel("CS_Sum_Rows");
        sun_column_kernel_handel           = compute_fitness_function.FindKernel("CS_Sum_Column");
        trans_fitness_to_prob_handel       = compute_selection_functions.FindKernel("CS_transform_fitness_to_probability");
        debug_hash_handel                  = compute_selection_functions.FindKernel("CS_debug_wang_hash");
        parent_selection_handel            = compute_selection_functions.FindKernel("CS_parent_selection");
        effect_command_buffer = new CommandBuffer
        {
            name = "Effect_Command_Buffer",
        };

        // -----------------------
        // Command Buffer Bindings
        //effect_command_buffer.SetRenderTarget(active_texture_target);
        effect_command_buffer.SetGlobalBuffer("_per_pixel_fitness_buffer",                  per_pixel_fitnes_buffer);
        effect_command_buffer.SetGlobalBuffer("_rows_sums_array",                           per_row_sum_buffer);
        effect_command_buffer.SetGlobalBuffer("_population_fitness_array",                  population_pool_fitness_buffer);
        effect_command_buffer.SetGlobalBuffer("_population_accumlative_probablities_array", population_accumlative_prob_buffer);
        effect_command_buffer.SetGlobalBuffer("_second_gen_parent_ids",                     second_gen_parents_ids_buffer);
        
        // -----------------------
        // Compute Shader Bindings
        compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_original",          ImageToReproduce);
        compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_forged",            compute_forged_in_render_texture);
       // compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_forged",            user_set_forged);                     // Used for debuging porpuses. Passing on a user given forged to test the fitness function
        compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_debug_texture",     debug_texture);
        compute_fitness_function.SetInt       ("_image_width",      ImageToReproduce.width);
        compute_fitness_function.SetInt       ("_image_height",     ImageToReproduce.height);
                                              
        compute_selection_functions.SetTexture(debug_hash_handel, "_debug_texture", debug_texture);
        compute_selection_functions.SetInt    ("_population_pool_size",    populationPoolNumber);
        compute_selection_functions.SetInt    ("_genes_number_per_member", maximumNumberOfBrushStrokes);
        compute_selection_functions.SetInt    ("_image_width",             ImageToReproduce.width);
        compute_selection_functions.SetInt    ("_image_height",            ImageToReproduce.height);

        Debug.Log(string.Format("Dispatch dimensions for compute shaders will be: " +
            "{0}, {1} thread groups and 32 in 32 threads in each group. " +
            "Image should be a multiple of 32 in dimesions", ImageToReproduce.width / 32, ImageToReproduce.height / 32));

        if (ImageToReproduce.width % 32 != 0 || ImageToReproduce.height % 32 != 0)
            Debug.LogError("image is not multiply of 32. Either change the image dimensions or" +
             "The threadnumbers set up in the compute shaders!");

        // ____________________________________________________________________________________________________
        // Command Buffer Recording

        for (int i = 0; i<populationPoolNumber; i++){


            // -----------------------
            // Population Pool first gen initializatiopn
            population_genes[i] = new ComputeBuffer(maximumNumberOfBrushStrokes, sizeof(float) * 8 + sizeof(int) * 1);
            CPUSystems.InitatePopulationMember(ref initial_gene);

            population_genes[i].SetData(initial_gene);

            populations[i] = new PopulationMember()
            {
                population_Handel = i,
            };

            // -----------------------
            // Draw Population Pool Member
            effect_command_buffer.ClearRenderTarget(true, true, Color.black);
            effect_command_buffer.SetGlobalBuffer("Brushes_Buffer", population_genes[i]);
            effect_command_buffer.DrawProcedural(Matrix4x4.identity, rendering_material, 0, 
                MeshTopology.Triangles, maximumNumberOfBrushStrokes * 6);

            // -----------------------
            //Compute Fitness
            effect_command_buffer.CopyTexture(active_texture_target, compute_forged_in_render_texture);                                             
            effect_command_buffer.SetGlobalInt("_population_id_handel", i);

            // thread groups are made up 32 in 32 threads. The image should be a multiply of 32. 
            // so ideally padded to a power of 2. For other image dimensions, the threadnums
            // should be changed in the compute shader for the kernels as well as here the 
            // height or width divided by 32. Change it only if you know what you are doing 
            effect_command_buffer.DispatchCompute(compute_fitness_function, per_pixel_fitness_kernel_handel,
                ImageToReproduce.width / 32, ImageToReproduce.height / 32, 1);

            // dispatch one compute per row in groups of 32
            effect_command_buffer.DispatchCompute(compute_fitness_function, sun_rows_kernel_handel,
                ImageToReproduce.height / 32, 1, 1);

            //// dispatch a single thread
            effect_command_buffer.DispatchCompute(compute_fitness_function, sun_column_kernel_handel,
                1, 1, 1);


        }

        //effect_command_buffer.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget);                                                   // Used for debuging the output of the per pixel comute calculations

        // -----------------------
        // Convert Fitness to accumlative weighted probablities

        // Dispatch single thread. 
        effect_command_buffer.DispatchCompute(compute_selection_functions, trans_fitness_to_prob_handel, 1, 1, 1);

        //effect_command_buffer.DispatchCompute(compute_selection_functions, debug_hash_handel,
        //      ImageToReproduce.width / 8, ImageToReproduce.height / 8, 1);
        //effect_command_buffer.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget);                                                   // used to debug how well the hashing works

        if (populationPoolNumber % 32 != 0 )
            Debug.LogError("The population pool number is set to" + populationPoolNumber +
             "Which is not multiple of 32. Either change this number or numThreads in the compute shader!");

        effect_command_buffer.DispatchCompute(compute_selection_functions, parent_selection_handel, populationPoolNumber / 32, 1, 1);

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, effect_command_buffer);


    }

    private void Update()
    {

        generation_identifier++;
        compute_selection_functions.SetInt("_generation_number", generation_identifier);                                                       // This number is used in the compute shader to differention between rand number geneartion between different generations


        // debug_population_member_fitness_value();
        // debug_fitness_to_probabilities_transformation();
        // debug_parent_selection();
    }


    private void OnDestroy()
    {
        foreach(ComputeBuffer cb in population_genes)
        {
            cb.Release();
        }

        per_pixel_fitnes_buffer.Release();
        per_row_sum_buffer.Release();
        population_pool_fitness_buffer.Release();
        population_accumlative_prob_buffer.Release();
        second_gen_parents_ids_buffer.Release();
    }


    struct parentPair
    {
        public int parentX, parentY;
    }

    void debug_parent_selection()
    {
        parentPair[] second_gen_parents_ids = new parentPair[populationPoolNumber];
        second_gen_parents_ids_buffer.GetData(second_gen_parents_ids);
        for (int i = 0; i < populationPoolNumber; i++)
        {
            print(string.Format("second generation {0}, has the parents {1} and {2}", 
                i, second_gen_parents_ids[i].parentX, second_gen_parents_ids[i].parentY));
        }
    }


    void debug_fitness_to_probabilities_transformation()
    {
        float[] accmulative_probabilities = new float[populationPoolNumber];
        population_accumlative_prob_buffer.GetData(accmulative_probabilities);
        for (int i = 0; i < populationPoolNumber; i++)
        {
            print(string.Format("population {0}, has the accumalative weighted probablity {1}", i, accmulative_probabilities[i]));
        }
    }

    /// <summary>
    /// Pulls the fitness value data from the GPU and prints them out per member
    /// </summary>
    void debug_population_member_fitness_value()
    {
        float[] population_fitness_cpu_values = new float[populationPoolNumber];
        population_pool_fitness_buffer.GetData(population_fitness_cpu_values);

        for (int i = 0; i < populationPoolNumber; i++)
        {
            print(string.Format("population {0}, has fitnesss {1}", i, population_fitness_cpu_values[i]));
        }
    }

    void texture_to_RenderTexture(Texture toConvert, RenderTexture toBlitTo)
    {
        RenderTexture temp_ref = RenderTexture.active;
        RenderTexture.active   = toBlitTo;

        Graphics.Blit(toConvert, toBlitTo);
        RenderTexture.active   = temp_ref;
    }

    string population_member_to_string(Genes[] arrayToString)
    {
        string debugStringOut = "";
        for(int i =0; i< arrayToString.Length; i++)
        {
            debugStringOut += GenesToString(arrayToString[i]);
        }
        return debugStringOut;
    }

        string GenesToString(Genes g)
    {
        return string.Format("position: ({0}, {1}), rotation: {2}, scale: ({3}, {4}), color: ({5}, {6}, {7}), textureID: {8}",
                              g.position_X, g.position_Y, g.z_Rotation, g.scale_X, g.scale_Y, 
                              g.color_r, g.color_g, g.color_b, g.texture_ID);
    }
}
