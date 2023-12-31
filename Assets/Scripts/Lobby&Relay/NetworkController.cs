using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class NetworkController : NetworkBehaviour
{
    [SerializeField] GameObject cameraHolder;
    [SerializeField] Vector3 offset;

    private void Start()
    {
        
    }

    private void Update()
    {
        cameraHolder.SetActive(IsOwner);
    }
}
