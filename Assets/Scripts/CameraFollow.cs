using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smoothTime = 0.18f;
    public float minX = -4f;
    public float maxX = 105f;
    public float minY = -2f;
    public float maxY = 8f;

    private Vector3 velocity;
    private Vector3 _basePos;   // сглаженная позиция без тряски (чтобы тряска не «накапливалась» в SmoothDamp)
    private bool _baseInit = false;

    private float _shakeTimer = 0f;
    private float _shakeDuration = 0f;
    private float _shakeMagnitude = 0f;

    void Awake()
    {
        Instance = this;
    }

    // Тряска камеры. Не перебивается более слабым импульсом, пока действует сильный.
    public void Shake(float duration, float magnitude)
    {
        if (magnitude * duration >= _shakeMagnitude * _shakeTimer)
        {
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTimer = _shakeDuration;
            _shakeMagnitude = magnitude;
        }
    }

    // Мгновенно ставит камеру на игрока (без плавного доезда). Вызывается при смене уровня.
    public void SnapToTarget()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            target = player.transform;
        }

        Vector3 p = target.position + offset;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        p.z = offset.z;

        _basePos = p;
        _baseInit = true;
        velocity = Vector3.zero;
        transform.position = p;
    }

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

        if (!_baseInit)
        {
            _basePos = transform.position;
            _baseInit = true;
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        desiredPosition.z = offset.z;

        _basePos = Vector3.SmoothDamp(_basePos, desiredPosition, ref velocity, smoothTime);

        // Тряска добавляется поверх базовой позиции и плавно затухает.
        Vector3 shake = Vector3.zero;
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float k = Mathf.Clamp01(_shakeTimer / _shakeDuration);
            Vector2 r = Random.insideUnitCircle * (_shakeMagnitude * k);
            shake = new Vector3(r.x, r.y, 0f);
        }

        transform.position = _basePos + shake;
    }
}
