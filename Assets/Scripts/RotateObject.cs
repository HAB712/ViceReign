using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField] private float speed = 80f;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}