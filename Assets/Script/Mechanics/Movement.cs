using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8f;
    public float speedMultiplier = 1f;
    public Vector2 initialDirection;
    public LayerMask obstacleLayer;

    [Header("Cooling Settings")]
    [Tooltip("Ukuran box untuk deteksi rintangan (lebar/Tinggi)")]
    [SerializeField] private float boxCastSize = 0.75f;
    [Tooltip("Jarak deteksi rintangan ke depan")]
    [SerializeField] private float boxCastDistance = 1.5f;

    public Rigidbody2D rb { get; private set; }
    public Vector2 direction { get; private set; }
    public Vector2 nextDirection { get; private set; }
    public Vector3 startingPosition { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startingPosition = transform.position;
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        speedMultiplier = 1f;
        direction = initialDirection;
        nextDirection = Vector2.zero;
        transform.position = startingPosition;
        enabled = true;
    }

    private void Update()
    {
        // jika ada arah yang diinginkan, coba set arah tersebut
        if (nextDirection != Vector2.zero)
        {
            SetDirection(nextDirection);
        }
    }

    private void FixedUpdate()
    {
        Vector2 translation = direction * speed * speedMultiplier * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + translation);
    }

    public void SetDirection(Vector2 direction, bool forced = false)
    {
        if (forced || !Occupied(direction))
        {
            this.direction = direction;
            nextDirection = Vector2.zero;
        }
        else
        {
            nextDirection = direction;
        }
    }

    public bool Occupied(Vector2 direction)
    {
        if(direction == Vector2.zero) return false;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, Vector2.one * boxCastSize, 0f, direction, boxCastDistance, obstacleLayer);
        return hit.collider != null;
    }
}