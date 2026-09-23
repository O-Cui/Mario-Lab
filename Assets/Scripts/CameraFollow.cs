using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float minX = 0f;
    public float maxX = 24f;
    [Min(0f)] public float smoothTime = 0.15f;

    private float horizontalVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float desiredX = Mathf.Clamp(target.position.x + 3f, minX, maxX);
        float x = Mathf.SmoothDamp(transform.position.x, desiredX, ref horizontalVelocity, smoothTime);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
