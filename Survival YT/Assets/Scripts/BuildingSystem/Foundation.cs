using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foundation : BuildingBlock, ICheckColliders, IHaveConnections
{
    public List<GameObject> foundationConnections;
    public List<GameObject> edgeConnections;
    protected override void Start()
    {
        foreach (GameObject foundationConnection in foundationConnections)
        {
            foundationConnection.SetActive(false);
        }
        foreach (GameObject edgeConnection in edgeConnections)
        {
            edgeConnection.SetActive(false);
        }
        base.Start();
    }

    public void TurnOnConnections()
    {
        for (int i = 0; i < foundationConnections.Count; i++)
        {
            foundationConnections[i].SetActive(true);
        }

        foreach (GameObject edgeConnection in edgeConnections)
        {
            edgeConnection.SetActive(true);
        }
    }
    
    public void CheckColliders()
    {
        detectedColliders = new List<Collider>();
        Collider[] currentColliders = Physics.OverlapBox(transform.position, new Vector3(1.5f,0.75f,1.5f),
            transform.rotation,~LayerMask.GetMask("Terrain","FoundationConnection","EdgeConnection", "CeilingConnection", "DoorConnection"));

        detectedColliders.AddRange(currentColliders);
    }

    public void CheckNearbyFoundations(int i)
    {
        Collider[] overlapColliders = Physics.OverlapSphere(foundationConnections[i].transform.position,
            foundationConnections[i].GetComponent<SphereCollider>().radius);
        foreach (Collider overlapCollider in overlapColliders)
        {
            if (overlapCollider.gameObject.GetComponent<Foundation>() != null)
            {
                GameObject foundationConnection = foundationConnections[i];
                foundationConnections.Remove(foundationConnection);
                Destroy(foundationConnection);
                break;
            }
        }
    }

    public void CheckNearbyEdgeConnections(int i)
    {
        Collider[] overlapColliders = Physics.OverlapSphere(edgeConnections[i].transform.position + Vector3.down * 1.5f,
            0.5f,LayerMask.GetMask("EdgeConnection"));

        if (overlapColliders.Length >= 2)
        {
            GameObject edgeConnection = overlapColliders[0].gameObject;
            edgeConnections.Remove(edgeConnection);
            Destroy(edgeConnection);
        }
    }
}
