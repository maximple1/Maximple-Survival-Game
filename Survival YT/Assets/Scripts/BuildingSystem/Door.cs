using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : BuildingBlock, ICheckColliders
{
    [SerializeField] private Animator _animator;
    private static readonly int IsOpened = Animator.StringToHash("IsOpened");
    [SerializeField] private List<MeshCollider> _doorMeshColliders;
    

    public void CheckColliders()
    {
        detectedColliders = new List<Collider>(); 
        Collider[] currentColliders = Physics.OverlapBox(transform.position, new Vector3(0.2f,1.5f,1.5f),
            transform.rotation,~LayerMask.GetMask("FoundationConnection","EdgeConnection", "CeilingConnection", "DoorConnection"));

        detectedColliders.AddRange(currentColliders);
    }

    public void InvertDoorState()
    {
        _animator.SetBool(IsOpened,!_animator.GetBool(IsOpened));
    }

    public void TurnOnSeparateDoorColliders()
    {
        foreach (MeshCollider doorMeshCollider in _doorMeshColliders)
        {
            doorMeshCollider.enabled = true;
        }
    }
}
