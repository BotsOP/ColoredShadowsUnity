using System;
using UnityEngine;

public class CustomInputManager : MonoBehaviour
{
    [SerializeField] private Material material1;
    [SerializeField] private Material material2;
    [SerializeField] private Gradient gradient1;
    [SerializeField] private Gradient gradient2;
    [SerializeField] private float speed;

    private void Update()
    {
        material1.SetColor("_Color", gradient1.Evaluate(Time.time * speed % 1));
        material2.SetColor("_Color", gradient2.Evaluate(Time.time * speed % 1));
    }
}
