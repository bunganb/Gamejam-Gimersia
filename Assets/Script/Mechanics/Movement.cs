using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8f;
    public float speedMultiplier = 1f;
    public Vector2 initialDirection;
    public LayerMask obstacleLayer;

    [Header("Collision Settings")]
    [Tooltip("Ukuran box untuk deteksi rintangan (lebar/tinggi)")]
    [SerializeField] private float boxCastSize = 0.75f;
    [Tooltip("Jarak deteksi rintangan ke depan")]
    [SerializeField] private float boxCastDistance = 1.5f;

    // Event untuk animasi
    public UnityEvent<Vector2> OnDirectionChanged;

    public Rigidbody2D Rb { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 NextDirection { get; private set; }
    public Vector3 StartingPosition { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        StartingPosition = transform.position;
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        speedMultiplier = 1f;
        Direction = initialDirection;
        NextDirection = Vector2.zero;
        transform.position = StartingPosition;
        enabled = true;
    }

    private void Update()
    {
        // Jika ada arah yang diinginkan, coba terapkan
        if (NextDirection != Vector2.zero)
        {
            SetDirection(NextDirection);
        }
    }

    private void FixedUpdate()
    {
        Vector2 translation = Direction * speed * speedMultiplier * Time.fixedDeltaTime;
        Rb.MovePosition(Rb.position + translation);
    }

    public void SetDirection(Vector2 newDirection, bool forced = false)
    {
        if (forced || !Occupied(newDirection))
        {
            Direction = newDirection;
            NextDirection = Vector2.zero;
            OnDirectionChanged?.Invoke(Direction); // 🔔 Panggil event
        }
        else
        {
            NextDirection = newDirection;
        }
    }

    public bool Occupied(Vector2 direction)
    {
        if (direction == Vector2.zero) return false;
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            Vector2.one * boxCastSize,
            0f,
            direction,
            boxCastDistance,
            obstacleLayer);
        return hit.collider != null;
    }
}