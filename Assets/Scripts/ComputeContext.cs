using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// I have all the stuff here as classes to avoid them bein passed on by value in functions, and always passed on by reference. Structs would trigger a coppy, which is not a big deal 
// since they are only holding ids, and unity takes care of the GPU resources so that no destruction/ construction are called, but it never hurts to be tripple sure

[System.Serializable]
public class Compute_Shaders
{
    // ____________________________________________________________________________________________________
    // Hard references for the computes
    public ComputeShader compute_fitness_function;                            // holds all code for the fitness function of the genetic evolution algo
    public ComputeShader compute_selection_functions;                         // this file contains the compute kernels for the adjusting the fitness to accmulative weighted probablities, selecting parents, cross over as well as mutation
    public ComputeShader gaussian_compute;
    public ComputeShader sobel_compute_original;
    public ComputeShader construct_position_domain_compute;

    [HideInInspector]
    public ComputeShader sobel_compute_forged;
    // ____________________________________________________________________________________________________
    // handel ides for each kernel


    [HideInInspector]
    public int per_pixel_fitness_kernel_handel;                              // Handels used to dispatch compute. This function calculatis fitness on level of pixel by comparing it to orginal
    [HideInInspector]
    public int sun_rows_kernel_handel;                                       // Handels used to dispatch compute. Sums up each pixel of a row to a single value. The result is an array of floats
    [HideInInspector]
    public int sun_column_kernel_handel;                                     // Handels used to dispatch compute. Sums up the sums of rows that are saved in a single column to one float.
    [HideInInspector]
    public int trans_fitness_to_prob_handel;                                 // Handel used to dispatch compute.  this is used to convert the fitness values which are already normalized to an accumaletive weighted probabilities for sampling 
    [HideInInspector]
    public int debug_hash_handel;                                            // Used for debuging how well the hash creation function is working
    [HideInInspector]
    public int parent_selection_handel;                                      // used for selecting a pair of parents for each second geneariton of population members  
    [HideInInspector]
    public int cross_over_handel;                                            // This compute shader breeds the second generation based on the parents and creates a second generation per population pool
    [HideInInspector]
    public int mutation_and_copy_handel;                                     // This copies over the second generation members to the main buffer to be rendered in the next frame and mutates some of the genes along the way
    [HideInInspector]
    public int mutation_and_copy_BW_handel;                                  // This copies over the second generation members to the main buffer to be rendered in the next frame and mutates some of the genes along the way. The mutates genes are colorless and only have value
    [HideInInspector]
    public int sobel_handel_original;                                        // This applies a sobel filter on an image. Which creates a per pixel gradient image
    [HideInInspector]
    public int sobel_handel_forged;                                          // same as above but for the forged image. This is another instance of the same compute shader
    [HideInInspector]
    public int gaussian_horizontal_handel;
    [HideInInspector]
    public int gaussian_vertical_handel;
    [HideInInspector]
    public int Construct_Position_Domain_handel;
    [HideInInspector]
    public int Debug_Position_Domain_to_Texture_handel;
    [HideInInspector]
    public int populate_population;
    [HideInInspector]
    public int populate_population_BW;

    // ____________________________________________________________________________________________________
    // CONSTRUCTOR: Populate the bindings
    public void Construct_Computes()
    {
        sobel_compute_forged = Object.Instantiate(sobel_compute_original);                                  // We are going to need two instances of this compute shader so that we can staticly bind textures to it without the need of rebinding during the frame updating

        per_pixel_fitness_kernel_handel         = compute_fitness_function.         FindKernel("CS_Fitness_Per_Pixel");
        sun_rows_kernel_handel                  = compute_fitness_function.         FindKernel("CS_Sum_Rows");
        sun_column_kernel_handel                = compute_fitness_function.         FindKernel("CS_Sum_Column");
        trans_fitness_to_prob_handel            = compute_selection_functions.      FindKernel("CS_transform_fitness_to_probability");
        debug_hash_handel                       = compute_selection_functions.      FindKernel("CS_debug_wang_hash");
        parent_selection_handel                 = compute_selection_functions.      FindKernel("CS_parent_selection");
        cross_over_handel                       = compute_selection_functions.      FindKernel("CS_cross_over");
        mutation_and_copy_handel                = compute_selection_functions.      FindKernel("CS_mutation_and_copy");
        mutation_and_copy_BW_handel             = compute_selection_functions.      FindKernel("CS_mutation_and_copy_BW");
        populate_population                     = compute_selection_functions.      FindKernel("CS_populate_population");
        populate_population_BW                  = compute_selection_functions.      FindKernel("CS_populate_population_BW");
        sobel_handel_original                   = sobel_compute_original.           FindKernel("Sobel");
        sobel_handel_forged                     = sobel_compute_forged.             FindKernel("Sobel");
        gaussian_vertical_handel                = gaussian_compute.                 FindKernel("CS_gaussian_vertical");
        gaussian_horizontal_handel              = gaussian_compute.                 FindKernel("CS_gaussian_horizontal");
        Construct_Position_Domain_handel        = construct_position_domain_compute.FindKernel("CS_Construct_Position_Domain");
        Debug_Position_Domain_to_Texture_handel = construct_position_domain_compute.FindKernel("CS_Debug_Position_Domain_to_Texture");

    }

    
    public void Bind_Compute_Resources(Texture target_image, Compute_Resources compute_resources, Evolution_Settings evolution_settings)
    {
        bind_population_pool_buffer                     (compute_resources.population_pool_buffer);
        bind_second_gen_buffer                          (compute_resources.second_gen_population_pool_buffer);
        bind_per_pixel_fitness_buffer                   (compute_resources.per_pixel_fitnes_buffer);
        bind_rows_sum_buffer                            (compute_resources.per_row_sum_buffer);
        bind_population_pool_fitness_buffer             (compute_resources.population_pool_fitness_buffer);
        bind_population_accumlative_probablities_buffer (compute_resources.population_accumlative_prob_buffer);
        bind_second_gen_parent_ids_buffer               (compute_resources.second_gen_parents_ids_buffer);
        bind_fittest_member_buffer                      (compute_resources.fittest_member_buffer);
        bind_position_domain_buffer                     (compute_resources.position_domain_buffer);
        bind_positon_domain_arguments_buffer            (compute_resources.positon_domain_arguments_buffer);

        bind_original_texture         (target_image);
        bind_forged_texture           (compute_resources.compute_forged_in_render_texture);
        bind_orignal_gradient_texture (compute_resources.original_image_gradient);
        bind_forged_gradient_texture  (compute_resources.forged_image_gradient);
        bind_sobel_out                (compute_resources.sobel_out);
        bind_original_blured          (compute_resources.original_image_blured);
        bind_gaussian_out             (compute_resources.gaussian_out);
        bind_debug_texture            (compute_resources.debug_texture);

        set_image_dimensions((uint)target_image.width, (uint)target_image.height);
        set_evolution_settings(evolution_settings.populationPoolNumber, evolution_settings.maximumNumberOfBrushStrokes);
    }

    // ____________________________________________________________________________________________________
    // Binding Compute Buffers

    private void bind_position_domain_buffer(ComputeBuffer position_domain)
    {
        bind_buffers_on_compute(construct_position_domain_compute, new int[] { Construct_Position_Domain_handel },                                                                    "_position_domain_buffer", position_domain);
        bind_buffers_on_compute(compute_selection_functions,       new int[] { mutation_and_copy_handel, mutation_and_copy_BW_handel, populate_population , populate_population_BW }, "_position_domain_buffer", position_domain);
    }

    private void bind_positon_domain_arguments_buffer(ComputeBuffer position_domain_arguments)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { mutation_and_copy_handel, mutation_and_copy_BW_handel, populate_population, populate_population_BW }, "_position_domain_argument_buffer", position_domain_arguments);
    }

    private void bind_population_pool_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel, parent_selection_handel, cross_over_handel,
                                                                         mutation_and_copy_handel, mutation_and_copy_BW_handel, populate_population, populate_population_BW }, "_population_pool", buffer);
    }

    private void bind_second_gen_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { cross_over_handel, mutation_and_copy_handel, mutation_and_copy_BW_handel }, "_second_gen_population_pool", buffer);
    }

    private void bind_per_pixel_fitness_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_fitness_function,    new int[] { per_pixel_fitness_kernel_handel, sun_rows_kernel_handel }, "_per_pixel_fitness_buffer", buffer);
    }

    private void bind_rows_sum_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_fitness_function,    new int[] { sun_rows_kernel_handel, sun_column_kernel_handel }, "_rows_sums_array", buffer);
    }

    private void bind_population_pool_fitness_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_fitness_function,    new int[] { sun_column_kernel_handel },     "_population_fitness_array", buffer);
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel }, "_population_fitness_array", buffer);
    }

    private void bind_population_accumlative_probablities_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel, parent_selection_handel }, "_population_accumlative_probablities_array", buffer);
    }

    private void bind_second_gen_parent_ids_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { parent_selection_handel, cross_over_handel }, "_second_gen_parent_ids", buffer);
    }

    private void bind_fittest_member_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel }, "_fittest_member", buffer);
    }


    

    // ____________________________________________________________________________________________________
    // Bind Textures

    private void bind_original_blured(Texture original_blured)
    {
        gaussian_compute.SetTexture      (gaussian_vertical_handel, "_vertical_source", original_blured); // the result of the horizontal pass is copied over to the original_blur texture
        sobel_compute_original.SetTexture(sobel_handel_original,    "_source",          original_blured);
    }

    private void bind_gaussian_out(Texture gaussian_out)
    {
        gaussian_compute.SetTexture(gaussian_horizontal_handel, "_horizontal_results", gaussian_out);
        gaussian_compute.SetTexture(gaussian_vertical_handel,   "_vertical_results",   gaussian_out);
    }


    private void bind_original_texture(Texture original)
    {
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_original",          original);
        gaussian_compute.SetTexture        (gaussian_horizontal_handel,      "_horizontal_source", original);

    }
    private void bind_sobel_out(Texture sobel_results)
    {
        sobel_compute_original.SetTexture  (sobel_handel_original,           "_result",   sobel_results);
        sobel_compute_forged.SetTexture    (sobel_handel_forged,             "_result",   sobel_results);
    }

    private void bind_orignal_gradient_texture(Texture original_gradient)
    {
        compute_fitness_function.SetTexture         (per_pixel_fitness_kernel_handel,         "_original_gradient", original_gradient);
        construct_position_domain_compute.SetTexture(Construct_Position_Domain_handel,        "_mask",              original_gradient);
        construct_position_domain_compute.SetTexture(Debug_Position_Domain_to_Texture_handel, "_mask",              original_gradient);
    }
    private void bind_forged_gradient_texture(Texture forged_gradient)
    {
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_forged_gradient", forged_gradient);
    }

    private void bind_forged_texture(Texture forged)
    {
        sobel_compute_forged.SetTexture    (sobel_handel_forged,             "_source",   forged);
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_forged",   forged);
    }

    private void bind_debug_texture(Texture debug_texture)
    {
        construct_position_domain_compute.SetTexture(Debug_Position_Domain_to_Texture_handel, "_position_domain_visualiser", debug_texture);
    }


    private void set_image_dimensions(uint width, uint height)
    {
        compute_fitness_function.   SetInt      ("_image_width",      (int) width) ;
        compute_fitness_function.   SetInt      ("_image_height",     (int) height);
        compute_selection_functions.SetInt      ("_image_width",      (int) width) ;
        compute_selection_functions.SetInt      ("_image_height",     (int) height);
        sobel_compute_original.     SetInt      ("_source_width",     (int) width) ;
        sobel_compute_original.     SetInt      ("_source_height",    (int) height);
        sobel_compute_forged.       SetInt      ("_source_width",     (int) width) ;
        sobel_compute_forged.       SetInt      ("_source_height",    (int) height);
        gaussian_compute.           SetInt      ("_source_width",     (int) width) ;
        gaussian_compute.           SetInt      ("_source_height",    (int) height);
        construct_position_domain_compute.SetInt("_image_width",      (int) width) ;
        construct_position_domain_compute.SetInt("_image_height",     (int) height);
    }

    private void set_evolution_settings(uint population_size, uint genes_number_per_population)
    {
        compute_selection_functions.SetInt    ("_population_pool_size",    (int) population_size);
        compute_selection_functions.SetInt    ("_genes_number_per_member", (int) genes_number_per_population);
    }

    // ____________________________________________________________________________________________________
    // Helper Methods

    // Used to bind a buffer on several kernels. This is to hid a bit of the boiler plate code
    static void bind_buffers_on_compute(ComputeShader computeShader, int[] handels, string identifier, ComputeBuffer data)
    {
        for (int i = 0; i < handels.Length; i++)
        {
            computeShader.SetBuffer(handels[i], identifier, data);
        }

    }

}

// ----------------------------------------------------------------------------------------------------------------------------------------------------------------
// Resources : Structured Buffer and Render Textures
// ----------------------------------------------------------------------------------------------------------------------------------------------------------------


public class Compute_Resources                                               // Encapsulates all the buffers and render textures
{

    public ComputeBuffer          population_pool_buffer;                    // Where the population memebers live. The length of this list is population number * number of genes (brush strokes) per population. It is an array of genes where the population member are implied through indexing and strides
    public ComputeBuffer          second_gen_population_pool_buffer;         // This buffer is used to write the members the second generation into. This is then coppied at the last stage in to the buffer above.
    public ComputeBuffer          per_pixel_fitnes_buffer;                   // holds the per pixel info on how close a pixel is to the solution. Reused for each population member
    public ComputeBuffer          per_row_sum_buffer;                        // This buffer is used to sum up all the pixel in a row. It has one entry per row, aka number of pixels in height. Reused for each population member
    public ComputeBuffer          population_pool_fitness_buffer;            // This array contains the fitness of each of the population pool members. One member per population member
    public ComputeBuffer          population_accumlative_prob_buffer;        // This buffer contains the result of transforming the fitness values to an wieghted accmulative probabilities form
    public ComputeBuffer          second_gen_parents_ids_buffer;             // a buffer of pairs of IDs. Each id refers to one of the parents which is used for the cross over algo. Papulated in Computeshader
    public ComputeBuffer          fittest_member_buffer;                     // This buffer contains only one element which is the info of the fittest member of the population pool. It is written to in the compute shader and later in the looop the fittest member is redrawn for visualisation. I am sure there are better ways of doing this without a structured buffer. 
    public ComputeBuffer          position_domain_buffer;
    public ComputeBuffer          positon_domain_arguments_buffer;


    public RenderTexture          active_texture_target;                     // the population is renedred in this render texture, it is compared per pixel for fitness in compute later
    public RenderTexture          compute_forged_in_render_texture;          // After rendering is done and written to active render texture, the results is coppied here, Without this copy, I was getting some weird issues with bindings and rebinding
    public RenderTexture          debug_texture;                             // texture used to visualize the compute calclulations. It is not always bound, you need to write code to bind it. I am just leaving it here so that I dont need to create a new texture every time for debuging
    public RenderTexture          clear_base;                                // This is the image used to clear the render target. After the first stage, this would be the basis which the second/ thirds etc stages would paint on
    public RenderTexture          original_image_blured;
    public RenderTexture          original_image_gradient;                   // This texture contains a version of the original image that has the sobel filter applied to it. This filter creates an image gradient which is used for calculating fitness of a pixel
    public RenderTexture          forged_image_gradient;                     // same as above but for the forged attmpted by the AI
    public RenderTexture          sobel_out;                                 // The sobel shader first writes its results to this. The reason why it doesnt directly write to the input of the fitness function is that this texture needs random access, which is not compatible with samplers.
    public RenderTexture          gaussian_out;
    // ____________________________________________________________________________________________________
    // Constructor
    public void Consruct_Buffers(Texture Image_to_reproduce, RenderTexture stage_base, Evolution_Settings evolution_setting)
    {
        int  pixel_count_in_image           = Image_to_reproduce.width * Image_to_reproduce.height;
        uint total_number_of_genes          = evolution_setting.populationPoolNumber * evolution_setting.maximumNumberOfBrushStrokes;                      // although population_pool is the array of populations, each population memberis implied through the max number of brushes. It is in parsing an array of genes.

        population_pool_buffer             = new ComputeBuffer((int)total_number_of_genes,                  sizeof(float) * 8 + sizeof(int) * 1);          // for the stride size look ath the DNA.cs and defination of Genes. 
        second_gen_population_pool_buffer  = new ComputeBuffer((int)total_number_of_genes,                  sizeof(float) * 8 + sizeof(int) * 1);          // exact same layout as the one above
        per_pixel_fitnes_buffer            = new ComputeBuffer((int)pixel_count_in_image,                   sizeof(float) * 2                  );          // this buffer has one entry per image pixel. So width*height
        per_row_sum_buffer                 = new ComputeBuffer((int)Image_to_reproduce.height,              sizeof(float) * 2                  );          // This will have one entry per row of the image. So as many as the height value of the render target. Each of these entries will hold the sum of that row
        population_pool_fitness_buffer     = new ComputeBuffer((int)evolution_setting.populationPoolNumber, sizeof(float) * 2                  );
        population_accumlative_prob_buffer = new ComputeBuffer((int)evolution_setting.populationPoolNumber, sizeof(float)                      );          // You could combin this and the fitnes buffer together, I am keeping them seprated for the sake of debuging ease
        second_gen_parents_ids_buffer      = new ComputeBuffer((int)evolution_setting.populationPoolNumber, sizeof(int)   * 2                  );          // ids (int) paris. Needs to change to smoething else if you have more than 2 parent
        fittest_member_buffer              = new ComputeBuffer(1,                                           sizeof(float) + sizeof(int)        );          // This is a single value. Not sure how to bind it as random read write access without creating an entire buffer
        position_domain_buffer             = new ComputeBuffer((int)pixel_count_in_image,                   sizeof(float) * 2, ComputeBufferType.Append);
        position_domain_buffer.SetCounterValue(0);
        positon_domain_arguments_buffer    = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        int[] ini_value = new int[4] { 0, 0, 0, 0 };
        positon_domain_arguments_buffer.SetData(ini_value);

        // -----------------------
        // Textures Initialization
        active_texture_target = new RenderTexture(Image_to_reproduce.width, Image_to_reproduce.height,
            0, RenderTextureFormat.ARGB32)
        {
            wrapMode = TextureWrapMode.Clamp,
        };
        active_texture_target.Create();
        compute_forged_in_render_texture = new RenderTexture(active_texture_target)
        {
            wrapMode = TextureWrapMode.Clamp,
        };
        compute_forged_in_render_texture.Create();

        debug_texture = new RenderTexture(Image_to_reproduce.width, Image_to_reproduce.height,
            0, RenderTextureFormat.ARGB32)
        {
            wrapMode          = TextureWrapMode.Clamp,
            enableRandomWrite = true,
        };
        
        debug_texture.Create();
        
        clear_base = new RenderTexture(stage_base);
        clear_base.Create();
        Graphics.Blit(stage_base, clear_base);

        original_image_gradient = new RenderTexture(active_texture_target)
        {
            wrapMode          = TextureWrapMode.Clamp,
        };
        original_image_gradient.Create();

        original_image_blured = new RenderTexture(active_texture_target)
        {
            wrapMode = TextureWrapMode.Clamp,
        };

        original_image_blured.Create();

        forged_image_gradient = new RenderTexture(active_texture_target)
        {
            wrapMode          = TextureWrapMode.Clamp,
        };
        forged_image_gradient.Create();

        sobel_out = new RenderTexture(active_texture_target)
        {
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true,                   // This image is written to in compute shader as a random accesed resources
        };

        sobel_out.Create();

        gaussian_out = new RenderTexture(active_texture_target)
        {
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true,                   // This image is written to in compute shader as a random accesed resources
        };

        gaussian_out.Create();


    }


    // ____________________________________________________________________________________________________
    // Destructor
    public void Destruct_Buffers()
    {
        // Reducing the refernce counts of the COM objects when we are done from the application side
        population_pool_buffer.            Release();
        second_gen_population_pool_buffer. Release();
        per_pixel_fitnes_buffer.           Release();
        per_row_sum_buffer.                Release();
        population_pool_fitness_buffer.    Release();
        population_accumlative_prob_buffer.Release();
        second_gen_parents_ids_buffer.     Release();
        fittest_member_buffer.             Release();
        position_domain_buffer.            Release();
        positon_domain_arguments_buffer.   Release();

        active_texture_target.             Release();
        compute_forged_in_render_texture.  Release();
        debug_texture.                     Release();
        clear_base.                        Release();

        original_image_blured.             Release();
        original_image_gradient.           Release();
        forged_image_gradient.             Release();
        sobel_out.                         Release();
        gaussian_out.                      Release();

    }

}


