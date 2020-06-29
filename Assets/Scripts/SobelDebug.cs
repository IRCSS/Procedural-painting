using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SobelDebug : MonoBehaviour
{

    public ComputeShader sobel_compute;
    public Texture       source_texture;


    
    private RenderTexture target_render_texture;
    private Camera        main_camera;
    private CommandBuffer cb;

    // Start is called before the first frame update
    void Start()
    {

        main_camera = Camera.main;

        target_render_texture = new RenderTexture(source_texture.width, source_texture.height, 0)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp,
        };
        target_render_texture.Create();

        int sobel_handel = sobel_compute.FindKernel("Sobel");

        sobel_compute.SetTexture(sobel_handel, "_source", source_texture);
        sobel_compute.SetTexture(sobel_handel, "_result", target_render_texture);

        sobel_compute.SetInt("_source_width",  source_texture.width);
        sobel_compute.SetInt("_source_height", source_texture.height);

        cb = new CommandBuffer()
        {
            name = "Sobel_Test_Pass"
        };
        cb.DispatchCompute(sobel_compute, sobel_handel, source_texture.width / 32, source_texture.height / 32, 1);

        cb.Blit(target_render_texture, BuiltinRenderTextureType.CameraTarget);

        main_camera.AddCommandBuffer(CameraEvent.AfterEverything, cb);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
