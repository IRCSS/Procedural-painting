using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class gaussian_sobel_debug : MonoBehaviour
{
    public Texture        Source;
    public ComputeShader  gaussian_compute;
    public ComputeShader  sobel_compute;
                          
    public int            kernelSize = 3;
    public float          sigma      = 4;
    public int            sobelStep  = 1;

    private RenderTexture target_ping;           // Used for several operations
    private RenderTexture target_pong;           // Used for several operations
    private CommandBuffer cb;

    private Camera        main_cam;

    // Start is called before the first frame update
    void Start()
    {
        main_cam = Camera.main;

        target_ping = new RenderTexture(Source.width, Source.height, 0)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp
        };
        target_ping.Create();

        target_pong = new RenderTexture(Source.width, Source.height, 0)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp
        };
        target_pong.Create();


        // ------------------------------------------------------------------------------------------------------------------------

        int gaussian_horizontal_handel = gaussian_compute.FindKernel("CS_gaussian_horizontal");
        int gaussian_vertical_handel   = gaussian_compute.FindKernel("CS_gaussian_vertical");
        int sobel_handel               = sobel_compute.FindKernel("Sobel");


        // ------------------------------------------------------------------------------------------------------------------------

        gaussian_compute.SetTexture(gaussian_horizontal_handel, "_horizontal_source",  Source);
        gaussian_compute.SetTexture(gaussian_horizontal_handel, "_horizontal_results", target_ping);
        gaussian_compute.SetTexture(gaussian_vertical_handel,   "_vertical_source",    target_ping);
        gaussian_compute.SetTexture(gaussian_vertical_handel,   "_vertical_results",   target_pong);

        gaussian_compute.SetInt("_kernel_size",      kernelSize);
        gaussian_compute.SetInt("_source_width",     Source.width);
        gaussian_compute.SetInt("_source_height",    Source.height);
        gaussian_compute.SetFloat("_gaussian_sigma", sigma);

        sobel_compute.SetTexture(sobel_handel, "_source", target_pong);
        sobel_compute.SetTexture(sobel_handel, "_result", target_ping);

        sobel_compute.SetInt("_source_width",  Source.width);
        sobel_compute.SetInt("_source_height", Source.height);
        sobel_compute.SetInt("_kernel_size",   sobelStep);

        // ------------------------------------------------------------------------------------------------------------------------

        cb = new CommandBuffer()
        {
            name = "gaussian_blur_debug"
        };


        cb.DispatchCompute(gaussian_compute, gaussian_horizontal_handel, Source.width / 8, Source.height / 8, 1);
        cb.DispatchCompute(gaussian_compute, gaussian_vertical_handel,   Source.width / 8, Source.height / 8, 1);
        cb.DispatchCompute(sobel_compute, sobel_handel, Source.width / 32, Source.height / 32, 1);



        cb.Blit(target_ping, BuiltinRenderTextureType.CameraTarget);

        main_cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
    }

    // Update is called once per frame
    void Update()
    {
        gaussian_compute.SetInt("_kernel_size",      Mathf.Max(0, kernelSize));
        gaussian_compute.SetFloat("_gaussian_sigma", Mathf.Max(0.001f, sigma));
        sobel_compute.SetInt("_kernel_size",         sobelStep);
    }
}
