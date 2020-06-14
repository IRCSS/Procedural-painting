using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

public class EvolutionManager : MonoBehaviour
{

    [Header("Image")]
    public  Texture                ImageToReproduce;
    public  int                    O_scaleSpaceDepth;
    private RenderTexture[]        O_scales;
            
    [Header("Evelution Settings")]
    public  int                    populationPoolNumber;
    public  int                    maximumNumberOfBrushStrokes;  
    public  Texture                brushTexture;  

    [Header("Soft References")]
    public  ComputeShader          compute_fitness_function;


    private ComputeBuffer[]        population_genes;
    private PopulationMember[]     populations;

    private Material               rendering_material;
                                   
                                   
    private CommandBuffer          effect_command_buffer;
    private RenderTexture          active_texture_target;
    private RenderTexture          per_pixel_fitness_target;

    private Camera                 main_cam;



    // Start is called before the first frame update
    void Start()
    {

        main_cam = Camera.main;
        if (!main_cam) Debug.LogError("Main Camera not found, add a camera to the scene and add the main camera tag to it");

        main_cam.orthographic    = true;
        main_cam.aspect          = (float)ImageToReproduce.width/ (float)ImageToReproduce.height;  
        main_cam.clearFlags      = CameraClearFlags.Color;
        main_cam.backgroundColor = Color.black;

        Screen.fullScreenMode = FullScreenMode.Windowed;

        Screen.SetResolution(ImageToReproduce.width, ImageToReproduce.height, false);
        
        rendering_material = new Material(Shader.Find("Unlit/PopulationShader"));
        if (!rendering_material) Debug.LogError("Couldnt find the population shader");

        rendering_material.SetTexture("_MainTex", brushTexture);


        effect_command_buffer = new CommandBuffer
        {
            name = "Effect_Command_Buffer",
        };

        active_texture_target    = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height, 
            0, RenderTextureFormat.ARGB32);
        per_pixel_fitness_target = new RenderTexture(ImageToReproduce.width, ImageToReproduce.height,
            0, RenderTextureFormat.ARGB32);
        per_pixel_fitness_target.enableRandomWrite = true;
        per_pixel_fitness_target.Create();
        

        population_genes     = new ComputeBuffer[populationPoolNumber]; 
        Genes[] initial_gene = new Genes[maximumNumberOfBrushStrokes];
        populations          = new PopulationMember[populationPoolNumber];

        int per_pixel_fitness_kernel_handel = compute_fitness_function.FindKernel("CS_Fitness_Per_Pixel");

        effect_command_buffer.SetRenderTarget(active_texture_target);
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_original",          ImageToReproduce);
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_forged",            active_texture_target);
        compute_fitness_function.SetTexture(per_pixel_fitness_kernel_handel, "_per_pixel_fitness", per_pixel_fitness_target);
        compute_fitness_function.SetInt    ("_image_width",       ImageToReproduce.width);
        compute_fitness_function.SetInt    ("_image_height",      ImageToReproduce.height);

        Debug.Log(string.Format("Dispatch dimensions for compute shaders will be: {0}, {1} thread groups and 32 in 32 threads in each group. " +
            "Image should be a multiple of 32 in dimesions", ImageToReproduce.width / 32, ImageToReproduce.height / 32));

        if (ImageToReproduce.width % 32 != 0 || ImageToReproduce.height % 32 != 0) Debug.LogError("image is not multiply of 32. Either change the image dimensions or" +
             "The threadnumbers set up in the compute shaders!");

        for(int i = 0; i<populationPoolNumber; i++){
            population_genes[i] = new ComputeBuffer(maximumNumberOfBrushStrokes, sizeof(float) * 8 + sizeof(int) * 1);
            CPUSystems.InitatePopulationMember(ref initial_gene);
            population_genes[i].SetData(initial_gene);


            effect_command_buffer.ClearRenderTarget(true, true, Color.black);
            effect_command_buffer.SetGlobalBuffer("Brushes_Buffer", population_genes[i]);
            effect_command_buffer.DrawProcedural(Matrix4x4.identity, rendering_material, 0, 
                MeshTopology.Triangles, maximumNumberOfBrushStrokes * 6);

            // thread groups are made up 32 in 32 threads. The image should be a multiply of 32. 
            // so ideally padded to a power of 2. For other image dimensions, the threadnums
            // should be changed in the compute shader for the kernels as well as here the 
            // height or width divided by 32. Change it only if you know what you are doing 



            effect_command_buffer.DispatchCompute(compute_fitness_function, per_pixel_fitness_kernel_handel,
                ImageToReproduce.width / 32, ImageToReproduce.height / 32, 1);


        }

            effect_command_buffer.Blit(per_pixel_fitness_target, BuiltinRenderTextureType.CameraTarget);
        

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, effect_command_buffer);


    }


    private void OnDestroy()
    {
        foreach(ComputeBuffer cb in population_genes)
        {
            cb.Release();
        }
    }

        string GenesToString(Genes g)
    {
        return string.Format("position: ({0}, {1}), rotation: {2}, scale: ({3}, {4}), color: ({5}, {6}, {7}), textureID: {8}",
                              g.position_X, g.position_Y, g.z_Rotation, g.scale_X, g.scale_Y, 
                              g.color_r, g.color_g, g.color_b, g.texture_ID);
    }
}
