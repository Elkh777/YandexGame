using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smoothTime = 0.18f;
    public float minX = -4f;
    public float maxX = 105f;
    public float minY = -2f;
    public float maxY = 8f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                return;
            }
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        desiredPosition.z = offset.z;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
