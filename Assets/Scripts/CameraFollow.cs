using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smoothTime = 0.25f; // Увеличили для большей плавности
    public float minX = -4f;
    public float maxX = 105f;
    public float minY = -2f;
    public float maxY = 8f;

    private Vector3 velocity;
    private Vector3 currentVelocityY; // Отдельная скорость для Y для более плавного слежения

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

        // Более плавное слежение по X и Y с раздельными коэффициентами
        float smoothX = smoothTime;
        float smoothY = smoothTime * 1.5f; // Y еще более плавный
        
        transform.position = new Vector3(
            Mathf.SmoothDamp(transform.position.x, desiredPosition.x, ref velocity.x, smoothX),
            Mathf.SmoothDamp(transform.position.y, desiredPosition.y, ref currentVelocityY, smoothY),
            desiredPosition.z
        );
    }
}
