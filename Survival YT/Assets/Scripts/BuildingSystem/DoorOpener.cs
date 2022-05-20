using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float reachDistance = 3f;
    
    
    void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Physics.Raycast(ray, out hit, reachDistance))
            {
                if (hit.collider.GetComponentInParent<Door>() != null)
                {
                    hit.collider.GetComponentInParent<Door>().InvertDoorState();
                }
            }
        }
    }
}
