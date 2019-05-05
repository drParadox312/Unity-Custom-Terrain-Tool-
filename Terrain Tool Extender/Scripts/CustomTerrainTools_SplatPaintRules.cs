using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TerrainToolExtender/create new  Splat Paint Rules")]
[System.Serializable]
public class CustomTerrainTools_SplatPaintRules : ScriptableObject
{
     // height parameters
    public float minHeightStart ;
    public float minHeightEnd ;
    public float maxHeightStart ;
    public float maxHeightEnd ;
    public bool inverseHeightRule ;
    public bool useHeightTransition ;
    public bool applyHeightRule ;



    // angle parameters
    public float minAngleStart ;
    public float minAngleEnd ;
    public float maxAngleStart ;
    public float maxAngleEnd ;
    public bool inverseAngleRule ;
    public bool useAngleTransition ;
    public bool applyAngleRule ;


    public CustomTerrainTools_SplatPaintRules()
    {
        this.minHeightStart = 0f;
        this.minAngleEnd = 0f;
        this.maxHeightStart = 3000f;
        this.maxHeightEnd = 3000f;
        this.inverseHeightRule = false;
        this.useHeightTransition = false;
        this.applyHeightRule = true;

        this.minAngleStart = 0f;
        this.minAngleEnd = 0f;
        this.maxAngleStart = 90f;
        this.maxAngleEnd = 90f;
        this.inverseAngleRule = false;
        this.useAngleTransition = false;
        this.applyAngleRule = true;
    }


    public void SetMaxHeights(float maxHeights)
    {
        this.maxHeightStart = maxHeights;
        this.maxHeightEnd = maxHeights;
    }

}
