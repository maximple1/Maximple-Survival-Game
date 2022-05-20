using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBlock : MonoBehaviour
{
    private Material[] _blockMaterials;
    [SerializeField] private Material _greenMaterial;
    [SerializeField] private Material _redMaterial;
    
    public List<Collider> detectedColliders;
    private MeshRenderer[] _meshRenderers;

    protected Vector3 _colliderExtents;
    
    public bool canPlace;
    public bool isPlaced;

    protected virtual void Start()
    {
        if(GetComponent<BoxCollider>() != null)
            _colliderExtents = GetComponent<BoxCollider>().size;
        
        _meshRenderers = GetComponentsInChildren<MeshRenderer>();
        _blockMaterials = _meshRenderers[0].materials;

        if (!isPlaced)
        {
            UpdateMaterials();
        }
        else
        {
            this.enabled = false;
        }   
    }
    
    protected virtual void Update()
    {
        canPlace = detectedColliders.Count <= 0;
        if (!isPlaced)
        {
            UpdateMaterials();
        }
        else
        {
            this.enabled = false;
        }
    }

    private void UpdateMaterials()
    {
        foreach (MeshRenderer meshRenderer in _meshRenderers)
        {
            Material[] redMaterials = new Material[meshRenderer.materials.Length];
            for (int i = 0; i < redMaterials.Length; i++)
            {
                redMaterials[i] = _redMaterial;
            }

            Material[] greenMaterials = new Material[meshRenderer.materials.Length];
            for (int i = 0; i < greenMaterials.Length; i++)
            {
                greenMaterials[i] = _greenMaterial;
            }

            meshRenderer.materials = detectedColliders.Count > 0 ? redMaterials : greenMaterials;
        }
    }

    public void ChangeToBlockMaterial()
    {
        foreach (MeshRenderer meshRenderer in _meshRenderers)
        {
            meshRenderer.materials = _blockMaterials;
        }
    }
}