using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeBlock : BuildingBlock, ICheckColliders, IHaveConnections
{
    public List<GameObject> ceilingConnections;
    
    protected override void Start()
    {
        foreach (GameObject ceilingConnection in ceilingConnections)
        {
            ceilingConnection.SetActive(false);
        }
        base.Start();
    }
    
    public void CheckColliders()
    {
        detectedColliders = new List<Collider>();
        
        Collider[] currentColliders = Physics.OverlapBox(transform.position, _colliderExtents/2,
            transform.rotation,~LayerMask.GetMask("FoundationConnection","EdgeConnection", "CeilingConnection", "DoorConnection"));

        detectedColliders.AddRange(currentColliders);
    }
    
    public void CheckNearbyCeilings(int i)
    {
        Collider[] overlapColliders = Physics.OverlapSphere(ceilingConnections[i].transform.position,
            ceilingConnections[i].GetComponent<SphereCollider>().radius);
        foreach (Collider overlapCollider in overlapColliders)
        {
            if (overlapCollider.gameObject.GetComponent<Ceiling>() != null)
            {
                GameObject ceilingConnection = ceilingConnections[i];
                ceilingConnections.Remove(ceilingConnection);
                Destroy(ceilingConnection);
                break;
            }
        }
    }
    
    public void TurnOnConnections()
    {
        foreach (var ceilingConnection in ceilingConnections)
        {
            ceilingConnection.SetActive(true);
        }
    }
}
