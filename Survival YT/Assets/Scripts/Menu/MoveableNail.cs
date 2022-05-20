using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MoveableNail : MonoBehaviour
{
    private bool _canDrag;
    private float _max = -0.136f;
    private float _min = -1.841574f;

    [SerializeField] private MenuManager _menuManager;
    
    private void OnMouseDown()
    {
        _canDrag = true;
    }
    

    private void OnMouseUp()
    {
        _canDrag = false;
    }
    
    void Update()
    {
        if (_canDrag)
        {
            if (transform.localPosition.x > _max)
            {
                transform.localPosition = new Vector3(_max, 0, 0);
                return;
            }

            if (transform.localPosition.x < _min)
            {
                transform.localPosition = new Vector3(_min, 0, 0);
                return;
            }
            
            transform.position += new Vector3(0, 0, -Input.GetAxis("Mouse X") / 5);
            _menuManager.SetVolume();
        }
    }
}
