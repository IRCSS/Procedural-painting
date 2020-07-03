using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;




public class EvolutionManager : MonoBehaviour
{

    [Header("Image")]
    public  Texture                ImageToReproduce;                          // This image is used for the evolution algo and is the ground truth
          
    
    [Header("Scale Settings")]
    public  ScaleStage[]           stages;

    [Header("Evelution Settings")]
    public  uint                   populationPoolNumber;                      // larger population pool could lead to reducing number of generations required to get to the answer however increases the memory overhead
    public  uint                   maximumNumberOfBrushStrokes;               // this controls how many brush strokes can be used to replicate the image aka how many genes a population has
    public  Texture                brushTexture;                              // four textures in one texture. R, G, B and A each hold a texture of its own

    public  bool                   blackAnwWhite;
    public  float                  mutationChance = 0.01f;
    public  float                  brushSizeLowerBound  = 0.1f;
    public  float                  brushSizeHigherBound = 1.0f;

    public  float                  hueWeight;                                 // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float                  satWeight;                                 // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float                  valWeight;                                 // These three values need to collectivly add up to ONE. used in the fitness function to determine what of the HSV is more important to match
    public  float                  fitnessPowFactor;
    [Header("Soft References")]
    public   Compute_Shaders        compute_shaders;                          // All the boiler plate code, binding and references for the compute shaders. Look in the comments in the struct for more info 

    [Header("Debug")]
    public Texture                 user_set_forged;                           // I used this to test if my fitness function works. In this texture I can insert a copy of the original which is slightly altered and see what value my fitness function gives me for that image
    public bool                    turn_on_fitness_debug;
    
    private Material               rendering_material;                        // material used to actually render the brush strokes
    private Material               fittest_rendering_material;                // this material is used to draw the fittest member of the population at the end of the lop after all calculations are done
                                   
    private CommandBuffer          effect_command_buffer;                     // this command buffer encapsulates everything that happens in the effect
   
    private Compute_Resources      compute_resources;                         // holds all the buffers and rendertargets. Look in the defination of the struct for more info and documentation

    private Camera                 main_cam;

    private int generation_identifier = 0;                                    // This number specifies how many generations have already gone by. 

    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // START
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    void Start()
    {


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

        fittest_rendering_material.SetInt("_genes_number_per_member", (int) maximumNumberOfBrushStrokes);
        fittest_rendering_material.SetTexture("_MainTex", brushTexture);

        // ____________________________________________________________________________________________________
        // Compute Resources Initialization

        compute_resources.Consruct_Buffers((uint) ImageToReproduce.width, (uint)ImageToReproduce.height, populationPoolNumber, maximumNumberOfBrushStrokes);

        // ____________________________________________________________________________________________________
        // Command Buffer initialization

        compute_shaders.Construct_Computes();


        // -----------------------
        // Compute Shader Bindings




        rendering_material.SetBuffer         ("_population_pool",         compute_resources.population_pool_buffer);
        fittest_rendering_material.SetBuffer ("_population_pool",         compute_resources.population_pool_buffer);
        fittest_rendering_material.SetBuffer ("_fittest_member",          compute_resources.fittest_member_buffer);

        compute_shaders.bind_population_pool_buffer                     (compute_resources.population_pool_buffer);
        compute_shaders.bind_second_gen_buffer                          (compute_resources.second_gen_population_pool_buffer);
        compute_shaders.bind_per_pixel_fitness_buffer                   (compute_resources.per_pixel_fitnes_buffer);
        compute_shaders.bind_rows_sum_buffer                            (compute_resources.per_row_sum_buffer);
        compute_shaders.bind_population_pool_fitness_buffer             (compute_resources.population_pool_fitness_buffer);
        compute_shaders.bind_population_accumlative_probablities_buffer (compute_resources.population_accumlative_prob_buffer);
        compute_shaders.bind_second_gen_parent_ids_buffer               (compute_resources.second_gen_parents_ids_buffer);
        compute_shaders.bind_fittest_member_buffer                      (compute_resources.fittest_member_buffer);

        compute_shaders.bind_original_texture(ImageToReproduce);
        compute_shaders.bind_forged_texture  (compute_resources.compute_forged_in_render_texture);

        if(turn_on_fitness_debug) compute_shaders.bind_forged_texture(user_set_forged);                                                        // Used for debuging porpuses. Passing on a user given forged to test the fitness function

        compute_shaders.set_image_dimensions((uint) ImageToReproduce.width, (uint) ImageToReproduce.height);
        compute_shaders.set_evolution_settings(populationPoolNumber, maximumNumberOfBrushStrokes);


        // ____________________________________________________________________________________________________
        // CPU Arrays initalization


        // -----------------------
        // Population Pool first gen initializatiopn
        int total_number_of_genes = (int)(populationPoolNumber * maximumNumberOfBrushStrokes);
        Genes[] initialPop = new Genes[total_number_of_genes];

        if(!blackAnwWhite)
        CPUSystems.InitatePopulationMember(ref initialPop, brushSizeLowerBound, brushSizeHigherBound);
        else
        CPUSystems.InitatePopulationMemberBW(ref initialPop, brushSizeLowerBound, brushSizeHigherBound);

        compute_resources.population_pool_buffer.SetData(initialPop);
        rendering_material.SetBuffer("_population_pool", compute_resources.population_pool_buffer);

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

        uint shader_population_member_begining_index = 0;

        for (int i = 0; i<populationPoolNumber; i++){


            effect_command_buffer.SetGlobalInt("_memember_begin_stride", (int)shader_population_member_begining_index);                              // This is used in the PopulationShader to sample the currect population member (brush stroke) from the population pool list. It is baisicly population_member_index * genes_number_per_population
            shader_population_member_begining_index += maximumNumberOfBrushStrokes;                                                                  // add the genes number (stride) in every loop iteration instead of caculating  population_member_index * genes_number_per_population every time

            // -----------------------
            // Draw Population Pool Member
            ClearAllRenderTargets(ref effect_command_buffer, true, true, Color.white);
            effect_command_buffer.DrawProcedural(Matrix4x4.identity, rendering_material, 0, 
                MeshTopology.Triangles, (int)maximumNumberOfBrushStrokes * 6);

            // -----------------------
            //Compute Fitness
            effect_command_buffer.CopyTexture(compute_resources.active_texture_target, compute_resources.compute_forged_in_render_texture);           // Without copying the rendering results to a new buffer, I was getting weird results after the rendering of the first population member. Seemed like unity unbinds this buffer since it thinks another operation is writing to it and binds a compeletly different buffer as input (auto generated one). The problem is gone if you copy the buffer 
            effect_command_buffer.SetGlobalInt("_population_id_handel", i);                                                                           // the id is used in compute to know which of the populatioin members are currently being dealt with. 

            // thread groups are made up 32 in 32 threads. The image should be a multiply of 32. 
            // so ideally padded to a power of 2. For other image dimensions, the threadnums
            // should be changed in the compute shader for the kernels as well as here the 
            // height or width divided by 32. Change it only if you know what you are doing 
            effect_command_buffer.DispatchCompute(compute_shaders.compute_fitness_function, compute_shaders.per_pixel_fitness_kernel_handel,
                ImageToReproduce.width / 32, ImageToReproduce.height / 32, 1);

            // dispatch one compute per row in groups of 64
            effect_command_buffer.DispatchCompute(compute_shaders.compute_fitness_function, compute_shaders.sun_rows_kernel_handel,
                ImageToReproduce.height / 32, 1, 1);

            //// dispatch a single thread
            effect_command_buffer.DispatchCompute(compute_shaders.compute_fitness_function, compute_shaders.sun_column_kernel_handel,
                1, 1, 1);


        }

        //effect_command_buffer.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget);                                                            // Used for debuging the output of the per pixel comute calculations

        // -----------------------
        // Convert Fitness to accumlative weighted probablities

        // Dispatch single thread. 
        effect_command_buffer.DispatchCompute(compute_shaders.compute_selection_functions, compute_shaders.trans_fitness_to_prob_handel, 1, 1, 1);     // dispatching only a single thread is a waste of a wave front and generally gpu resrources. There  are better reduction algorithmns designed for GPU, have a look at those


        // -----------------------
        // Redraw The Fittest of the Population Members
        ClearAllRenderTargets(ref effect_command_buffer, true, true, Color.white);

        effect_command_buffer.DrawProcedural(Matrix4x4.identity, fittest_rendering_material, 0,
            MeshTopology.Triangles, (int) maximumNumberOfBrushStrokes * 6);
        effect_command_buffer.Blit(compute_resources.active_texture_target, BuiltinRenderTextureType.CameraTarget);

        //effect_command_buffer.DispatchCompute(compute_selection_functions, debug_hash_handel,
        //      ImageToReproduce.width / 8, ImageToReproduce.height / 8, 1);
        //effect_command_buffer.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget);                                                   // used to debug how well the hashing works

        if (populationPoolNumber % 16 != 0)
            Debug.LogError("The population pool number is set to" + populationPoolNumber +
             "Which is not multiple of 32. Either change this number or numThreads in the compute shader!");

        effect_command_buffer.DispatchCompute(compute_shaders.compute_selection_functions, compute_shaders.parent_selection_handel, (int)populationPoolNumber / 16, 1, 1);


        if (total_number_of_genes % 128 != 0)
            Debug.LogError(string.Format("Total number of genes in the population pool is: {0}, which is not a factor of 128. " +
                "Either change this number to a factor of 128 or change the numThreads in the compute shader"));

        effect_command_buffer.DispatchCompute(compute_shaders.compute_selection_functions, compute_shaders.cross_over_handel, total_number_of_genes / 128, 1, 1);             // This stage takes the selected parents and combines their genes 

        if(!blackAnwWhite)
            effect_command_buffer.DispatchCompute(compute_shaders.compute_selection_functions, compute_shaders.mutation_and_copy_handel, total_number_of_genes / 128, 1, 1);      // This copies the cross overed genes from second gen to the main buffer for rendering in next frame and also mutates some of them
        else
            effect_command_buffer.DispatchCompute(compute_shaders.compute_selection_functions, compute_shaders.mutation_and_copy_BW_handel, total_number_of_genes / 128, 1, 1);

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

        compute_resources.fittest_member_buffer.GetData(fittestMember);

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
        compute_resources.Destruct_Buffers();
    }


    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // HELPER_FUNCTIONS
    // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

    void UpdateBalancingParameters()
    {

        generation_identifier++;
        compute_shaders.compute_selection_functions.SetInt  ("_generation_seed",     generation_identifier + Random.Range(0, 2147483647));                                                       // This number is used in the compute shader to differention between rand number geneartion between different generations
        compute_shaders.compute_selection_functions.SetFloat("_scale_lower_bound",   brushSizeLowerBound);
        compute_shaders.compute_selection_functions.SetFloat("_scale_higher_bound",  brushSizeHigherBound);
        compute_shaders.compute_selection_functions.SetFloat("_mutation_rate",       mutationChance);
        compute_shaders.compute_selection_functions.SetFloat("_fittness_pow_factor", fitnessPowFactor);



        compute_shaders.compute_fitness_function.SetFloat("_hue_weight", hueWeight);
        compute_shaders.compute_fitness_function.SetFloat("_sat_weight", satWeight);
        compute_shaders.compute_fitness_function.SetFloat("_val_weight", valWeight);


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

        cb.SetRenderTarget(compute_resources.compute_forged_in_render_texture);         // The texture which is used as input for the compute shader also needs to be cleared
        cb.ClearRenderTarget(color, depth, c);               

        cb.SetRenderTarget(compute_resources.active_texture_target);                    // make sure you end up with active_texture_target being the last bound render target
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
        compute_resources.second_gen_population_pool_buffer.GetData(second_gen);

        population_member_to_string(second_gen);
    }

    void debug_parent_selection()
    {
        parentPair[] second_gen_parents_ids = new parentPair[populationPoolNumber];
        compute_resources.second_gen_parents_ids_buffer.GetData(second_gen_parents_ids);
        for (int i = 0; i < populationPoolNumber; i++)
        {
            print(string.Format("second generation {0}, has the parents {1} and {2}", 
                i, second_gen_parents_ids[i].parentX, second_gen_parents_ids[i].parentY));
        }
    }


    void debug_fitness_to_probabilities_transformation()
    {
        float[] accmulative_probabilities = new float[populationPoolNumber];
        compute_resources.population_accumlative_prob_buffer.GetData(accmulative_probabilities);
        for (int i = 0; i < populationPoolNumber; i++)
        {
            print(string.Format("population {0}, has the accumalative weighted probablity {1}", i, accmulative_probabilities[i]));
        }

        uint[] fittestMember = new uint[1];
        compute_resources.fittest_member_buffer.GetData(fittestMember);
        print(string.Format("The fittest population member is the member {0}", fittestMember[0]));
    }

    /// <summary>
    /// Pulls the fitness value data from the GPU and prints them out per member
    /// </summary>
    void debug_population_member_fitness_value()
    {
        float[] population_fitness_cpu_values = new float[populationPoolNumber];
        compute_resources.population_pool_fitness_buffer.GetData(population_fitness_cpu_values);

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
