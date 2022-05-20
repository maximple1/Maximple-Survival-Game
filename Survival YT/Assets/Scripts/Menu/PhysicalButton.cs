using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class PhysicalButton : MonoBehaviour
{
    private Animator _animator;
    public UnityEvent _onClickEvent;
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnMouseEnter()
    {
        _animator.SetBool("IsMouseOver",true);
    }

    private void OnMouseExit()
    {
        _animator.SetBool("IsMouseOver",false);
    }

    private void OnMouseDown()
    {
        _onClickEvent.Invoke();
    }
}
