using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
     
public class SampleOnEdgesTest : MonoBehaviour
{

    public  Texture       mask;
    public  ComputeShader construct_position_domain_compute;

    private Material      debug_positions_mat;


    private ComputeBuffer position_domain_buffer;
    private ComputeBuffer debug_positions_buffer;
    private ComputeBuffer positon_domain_arguments_buffer;

    private CommandBuffer command_buffer;

    private int           numberOfPoints =128;

    private Camera        cam;

    private int           Construct_Position_Domain_handel;
    private int           Debug_Position_Domain_handel;

    private int           generation_id;

    public struct float2
    {
        public float x, y;
    }

    void Start()
    {

        // -----------------------------------------

        if (mask.width > 1024 || mask.height > 1024) Debug.LogError("image provided is bigger than 1024. This probabaly not what you want");

        position_domain_buffer = new ComputeBuffer(mask.width * mask.height, sizeof(float) * 2, ComputeBufferType.Append);
        position_domain_buffer.SetCounterValue(0);

        //_____________
        positon_domain_arguments_buffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        int[] ini_value = new int[4] { 0, 0, 0, 0 };
        positon_domain_arguments_buffer.SetData(ini_value);

        //_____________

        debug_positions_buffer = new ComputeBuffer(numberOfPoints, sizeof(float) * 2);

        // -----------------------------------------
        debug_positions_mat = new Material(Shader.Find("Unlit/pointRenderer"));
        if (!debug_positions_mat) Debug.LogError("No Material found!");
        debug_positions_mat.SetBuffer("buffer", debug_positions_buffer);
        // -----------------------------------------
        Construct_Position_Domain_handel = construct_position_domain_compute.FindKernel("CS_Construct_Position_Domain");
        Debug_Position_Domain_handel     = construct_position_domain_compute.FindKernel("CS_Debug_Position_Domain");

        construct_position_domain_compute.SetTexture(Construct_Position_Domain_handel, "_mask",                   mask);
        construct_position_domain_compute.SetBuffer (Construct_Position_Domain_handel, "_position_domain_buffer", position_domain_buffer);

        construct_position_domain_compute.SetInt("_image_width",  mask.width);
        construct_position_domain_compute.SetInt("_image_height", mask.height);

        construct_position_domain_compute.SetBuffer(Debug_Position_Domain_handel, "_R_position_domain_buffer",        position_domain_buffer);
        construct_position_domain_compute.SetBuffer(Debug_Position_Domain_handel, "_position_domain_argument_buffer", positon_domain_arguments_buffer);
        construct_position_domain_compute.SetBuffer(Debug_Position_Domain_handel, "_debug_position_buffer",           debug_positions_buffer);


        // -----------------------------------------
        cam = Camera.main;
        if (!cam) Debug.LogError("No Camera is tagged as main");

        cam.clearFlags   = CameraClearFlags.Nothing;
        cam.orthographic = true;
        cam.orthographicSize = 1;

        // -----------------------------------------

        construct_position_domain_compute.Dispatch(Construct_Position_Domain_handel, mask.width / 8, mask.height / 8, 1);
        ComputeBuffer.CopyCount(position_domain_buffer, positon_domain_arguments_buffer, 0);


        int[] counter = new int[4];

        positon_domain_arguments_buffer.GetData(counter);

        Debug.Log(string.Format("The number of pixels in the position domains is: {0}", counter[0]));

        // -----------------------------------------

        command_buffer = new CommandBuffer()
        {
            name = "constructPositionDomain"
        };


        command_buffer.DispatchCompute(construct_position_domain_compute, Debug_Position_Domain_handel,
                                       numberOfPoints / 64, 1, 1);
        command_buffer.DrawProcedural(Matrix4x4.identity, debug_positions_mat, 0, MeshTopology.Points, numberOfPoints);


        cam.AddCommandBuffer(CameraEvent.AfterEverything, command_buffer);
    }


    void Update()
    {
        construct_position_domain_compute.SetInt("_generation_seed", generation_id);
        generation_id++;


        if (Input.GetKeyDown(KeyCode.D))
        {
            float2[] points_pos = new float2[numberOfPoints];
            debug_positions_buffer.GetData(points_pos);

            int i = 0;
            foreach(float2 f in points_pos)
            {

                Debug.Log(string.Format("Partile index {0}, has position ({1}, {2})", i, f.x, f.y));
                i++;
            }
        }

    }
}
