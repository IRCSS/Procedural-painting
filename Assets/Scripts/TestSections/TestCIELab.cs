using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestCIELab : MonoBehaviour
{
    public Texture debug_texture;

    private Material cielab_mat;

    private CommandBuffer cb;

    // Start is called before the first frame update
    void Start()
    {
        cielab_mat = new Material(Shader.Find("Unlit/CIELabBlit"));
        cb = new CommandBuffer()
        {
            name = "CIELabPass"
        };

        cb.Blit(debug_texture, BuiltinRenderTextureType.CameraTarget, cielab_mat);

        Camera.main.AddCommandBuffer(CameraEvent.AfterEverything, cb);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
