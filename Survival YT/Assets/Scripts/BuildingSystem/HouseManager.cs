using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseManager : MonoBehaviour
{
    public List<BuildingBlock> buildingBlocks = new List<BuildingBlock>();

    public void UpdateBuildingBlocks()
    {
        buildingBlocks.Clear();
        buildingBlocks.AddRange(transform.GetComponentsInChildren<BuildingBlock>());
        
        foreach (BuildingBlock buildingBlock in buildingBlocks)
        {
            if (buildingBlock.GetComponent<Foundation>() != null)
            {
                Foundation foundation = buildingBlock.GetComponent<Foundation>();
                for (int i = 0; i < foundation.foundationConnections.Count; i++)
                {
                    foundation.CheckNearbyFoundations(i);
                }
                for (int i = 0; i < foundation.edgeConnections.Count; i++)
                {
                    foundation.CheckNearbyEdgeConnections(i);
                }
            }

            if (buildingBlock.GetComponent<EdgeBlock>() != null)
            {
                EdgeBlock edgeBlock = buildingBlock.GetComponent<EdgeBlock>();
                for (int i = 0; i < edgeBlock.ceilingConnections.Count; i++)
                {
                    edgeBlock.CheckNearbyCeilings(i);
                }
            }

            if (buildingBlock.GetComponent<Ceiling>() != null)
            {
                Ceiling ceiling = buildingBlock.GetComponent<Ceiling>();
                for (int i = 0; i < ceiling.ceilingConnections.Count; i++)
                {
                    ceiling.CheckNearbyCeilings(i);
                }
            }
        } 
    }
}
