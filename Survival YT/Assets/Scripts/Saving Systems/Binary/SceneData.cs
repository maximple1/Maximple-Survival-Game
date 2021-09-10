using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SceneData
{
    public string[] objectNames;
    public Hector[] objectPositions;
    public int[] objectAmounts;
    public SceneData(Transform parentTransform)
    {
        var childCount = parentTransform.childCount;
        objectNames = new string[childCount];
        objectPositions = new Hector[childCount];
        objectAmounts = new int[childCount];
        
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform currentObject = parentTransform.GetChild(i);
            
            objectNames[i] = currentObject.name;
            
            var position = currentObject.position;
            objectPositions[i] = new Hector(position.x, position.y, position.z);
            
            objectAmounts[i] = currentObject.GetComponent<Item>().amount;
        }
    }
    [System.Serializable]
    public class Hector
    {
        public float x;
        public float y;
        public float z;

        public Hector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
