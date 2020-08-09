using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DEPRICATED: ALL INITIALISATION NOW HAPENS IN THE GPU
static class CPUSystems{

    

    // Change the values here to change range of brush stroke properties that can pop up as initial value. To change the mutation variables, you should look in to the Selection_compute_functions.compute
    public static void InitatePopulationMember(ref Genes[] toPopulate, float scale_lower_bound, float scale_higher_bound)
    {
        for(int i = 0; i<toPopulate.Length; i++)
        {
            Genes member = new Genes();

            member.position_X = Random.Range(-1.0f, 1.0f);
            member.position_Y = Random.Range(-1.0f, 1.0f);

            member.z_Rotation = Random.value * Mathf.PI * 2.0f;

            float scale_adjust= 1.0f - ((float)(i) / (float)(toPopulate.Length));

            member.scale_X    = Random.Range(scale_lower_bound, scale_lower_bound + (scale_higher_bound - scale_lower_bound) * scale_adjust);
            member.scale_Y    = Random.Range(scale_lower_bound, scale_lower_bound + (scale_higher_bound - scale_lower_bound) * scale_adjust);

            member.color_r    = Random.value;
            member.color_g    = Random.value;
            member.color_b    = Random.value;

            member.texture_ID = Random.Range(0, 4);
            toPopulate[i] = member;
        }

    }


        public static void InitatePopulationMemberBW(ref Genes[] toPopulate, float scale_lower_bound, float scale_higher_bound)
    {
        for(int i = 0; i<toPopulate.Length; i++)
        {
            Genes member = new Genes();

            member.position_X = Random.Range(-1.0f, 1.0f);
            member.position_Y = Random.Range(-1.0f, 1.0f);

            member.z_Rotation = Random.value * Mathf.PI * 2.0f;

            float scale_adjust= 1.0f - ((float)(i) / (float)(toPopulate.Length));

            member.scale_X    = Random.Range(scale_lower_bound, scale_lower_bound + (scale_higher_bound - scale_lower_bound) * scale_adjust);
            member.scale_Y    = Random.Range(scale_lower_bound, scale_lower_bound + (scale_higher_bound - scale_lower_bound) * scale_adjust);

            member.color_r    = Random.value;
            member.color_g    = member.color_r;
            member.color_b    = member.color_r;

            member.texture_ID = Random.Range(0, 4);
            toPopulate[i] = member;
        }

    }



}