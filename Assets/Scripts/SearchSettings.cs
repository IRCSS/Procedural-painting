using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SearchSettings", order = 1)]
public class SearchSettings : ScriptableObject
{

    public StageSeries[] series = new StageSeries[1];
}
