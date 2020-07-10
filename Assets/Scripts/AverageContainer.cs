using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AverageContainer                     // combination of a ring buffer and a data container that holds sums
{
    public  int     length;
    private float[] values;
    private int     index = 0;
    private float   sum   = 0;
    private int     numberOfFilledElements;

    public AverageContainer(int size)
    {
        numberOfFilledElements = 0;
        length = size;
        index  = 0;
        sum    = 0;
        values = new float[length];
    }


    public float GetAverage()
    {
        return sum / Mathf.Max(1, (float)numberOfFilledElements); // avoid division by zero
    }

    public void Add(float member)
    {
        sum = sum - values[index];         // Subtract the value we will be replacing from the sum
        sum = sum + member;                // instead add the new value. This is so that I dont have to sum up the members each time an average is requested
        values[index] = member;            // replace the new member with the oldest entry
        numberOfFilledElements = Mathf.Min(length, numberOfFilledElements + 1 );
        IncrementIndex();
    }

    private void IncrementIndex()           // implements the ring allocater type of behaviour
    {
        if ((index + 1)>= length) index = 0;
        else index++;
    }
}
