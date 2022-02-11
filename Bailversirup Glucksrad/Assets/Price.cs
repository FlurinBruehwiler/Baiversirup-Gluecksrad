using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Price", menuName = "Price")]
public class Price : ScriptableObject
{
    public string name = "";

    public bool hauptGewinn;
    
    [TextArea]
    public string text = "";
    
    [Range(0f, 100f)]
    public float fromProbability = 0;

    [Range(0f, 100f)]
    public float toProbability = 0;
}
