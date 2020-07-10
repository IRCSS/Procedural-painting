using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Genes
{
    public float position_X, position_Y;    // screen space 0-1
    public float z_Rotation;                // 0 to tau
    public float scale_X, scale_Y;          // scale in quad aligned space, will be clamped
    public float color_r, color_g, color_b; // the colors of each stroke
    public int   texture_ID;                // such a waste of bit space. I only need 2 bits, but paying the cost 8 + padding
}  // Struct size 8 *4 bytes + 1  * 4 = 36 bytes



public struct PopulationMember
{
   public int population_Handel;
}

public struct MemberIDFitnessPair
{
    public uint  memberID;
    public float memberFitness;
};
