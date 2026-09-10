using UnityEngine;

public class Needle : MonoBehaviour
{
    public float speed = 90f; // grados por segundo

    void Update()
    {
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }

    public float CurrentAngle
    {
        get => (360f - transform.eulerAngles.z) % 360f;
    }
}
