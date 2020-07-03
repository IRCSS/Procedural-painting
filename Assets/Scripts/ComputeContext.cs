using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Compute_Shaders
{
    // ____________________________________________________________________________________________________
    // Hard references for the computes
    public ComputeShader compute_fitness_function;                            // holds all code for the fitness function of the genetic evolution algo
    public ComputeShader compute_selection_functions;                         // this file contains the compute kernels for the adjusting the fitness to accmulative weighted probablities, selecting parents, cross over as well as mutation
    public ComputeShader gaussian_compute;
    public ComputeShader sobel_compute;
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

    // ____________________________________________________________________________________________________
    // CONSTRUCTOR: Populate the bindings
    public void Construct_Computes()
    {

        per_pixel_fitness_kernel_handel = compute_fitness_function.FindKernel("CS_Fitness_Per_Pixel");
        sun_rows_kernel_handel          = compute_fitness_function.FindKernel("CS_Sum_Rows");
        sun_column_kernel_handel        = compute_fitness_function.FindKernel("CS_Sum_Column");
        trans_fitness_to_prob_handel    = compute_selection_functions.FindKernel("CS_transform_fitness_to_probability");
        debug_hash_handel               = compute_selection_functions.FindKernel("CS_debug_wang_hash");
        parent_selection_handel         = compute_selection_functions.FindKernel("CS_parent_selection");
        cross_over_handel               = compute_selection_functions.FindKernel("CS_cross_over");
        mutation_and_copy_handel        = compute_selection_functions.FindKernel("CS_mutation_and_copy");
        mutation_and_copy_BW_handel     = compute_selection_functions.FindKernel("CS_mutation_and_copy_BW");
    }

    

    // ____________________________________________________________________________________________________
    // Binding Compute Buffers

    public void bind_population_pool_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel, parent_selection_handel, cross_over_handel, mutation_and_copy_handel, mutation_and_copy_BW_handel }, "_population_pool", buffer);
    }

    public void bind_second_gen_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { cross_over_handel, mutation_and_copy_handel, mutation_and_copy_BW_handel }, "_second_gen_population_pool", buffer);
    }

    public void bind_per_pixel_fitness_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_fitness_function, new int[] { per_pixel_fitness_kernel_handel, sun_rows_kernel_handel }, "_per_pixel_fitness_buffer", buffer);
    }

    public void bind_rows_sum_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_fitness_function, new int[] { sun_rows_kernel_handel, sun_column_kernel_handel }, "_rows_sums_array", buffer);
    }

    public void bind_population_pool_fitness_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_fitness_function,    new int[] { sun_column_kernel_handel },     "_population_fitness_array", buffer);
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel }, "_population_fitness_array", buffer);
    }

    public void bind_population_accumlative_probablities_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel, parent_selection_handel }, "_population_accumlative_probablities_array", buffer);
    }

    public void bind_second_gen_parent_ids_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { parent_selection_handel, cross_over_handel }, "_second_gen_parent_ids", buffer);
    }

    public void bind_fittest_member_buffer(ComputeBuffer buffer)
    {
        bind_buffers_on_compute(compute_selection_functions, new int[] { trans_fitness_to_prob_handel }, "_fittest_member", buffer);
    }

    // ____________________________________________________________________________________________________
    // Bind Textures

    public void bind_original_texture(Texture original)
    {
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_original", original);
    }

    public void bind_forged_texture(Texture forged)
    {
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_forged", forged);
    }

    public void set_image_dimensions(uint width, uint height)
    {
        compute_fitness_function.SetInt       ("_image_width",      (int) width);
        compute_fitness_function.SetInt       ("_image_height",     (int) height);
        compute_selection_functions.SetInt    ("_image_width",      (int) width);
        compute_selection_functions.SetInt    ("_image_height",     (int) height);
    }

    public void set_evolution_settings(uint population_size, uint genes_number_per_population)
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


public struct Compute_Resources                                                                                           // Encapsulates all the buffers and render textures
{

    public ComputeBuffer          population_pool_buffer;                    // Where the population memebers live. The length of this list is population number * number of genes (brush strokes) per population. It is an array of genes where the population member are implied through indexing and strides
    public ComputeBuffer          second_gen_population_pool_buffer;         // This buffer is used to write the members the second generation into. This is then coppied at the last stage in to the buffer above.
    public ComputeBuffer          per_pixel_fitnes_buffer;                   // holds the per pixel info on how close a pixel is to the solution. Reused for each population member
    public ComputeBuffer          per_row_sum_buffer;                        // This buffer is used to sum up all the pixel in a row. It has one entry per row, aka number of pixels in height. Reused for each population member
    public ComputeBuffer          population_pool_fitness_buffer;            // This array contains the fitness of each of the population pool members. One member per population member
    public ComputeBuffer          population_accumlative_prob_buffer;        // This buffer contains the result of transforming the fitness values to an wieghted accmulative probabilities form
    public ComputeBuffer          second_gen_parents_ids_buffer;             // a buffer of pairs of IDs. Each id refers to one of the parents which is used for the cross over algo. Papulated in Computeshader
    public ComputeBuffer          fittest_member_buffer;                     // This buffer contains only one element which is the info of the fittest member of the population pool. It is written to in the compute shader and later in the looop the fittest member is redrawn for visualisation. I am sure there are better ways of doing this without a structured buffer. 


    public RenderTexture          active_texture_target;                     // the population is renedred in this render texture, it is compared per pixel for fitness in compute later
    public RenderTexture          compute_forged_in_render_texture;          // After rendering is done and written to active render texture, the results is coppied here, Without this copy, I was getting some weird issues with bindings and rebinding
    public RenderTexture          debug_texture;                             // texture used to visualize the compute calclulations

    // ____________________________________________________________________________________________________
    // Constructor
    public void Consruct_Buffers(uint image_width, uint image_height, uint population_number, uint genes_number_per_population)
    {
        uint pixel_count_in_image           = image_width * image_height;
        uint total_number_of_genes          = population_number * genes_number_per_population;                                                     // although population_pool is the array of populations, each population memberis implied through the max number of brushes. It is in parsing an array of genes.

        population_pool_buffer             = new ComputeBuffer((int)total_number_of_genes,          sizeof(float) * 8 + sizeof(int) * 1);          // for the stride size look ath the DNA.cs and defination of Genes. 
        second_gen_population_pool_buffer  = new ComputeBuffer((int)total_number_of_genes,          sizeof(float) * 8 + sizeof(int) * 1);          // exact same layout as the one above
        per_pixel_fitnes_buffer            = new ComputeBuffer((int)pixel_count_in_image,           sizeof(float)                      );          // this buffer has one entry per image pixel. So width*height
        per_row_sum_buffer                 = new ComputeBuffer((int)image_height,                   sizeof(float)                      );          // This will have one entry per row of the image. So as many as the height value of the render target. Each of these entries will hold the sum of that row
        population_pool_fitness_buffer     = new ComputeBuffer((int)population_number,              sizeof(float)                      );
        population_accumlative_prob_buffer = new ComputeBuffer((int)population_number,              sizeof(float)                      );          // You could combin this and the fitnes buffer together, I am keeping them seprated for the sake of debuging ease
        second_gen_parents_ids_buffer      = new ComputeBuffer((int)population_number,              sizeof(int)   * 2                  );          // ids (int) paris. Needs to change to smoething else if you have more than 2 parent
        fittest_member_buffer              = new ComputeBuffer(1,                                   sizeof(float) + sizeof(int)        );          // This is a single value. Not sure how to bind it as random read write access without creating an entire buffer


        // -----------------------
        // Textures Initialization
        active_texture_target = new RenderTexture((int)image_width, (int)image_height,
            0, RenderTextureFormat.ARGB32);
        active_texture_target.Create();
        compute_forged_in_render_texture = new RenderTexture(active_texture_target);
        compute_forged_in_render_texture.Create();

        debug_texture = new RenderTexture((int)image_width, (int)image_height,
            0, RenderTextureFormat.ARGB32);
        debug_texture.enableRandomWrite = true;
        debug_texture.Create();

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

        active_texture_target.             Release();
        compute_forged_in_render_texture.  Release();
        debug_texture.                     Release();
    }

}


