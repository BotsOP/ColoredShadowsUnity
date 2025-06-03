using System;
using UnityEngine;

public class Dolly : MonoBehaviour
{
    [SerializeField] private Transform pos1;
    [SerializeField] private Transform pos2;
    [SerializeField] private float speed;

    private void Update()
    {
        transform.position = Vector3.Lerp(pos1.position, pos2.position, (Time.time - 5) / speed);
    }
}
