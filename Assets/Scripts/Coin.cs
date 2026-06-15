using UnityEngine;

// Монетка из пула. Физика скриптовая (без Rigidbody) — подскок, отскок от уровня спавна,
// затухание, магнит к игроку при приближении и подбор. По завершении возвращается в пул.
public class Coin : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Transform _player;
    private CoinManager _manager;

    private Vector2 _velocity;
    private float _restY;
    private bool _settled;
    private float _age;
    private bool _active;

    private float _gravity;
    private float _bounce;
    private float _pickupRange;
    private float _collectRadius;
    private float _magnetSpeed;
    private float _lifetime;

    public void Init(SpriteRenderer renderer, CoinManager manager)
    {
        _renderer = renderer;
        _manager = manager;
    }

    public void Launch(Vector3 position, Transform player, float gravity, float bounce,
        float pickupRange, float collectRadius, float magnetSpeed, float lifetime)
    {
        _player = player;
        _gravity = gravity;
        _bounce = bounce;
        _pickupRange = pickupRange;
        _collectRadius = collectRadius;
        _magnetSpeed = magnetSpeed;
        _lifetime = lifetime;

        transform.position = position;
        _restY = position.y - 0.2f;
        _velocity = new Vector2(Random.Range(-2.2f, 2.2f), Random.Range(3.5f, 5f));
        _settled = false;
        _age = 0f;
        _active = true;

        if (_renderer != null)
        {
            Color c = _renderer.color;
            c.a = 1f;
            _renderer.color = c;
        }
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!_active) return;

        float dt = Time.deltaTime;
        _age += dt;

        // Затухание в конце жизни.
        if (_age > _lifetime)
        {
            if (_renderer != null)
            {
                Color c = _renderer.color;
                c.a -= dt * 2f;
                _renderer.color = c;
                if (c.a <= 0f) { Despawn(); return; }
            }
            else { Despawn(); return; }
        }

        // Магнит к игроку при приближении.
        bool magnetized = false;
        if (_player != null && _age > 0.25f)
        {
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= _collectRadius)
            {
                Collect();
                return;
            }
            if (dist <= _pickupRange)
            {
                transform.position = Vector3.MoveTowards(transform.position, _player.position, _magnetSpeed * dt);
                magnetized = true;
            }
        }

        // Скриптовая физика подскока/отскока (пока монетку не притягивает игрок).
        if (!magnetized && !_settled)
        {
            _velocity.y -= _gravity * dt;
            Vector3 p = transform.position + (Vector3)(_velocity * dt);
            if (p.y <= _restY && _velocity.y < 0f)
            {
                p.y = _restY;
                if (Mathf.Abs(_velocity.y) > 1.2f)
                {
                    _velocity.y = -_velocity.y * _bounce;
                    _velocity.x *= 0.55f;
                }
                else
                {
                    _velocity = Vector2.zero;
                    _settled = true;
                }
            }
            transform.position = p;
        }
    }

    private void Collect()
    {
        if (_manager != null) _manager.OnCoinCollected(transform.position);
        Despawn();
    }

    private void Despawn()
    {
        _active = false;
        if (_manager != null) _manager.ReturnToPool(this);
        else gameObject.SetActive(false);
    }
}
