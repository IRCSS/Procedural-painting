using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GaussianDebug : MonoBehaviour
{

    public  Texture       Source;
    public  ComputeShader gaussian_compute;

    public  int           kernelSize = 3;

    public  float         sigma      = 4;

    private RenderTexture horizontal_target;
    private RenderTexture vertical_target;
    private CommandBuffer cb;

    private Camera        main_cam;


    // Start is called before the first frame update
    void Start()
    {
        main_cam = Camera.main;

        horizontal_target = new RenderTexture(Source.width, Source.height, 0)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp
        };
        horizontal_target.Create();

        vertical_target = new RenderTexture(Source.width, Source.height, 0)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp
        };
        vertical_target.Create();

        int gaussian_horizontal_handel = gaussian_compute.FindKernel("CS_gaussian_horizontal");
        int gaussian_vertical_handel   = gaussian_compute.FindKernel("CS_gaussian_vertical");

        gaussian_compute.SetTexture(gaussian_horizontal_handel, "_horizontal_source",  Source);
        gaussian_compute.SetTexture(gaussian_horizontal_handel, "_horizontal_results", horizontal_target);
        gaussian_compute.SetTexture(gaussian_vertical_handel,   "_vertical_source",    horizontal_target);
        gaussian_compute.SetTexture(gaussian_vertical_handel,   "_vertical_results",   vertical_target);

        gaussian_compute.SetInt("_kernel_size",      kernelSize);
        gaussian_compute.SetInt("_source_width",     Source.width);
        gaussian_compute.SetInt("_source_height",    Source.height);
        gaussian_compute.SetFloat("_gaussian_sigma", sigma);

        cb = new CommandBuffer()
        {
            name = "gaussian_blur_debug"
        };


        cb.DispatchCompute(gaussian_compute, gaussian_horizontal_handel, Source.width / 8, Source.height / 8, 1);
        cb.DispatchCompute(gaussian_compute, gaussian_vertical_handel,   Source.width / 8, Source.height / 8, 1);
        cb.Blit(vertical_target, BuiltinRenderTextureType.CameraTarget);

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);

    }

    // Update is called once per frame
    void Update()
    {
        gaussian_compute.SetInt("_kernel_size", kernelSize);
        gaussian_compute.SetFloat("_gaussian_sigma", sigma);
    }
}
