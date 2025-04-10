using System;
using UnityEngine;

public class LookAtZeroZero : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        transform.LookAt(Vector3.zero);
    }
    void Update()
    {
        transform.LookAt(Vector3.zero);
    }
}
