using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("Raycast Settings")] [SerializeField]
    private Camera _mainCamera;

    [SerializeField] private float _reachDistance = 2f;

    private Transform _blockConnection;
    private string _connectionType = "";

    private GameObject _buildingBlockOnScene;
    [HideInInspector] public GameObject currentBuildingBlock;

    void Update()
    {
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        SetConnectionTypeBasedOnBuildingBlock();

        if (Physics.Raycast(ray, out hit, _reachDistance, LayerMask.GetMask("Terrain", _connectionType)))
        {
            if (_buildingBlockOnScene == null)
            {
                CreateNewBlock();
            }

            Transform buildingBlockTransform = _buildingBlockOnScene.transform;

            _buildingBlockOnScene.GetComponent<ICheckColliders>().CheckColliders(); 

            if (LayerMask.LayerToName(hit.collider.gameObject.layer) == _connectionType)
            {
                var blockConnection = hit.collider.transform;
                _blockConnection = blockConnection;

                buildingBlockTransform.position = blockConnection.position;
                buildingBlockTransform.rotation = blockConnection.rotation;

                if (_blockConnection.parent.GetComponentInParent<HouseManager>() != null)
                {
                    RemoveHouseBlocksFromCollidersList();
                }
                else
                {
                    RemoveFirstBlockFromCollidersList();
                }
            }
            else
            {
                _blockConnection = null;

                buildingBlockTransform.position = hit.point;
                buildingBlockTransform.rotation = Quaternion.Euler(buildingBlockTransform.rotation.eulerAngles.x,
                    _mainCamera.transform.rotation.eulerAngles.y, buildingBlockTransform.rotation.eulerAngles.z);
            }
        }
        else
        {
            if (_buildingBlockOnScene != null)
            {
                Destroy(_buildingBlockOnScene);
            }
        }
    }

    private void SetConnectionTypeBasedOnBuildingBlock()
    {
        if (currentBuildingBlock.GetComponent<Foundation>() != null)
        {
            _connectionType = "FoundationConnection";
        }
        else if (currentBuildingBlock.GetComponent<EdgeBlock>() != null)
        {
            _connectionType = "EdgeConnection";
        }
        else if (currentBuildingBlock.GetComponent<Ceiling>() != null)
        {
            _connectionType = "CeilingConnection";
        }
        else if (currentBuildingBlock.GetComponent<Door>() != null)
        {
            _connectionType = "DoorConnection";
        }
    }

    private void RemoveFirstBlockFromCollidersList()
    {
        if (_buildingBlockOnScene.GetComponent<BuildingBlock>()
            .detectedColliders.Contains(_blockConnection.parent.GetComponent<Collider>()))
        {
            _buildingBlockOnScene.GetComponent<BuildingBlock>()
                .detectedColliders.Remove(_blockConnection.parent.GetComponent<Collider>());
        }
    }

    private void RemoveHouseBlocksFromCollidersList()
    {
        foreach (BuildingBlock buildingBlock in _blockConnection.parent.GetComponentInParent<HouseManager>()
            .buildingBlocks)
        {
            if (_buildingBlockOnScene.GetComponent<BuildingBlock>()
                .detectedColliders.Contains(buildingBlock.GetComponent<Collider>()))
            {
                _buildingBlockOnScene.GetComponent<BuildingBlock>()
                    .detectedColliders.Remove(buildingBlock.GetComponent<Collider>());
            }
        }
    }

    public void ChangeBuildingBlock()
    {
        if (currentBuildingBlock == null) return;

        Destroy(_buildingBlockOnScene);
        CreateNewBlock();
    }

    private void CreateNewBlock()
    {
        if (currentBuildingBlock == null)
            return;
        
        _buildingBlockOnScene = Instantiate(currentBuildingBlock);
        if(_buildingBlockOnScene.GetComponent<Collider>() != null) 
            _buildingBlockOnScene.GetComponent<Collider>().enabled = false;
    }

    public void PlaceBlock()
    {
        _buildingBlockOnScene.GetComponent<BuildingBlock>().isPlaced = true;
        
        if(_buildingBlockOnScene.GetComponent<Collider>() != null)
            _buildingBlockOnScene.GetComponent<Collider>().enabled = true;
        
        if(_buildingBlockOnScene.GetComponent<IHaveConnections>() != null)
            _buildingBlockOnScene.GetComponent<IHaveConnections>().TurnOnConnections();
        
        _buildingBlockOnScene.GetComponent<BuildingBlock>().ChangeToBlockMaterial();

        if (_blockConnection != null)
        {
            // Первая часть
            if (_blockConnection.parent.parent == null)
            {
                GameObject house = Instantiate(new GameObject("House"), _buildingBlockOnScene.transform.position,
                    Quaternion.identity);
                house.AddComponent<HouseManager>();
                _blockConnection.parent.SetParent(house.transform);
            }
            // Вторая часть
            _buildingBlockOnScene.transform.position = _blockConnection.position;
            _buildingBlockOnScene.transform.rotation = _blockConnection.rotation;
            _buildingBlockOnScene.transform.SetParent(_blockConnection.parent.parent);

            // Третья часть
            switch (_connectionType)
            {
                case "EdgeConnection":
                    if (_blockConnection.parent.GetComponent<Foundation>() != null)
                    {
                        GameObject edgeConnection = _blockConnection.gameObject;
                        _blockConnection.parent.GetComponent<Foundation>().edgeConnections.Remove(edgeConnection);
                        Destroy(edgeConnection);
                    }

                    break;
                case "DoorConnection":
                    _buildingBlockOnScene.GetComponent<Door>().TurnOnSeparateDoorColliders();
                    Destroy(_blockConnection.gameObject);
                    break;
            }
            _buildingBlockOnScene.transform.parent.GetComponent<HouseManager>().UpdateBuildingBlocks();
            
        }

        CreateNewBlock();
    }

    public bool CanPlace()
    {
        if (_buildingBlockOnScene == null) return false;

        return _buildingBlockOnScene.GetComponent<BuildingBlock>().canPlace;
    }

    private void OnEnable()
    {
        ChangeBuildingBlock();
    }

    private void OnDisable()
    {
        Destroy(_buildingBlockOnScene);
    }
}