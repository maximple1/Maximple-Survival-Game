using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    //[SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
    [SerializeField] private Transform _pivot;
    private float _yRot;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _rotationRadius = 5f;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _yRot += Time.deltaTime * _rotationSpeed;
        _pivot.rotation = Quaternion.Euler(0,_yRot,0);

        transform.position = _pivot.position + _pivot.forward * _rotationRadius;
    }
}
