using System;
using UnityEngine;

public class Dolly : MonoBehaviour
{
    [SerializeField] private Transform pos1;
    [SerializeField] private Transform pos2;
    [SerializeField] private float speed;

    private void Update()
    {
        float t = (Time.time - 5) / speed;
        transform.position = Vector3.Lerp(pos1.position, pos2.position, t);
        transform.rotation = Quaternion.Lerp(pos1.rotation, pos2.rotation, t);
        Vector3 pos = transform.position;
        pos.z = pos1.position.z;
        transform.position = pos;
    }
}
