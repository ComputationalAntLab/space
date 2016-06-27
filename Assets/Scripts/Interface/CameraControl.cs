﻿using UnityEngine;
using System.Collections;
using Assets.Scripts.Extensions;
using UnityEngine.UI;
using System;

public class CameraControl : MonoBehaviour
{
    private Button btnToggle;

    private bool _isFreeCamera = false;

    private Camera _freeCamera;
    private Camera _mainCamera;

    void Start()
    {
        btnToggle = this.ButtonByName("btnChangeCamera");

        btnToggle.onClick.AddListener(btnToggle_Click);

        _freeCamera = GameObject.FindGameObjectWithTag("FreeCamera").GetComponent<Camera>();
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        UpdateCamera();
    }

    private void UpdateCamera()
    {
        _freeCamera.enabled = _isFreeCamera;
        _mainCamera.enabled = !_isFreeCamera;

        if (_isFreeCamera)
        {
            btnToggle.GetComponentInChildren<Text>().text = "Free Cam";
        }
        else
        {
            btnToggle.GetComponentInChildren<Text>().text = "Fixed Cam";
        }
    }

    private void btnToggle_Click()
    {
        _isFreeCamera = !_isFreeCamera;
        UpdateCamera();
    }
}
