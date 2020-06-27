using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

public class EvolutionManager : MonoBehaviour
{

    [Header("Image")]
    public  Texture                ImageToReproduce;                          // This image is used for the evolution algo and is the ground truth
            
    [Header("Evelution Settings")]
    public  int                    populationPoolNumber;                      // larger population pool could lead to reducing number of generations required to get to the answer however increases the memory overhead
    public  int                    maximumNumberOfBrushStrokes;               // this controls how many brush strokes can be used to replicate the image aka how many genes a population has
    public  Texture                brushTexture;                              // four textures in one texture. R, G, B and A each hold a texture of its own
    public  float                  mutationChance = 0.01f;
    [Header("Soft References")]
    public  ComputeShader          compute_fitness_function;                  // holds all code for the fitness function of the genetic evolution algo
    public  ComputeShader          compute_selection_functions;               // this file contains the compute kernels for the adjusting the fitness to accmulative weighted probablities, selecting parents, cross over as well as mutation

    [Header("Debug")]
    public Texture                 user_set_forged;                           // I used this to test if my fitness function works. In this texture I can insert a copy of the original which is slightly altered and see what value my fitness function gives me for that image
    public bool                    turn_on_fitness_debug;
    
    private ComputeBuffer          population_pool_buffer;                    // Where the population memebers live. The length of this list is population number * number of genes (brush strokes) per population. It is an array of genes where the population member are implied through indexing and strides
    private ComputeBuffer          second_gen_population_pool_buffer;         // This buffer is used to write the members the second generation into. This is then coppied at the last stage in to the buffer above.
    private ComputeBuffer          per_pixel_fitnes_buffer;                   // holds the per pixel info on how close a pixel is to the solution. Reused for each population member
    private ComputeBuffer          per_row_sum_buffer;                        // This buffer is used to sum up all the pixel in a row. It has one entry per row, aka number of pixels in height. Reused for each population member
    private ComputeBuffer          population_pool_fitness_buffer;            // This array contains the fitness of each of the population pool members. One member per population member
    private ComputeBuffer          population_accumlative_prob_buffer;        // This buffer contains the result of transforming the fitness values to an wieghted accmulative probabilities form
    private ComputeBuffer          second_gen_parents_ids_buffer;             // a buffer of pairs of IDs. Each id refers to one of the parents which is used for the cross over algo. Papulated in Computeshader
    private ComputeBuffer          fittest_member_buffer;                     // This buffer contains only one element which is the info of the fittest member of the population pool. It is written to in the compute shader and later in the looop the fittest member is redrawn for visualisation. I am sure there are better ways of doing this without a structured buffer. 
    private Material               rendering_material;                        // material used to actually render the brush strokes
    private Material               fittest_rendering_material;                // this material is used to draw the fittest member of the population at the end of the lop after all calculations are done
                                   
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
    private int cross_over_handel;                                            // This compute shader breeds the second generation based on the parents and creates a second generation per population pool
    private int mutation_and_copy_handel;                                     // This copies over the second generation members to the main buffer to be rendered in the next frame and mutates some of the genes along the way

    private int generation_identifier = 0;                                    // This number specifies how many generations have already gone by. 

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // START
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

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
        main_cam.backgroundColor = Color.gray;
        //main_cam.targetTexture = active_texture_target;
        //Screen.fullScreenMode = FullScreenMode.Windowed;

        //Screen.SetResolution(ImageToReproduce.width, ImageToReproduce.height, false);
        // ____________________________________________________________________________________________________
        // Materials

        rendering_material = new Material(Shader.Find("Unlit/PopulationShader"));
        if (!rendering_material) Debug.LogError("Couldnt find the population shader");

        rendering_material.SetTexture("_MainTex", brushTexture);

        fittest_rendering_material = new Material(Shader.Find("Unlit/FittestRenderShader"));
        if (!fittest_rendering_material) Debug.LogError("could not find the sahder for rendering the fittest population member");

        fittest_rendering_material.SetInt("_genes_number_per_member", maximumNumberOfBrushStrokes);
        fittest_rendering_material.SetTexture("_MainTex", brushTexture);
        // ____________________________________________________________________________________________________
        // CPU Arrays initalization


        // ____________________________________________________________________________________________________
        // Compute buffers Initialization
        int pixel_count_in_image           = active_texture_target.width * active_texture_target.height;
        int total_number_of_genes          = populationPoolNumber * maximumNumberOfBrushStrokes;                                              // although population_pool is the array of populations, each population memberis implied through the max number of brushes. It is in parsing an array of genes.

        population_pool_buffer             = new ComputeBuffer(total_number_of_genes,          sizeof(float) * 8 + sizeof(int) * 1);          // for the stride size look ath the DNA.cs and defination of Genes. 
        second_gen_population_pool_buffer  = new ComputeBuffer(total_number_of_genes,          sizeof(float) * 8 + sizeof(int) * 1);
        per_pixel_fitnes_buffer            = new ComputeBuffer(pixel_count_in_image,           sizeof(float)); 
        per_row_sum_buffer                 = new ComputeBuffer(active_texture_target.height,   sizeof(float)    );                            // This will have one entry per row of the image. So as many as the height value of the render target. Each of these entries will hold the sum of that row
        population_pool_fitness_buffer     = new ComputeBuffer(populationPoolNumber,           sizeof(float)    );
        population_accumlative_prob_buffer = new ComputeBuffer(populationPoolNumber,           sizeof(float)    );                            // You could combin this and the fitnes buffer together, I am keeping them seprated for the sake of debuging ease
        second_gen_parents_ids_buffer      = new ComputeBuffer(populationPoolNumber,           sizeof(int)   * 2);
        fittest_member_buffer              = new ComputeBuffer(1,                              sizeof(float) + sizeof(int));

        // ____________________________________________________________________________________________________
        // Command Buffer initialization
        per_pixel_fitness_kernel_handel    = compute_fitness_function.FindKernel("CS_Fitness_Per_Pixel");
        sun_rows_kernel_handel             = compute_fitness_function.FindKernel("CS_Sum_Rows");
        sun_column_kernel_handel           = compute_fitness_function.FindKernel("CS_Sum_Column");
        trans_fitness_to_prob_handel       = compute_selection_functions.FindKernel("CS_transform_fitness_to_probability");
        debug_hash_handel                  = compute_selection_functions.FindKernel("CS_debug_wang_hash");
        parent_selection_handel            = compute_selection_functions.FindKernel("CS_parent_selection");
        cross_over_handel                  = compute_selection_functions.FindKernel("CS_cross_over");
        mutation_and_copy_handel           = compute_selection_functions.FindKernel("CS_mutation_and_copy");


        // -----------------------
        // Compute Shader Bindings

        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel, parent_selection_handel, cross_over_handel, mutation_and_copy_handel }, "_population_pool", population_pool_buffer);

        rendering_material.SetBuffer("_population_pool",         population_pool_buffer);
        fittest_rendering_material.SetBuffer("_population_pool", population_pool_buffer);

        bind_buffers_on_compute(compute_selection_functions, new int[] { cross_over_handel, mutation_and_copy_handel },             "_second_gen_population_pool",                second_gen_population_pool_buffer);
        bind_buffers_on_compute(compute_fitness_function,    new int[] { per_pixel_fitness_kernel_handel, sun_rows_kernel_handel }, "_per_pixel_fitness_buffer",                  per_pixel_fitnes_buffer);
        bind_buffers_on_compute(compute_fitness_function,    new int[] { sun_rows_kernel_handel, sun_column_kernel_handel },        "_rows_sums_array",                           per_row_sum_buffer);
        bind_buffers_on_compute(compute_fitness_function,    new int[] { sun_column_kernel_handel },                                "_population_fitness_array",                  population_pool_fitness_buffer);
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel },                            "_population_fitness_array",                  population_pool_fitness_buffer);
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel, parent_selection_handel },   "_population_accumlative_probablities_array", population_accumlative_prob_buffer);
        bind_buffers_on_compute(compute_selection_functions, new int[] { parent_selection_handel, cross_over_handel},               "_second_gen_parent_ids",                     second_gen_parents_ids_buffer);
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel},                             "_fittest_member",                            fittest_member_buffer);

        fittest_rendering_material.SetBuffer("_fittest_member", fittest_member_buffer);

        compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_original",          ImageToReproduce);
        compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_forged",            compute_forged_in_render_texture);
        if(turn_on_fitness_debug) compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_forged",            user_set_forged);                     // Used for debuging porpuses. Passing on a user given forged to test the fitness function
        //compute_fitness_function.SetTexture   (per_pixel_fitness_kernel_handel, "_debug_texture",     debug_texture);
        //compute_selection_functions.SetTexture(debug_hash_handel,               "_debug_texture",     debug_texture);

        compute_fitness_function.SetInt       ("_image_width",      ImageToReproduce.width);
        compute_fitness_function.SetInt       ("_image_height",     ImageToReproduce.height);
        compute_selection_functions.SetInt    ("_image_width",      ImageToReproduce.width);
        compute_selection_functions.SetInt    ("_image_height",     ImageToReproduce.height);

        compute_selection_functions.SetInt    ("_population_pool_size",    populationPoolNumber);
        compute_selection_functions.SetInt    ("_genes_number_per_member", maximumNumberOfBrushStrokes);

        // -----------------------
        // Population Pool first gen initializatiopn

        Genes[] initialPop = new Genes[total_number_of_genes];

        CPUSystems.InitatePopulationMember(ref initialPop);
        population_pool_buffer.SetData(initialPop);
        rendering_material.SetBuffer("_population_pool", population_pool_buffer);

        // -----------------------
        // Command Buffer Bindings
        //effect_command_buffer.SetRenderTarget(active_texture_target);

        effect_command_buffer = new CommandBuffer
        {
            name = "Effect_Command_Buffer",
        };

        ClearAllRenderTargets(ref effect_command_buffer, true, true, Color.white);

        Debug.Log(string.Format("Dispatch dimensions for compute shaders will be: " +
            "{0}, {1} thread groups and 32 in 32 threads in each group. " +
            "Image should be a multiple of 32 in dimesions", ImageToReproduce.width / 32, ImageToReproduce.height / 32));

        if (ImageToReproduce.width % 32 != 0 || ImageToReproduce.height % 32 != 0)
            Debug.LogError("image is not multiply of 32. Either change the image dimensions or" +
             "The threadnumbers set up in the compute shaders!");


        // ____________________________________________________________________________________________________
        // Command Buffer Recording

        int shader_population_member_begining_index = 0;

        for (int i = 0; i<populationPoolNumber; i++){


            effect_command_buffer.SetGlobalInt("_memember_begin_stride", shader_population_member_begining_index);                             // This is used in the PopulationShader to sample the currect population member (brush stroke) from the population pool list. It is baisicly population_member_index * genes_number_per_population
            shader_population_member_begining_index += maximumNumberOfBrushStrokes;                                                            // add the genes number (stride) in every loop iteration instead of caculating  population_member_index * genes_number_per_population every time

            // -----------------------
            // Draw Population Pool Member
            ClearAllRenderTargets(ref effect_command_buffer, true, true, Color.white);
            effect_command_buffer.DrawProcedural(Matrix4x4.identity, rendering_material, 0, 
                MeshTopology.Triangles, maximumNumberOfBrushStrokes * 6);

            // -----------------------
            //Compute Fitness
            effect_command_buffer.CopyTexture(active_texture_target, compute_forged_in_render_texture);                                         // Without copying the rendering results to a new buffer, I was getting weird results after the rendering of the first population member. Seemed like unity unbinds this buffer since it thinks another operation is writing to it and binds a compeletly different buffer as input (auto generated one). The problem is gone if you copy the buffer 
            effect_command_buffer.SetGlobalInt("_population_id_handel", i);                                                                     // the id is used in compute to know which of the populatioin members are currently being dealt with. 

            // thread groups are made up 32 in 32 threads. The image should be a multiply of 32. 
            // so ideally padded to a power of 2. For other image dimensions, the threadnums
            // should be changed in the compute shader for the kernels as well as here the 
            // height or width divided by 32. Change it only if you know what you are doing 
            effect_command_buffer.DispatchCompute(compute_fitness_function, per_pixel_fitness_kernel_handel,
                ImageToReproduce.width / 32, ImageToReproduce.height / 32, 1);

            // dispatch one compute per row in groups of 64
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
        effect_command_buffer.DispatchCompute(compute_selection_functions, trans_fitness_to_prob_handel, 1, 1, 1);                            // dispatching only a single thread is a waste of a wave front and generally gpu resrources. There  are better reduction algorithmns designed for GPU, have a look at those


        // -----------------------
        // Redraw The Fittest of the Population Members
        ClearAllRenderTargets(ref effect_command_buffer, true, true, Color.white);

        effect_command_buffer.DrawProcedural(Matrix4x4.identity, fittest_rendering_material, 0,
            MeshTopology.Triangles, maximumNumberOfBrushStrokes * 6);
        effect_command_buffer.Blit(active_texture_target, BuiltinRenderTextureType.CameraTarget);

        //effect_command_buffer.DispatchCompute(compute_selection_functions, debug_hash_handel,
        //      ImageToReproduce.width / 8, ImageToReproduce.height / 8, 1);
        //effect_command_buffer.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget);                                                   // used to debug how well the hashing works

        if (populationPoolNumber % 32 != 0)
            Debug.LogError("The population pool number is set to" + populationPoolNumber +
             "Which is not multiple of 32. Either change this number or numThreads in the compute shader!");

        effect_command_buffer.DispatchCompute(compute_selection_functions, parent_selection_handel, populationPoolNumber / 32, 1, 1);


        if (total_number_of_genes % 128 != 0)
            Debug.LogError(string.Format("Total number of genes in the population pool is: {0}, which is not a factor of 128. " +
                "Either change this number to a factor of 128 or change the numThreads in the compute shader"));

        effect_command_buffer.DispatchCompute(compute_selection_functions, cross_over_handel, total_number_of_genes / 128, 1, 1);             // This stage takes the selected parents and combines their genes 

        effect_command_buffer.DispatchCompute(compute_selection_functions, mutation_and_copy_handel, total_number_of_genes / 128, 1, 1);      // This copies the cross overed genes from second gen to the main buffer for rendering in next frame and also mutates some of them

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, effect_command_buffer);

        UpdateBalancingParameters();
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // UPTADE
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void Update()
    {

        UpdateBalancingParameters();

    
        
        // debug_population_member_fitness_value();
        // debug_fitness_to_probabilities_transformation();
        // debug_parent_selection();

        MemberIDFitnessPair[] fittestMember = new MemberIDFitnessPair[1];

        fittest_member_buffer.GetData(fittestMember);

        Debug.Log(string.Format("CurrentGeneration {0}, current fittest member is: {1} with a fittness value of {2}",
            generation_identifier, fittestMember[0].memberID, fittestMember[0].memberFitness));

        if (Input.GetKeyDown(KeyCode.D))
        {
            debug_cross_over();
        }
        
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // DESTRUCTOR
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private void OnDestroy()
    {
        population_pool_buffer.Release();
        second_gen_population_pool_buffer.Release();
        per_pixel_fitnes_buffer.Release();
        per_row_sum_buffer.Release();
        population_pool_fitness_buffer.Release();
        population_accumlative_prob_buffer.Release();
        second_gen_parents_ids_buffer.Release();
        fittest_member_buffer.Release();
    }


    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // HELPER_FUNCTIONS
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    void UpdateBalancingParameters()
    {

        generation_identifier++;
        compute_selection_functions.SetInt("_generation_seed", generation_identifier + Random.Range(0, 2147483647));                                                       // This number is used in the compute shader to differention between rand number geneartion between different generations


        compute_selection_functions.SetFloat("_mutation_rate", mutationChance);
    }


    void bind_buffers_on_compute(ComputeShader computeShader, int[] handels, string identifier, ComputeBuffer data)
    {
        for(int i = 0; i< handels.Length; i++)
        {
            computeShader.SetBuffer(handels[i], identifier, data);
        }

    }



    void ClearAllRenderTargets(ref CommandBuffer cb, bool color, bool depth, Color c)
    {

        //if this is called after a compute. it might cause sync issues with the compute. never understood how the 
        //the barriers and syncing is done in unity. So keep an eye out

        cb.SetRenderTarget(compute_forged_in_render_texture);         // The texture which is used as input for the compute shader also needs to be cleared
        cb.ClearRenderTarget(color, depth, c);               

        cb.SetRenderTarget(active_texture_target);                    // make sure you end up with active_texture_target being the last bound render target
        cb.ClearRenderTarget(color, depth, c);
    }







    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // DEBUG
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------



    struct parentPair
    {
        public int parentX, parentY;
    }


    void debug_cross_over()
    {
        Genes[] second_gen = new Genes[maximumNumberOfBrushStrokes * populationPoolNumber];
        second_gen_population_pool_buffer.GetData(second_gen);

        population_member_to_string(second_gen);
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

        uint[] fittestMember = new uint[1];
        fittest_member_buffer.GetData(fittestMember);
        print(string.Format("The fittest population member is the member {0}", fittestMember[0]));
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

     void population_member_to_string(Genes[] arrayToString)
    {
        for(int i =0; i< arrayToString.Length; i++)
        {
            Debug.Log( "gene [" + i+ "] : "+ GenesToString(arrayToString[i]));
        }
    }

        string GenesToString(Genes g)
    {
        return string.Format("position: ({0}, {1}), rotation: {2}, scale: ({3}, {4}), color: ({5}, {6}, {7}), textureID: {8}",
                              g.position_X, g.position_Y, g.z_Rotation, g.scale_X, g.scale_Y, 
                              g.color_r, g.color_g, g.color_b, g.texture_ID);
    }
}
