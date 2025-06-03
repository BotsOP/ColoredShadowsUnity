using System;
using UnityEngine;

public class CreepyManager : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private float speed;

    private void Update()
    {
        material.SetFloat("_Creepy", Mathf.Clamp01((Mathf.Sin(Time.time * speed) * 0.5f + 0.5f) * 3 - 1));
    }
}
