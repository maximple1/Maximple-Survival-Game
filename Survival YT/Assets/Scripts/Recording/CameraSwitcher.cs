using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _mainCVC;
    [SerializeField] private CinemachineVirtualCamera _cinematicCVC;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _mainCVC.Priority = 1;
            _cinematicCVC.Priority = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            _mainCVC.Priority = 0;
            _cinematicCVC.Priority = 1;
        }
    }
}
