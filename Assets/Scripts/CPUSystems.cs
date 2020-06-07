using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class CPUSystems{


    public static void InitatePopulationMember(ref Genes[] toPopulate)
    {
        for(int i = 0; i<toPopulate.Length; i++)
        {
            Genes member = new Genes();

            member.position_X = Random.Range(-1.0f, 1.0f);
            member.position_Y = Random.Range(-1.0f, 1.0f);

            member.z_Rotation = Random.value * Mathf.PI * 2.0f;

            member.scale_X    = Random.Range(0.1f, 0.5f);
            member.scale_Y    = Random.Range(0.1f, 0.5f);

            member.color_r    = Random.value;
            member.color_g    = Random.value;
            member.color_b    = Random.value;

            member.texture_ID = Random.Range(0, 4);
            toPopulate[i] = member;
        }

    }

}