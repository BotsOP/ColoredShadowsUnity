using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float y = Time.timeSinceLevelLoad % 1;
        transform.position = new Vector3(transform.position.x, -y * 5 + 4, transform.position.z);
    }
}
