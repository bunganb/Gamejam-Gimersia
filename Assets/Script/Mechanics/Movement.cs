// Movement.cs
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8f;
    public float speedMultiplier = 1f;
    public Vector2 initialDirection;
    public LayerMask obstacleLayer;
    public LayerMask nodeLayer;
    public bool freeMovement = false;

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
        Rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        StartingPosition = transform.position;

        // ✅ DEBUG: Info komponen saat Awake
        Debug.Log($"[Movement:{name}] Awake — " +
                  $"collider: {(_col != null ? _col.GetType().Name : "NULL")} " +
                  $"size: {GetColliderSize()}, " +
                  $"obstacleLayer: {obstacleLayer.value}, " +
                  $"nodeLayer: {nodeLayer.value}");
    }

    private void Start() => ResetState();

    public void ResetState()
    {
        speedMultiplier = 1f;
        direction = initialDirection;
        nextDirection = Vector2.zero;
        transform.position = startingPosition;
        enabled = true;

        Direction = initialDirection;
        UpdateNodeStatus();

        if (initialDirection == Vector2.zero)
        {
            Debug.LogWarning($"[Movement:{name}] ⚠️ initialDirection belum diset di Inspector!");
        }
        else
        {
            Debug.Log($"[Movement:{name}] ResetState — " +
                      $"Direction: {Direction}, speed: {speed}, " +
                      $"freeMovement: {freeMovement}, " +
                      $"bodyType: {Rb.bodyType}, " +
                      $"constraints: {Rb.constraints}, " +
                      $"colliderSize: {GetColliderSize()}, " +
                      $"castSize: {castSize}, castDist: {castDistance}");
        }
    }

    // ─────────────────────────────────────────────
    //  UPDATE & FIXED UPDATE
    // ─────────────────────────────────────────────

    private void Update()
    {
        // jika ada arah yang diinginkan, coba set arah tersebut
        if (nextDirection != Vector2.zero)
        {
            if (!Occupied(NextDirection))
            {
                Direction = NextDirection;
                NextDirection = Vector2.zero;
                OnDirectionChanged?.Invoke(Direction);
            }
            else
            {
                NextDirection = Vector2.zero;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 translation = direction * speed * speedMultiplier * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + translation);
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    public void SetDirection(Vector2 newDirection, bool forced = false)
    {
        if (forced || !Occupied(direction))
        {
            if (forced || !Occupied(newDirection))
            {
                Direction = newDirection;
                NextDirection = Vector2.zero;
                OnDirectionChanged?.Invoke(Direction);
            }
            else
            {
                NextDirection = newDirection;
            }
        }
        else
        {
            bool canApplyNow = forced
                            || Direction == Vector2.zero
                            || (IsAtNode && !Occupied(newDirection));

            if (canApplyNow)
            {
                Direction = newDirection;
                NextDirection = Vector2.zero;
                OnDirectionChanged?.Invoke(Direction);
            }
            else
            {
                NextDirection = newDirection;
            }
        }
    }

    public bool Occupied(Vector2 direction)
    {
        if(direction == Vector2.zero) return false;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, Vector2.one * boxCastSize, 0f, direction, boxCastDistance, obstacleLayer);
        return hit.collider != null;
    }

    public Node GetCurrentNode() => _currentNode;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    private void UpdateNodeStatus()
    {
        bool wasAtNode = IsAtNode;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, nodeCheckRadius, nodeLayer);
        IsAtNode = hits.Length > 0;

        if (IsAtNode == wasAtNode) return;

        if (IsAtNode)
        {
            _currentNode = hits[0].GetComponent<Node>();
            OnEnteredNode?.Invoke();
        }
        else
        {
            _currentNode = null;
            OnExitedNode?.Invoke();
        }
    }

    private string GetColliderSize()
    {
        if (_col == null) return "null";
        if (_col is BoxCollider2D box) return $"Box({box.size.x:F2}x{box.size.y:F2})";
        if (_col is CircleCollider2D circle) return $"Circle(r={circle.radius:F2})";
        return _col.GetType().Name;
    }

    private float GetColliderRadius()
    {
        if (_col is CircleCollider2D circle) return circle.radius;
        if (_col is BoxCollider2D box) return Mathf.Max(box.size.x, box.size.y) * 0.5f;
        return 0.3f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = freeMovement ? Color.blue : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, nodeCheckRadius);

        if (Direction != Vector2.zero)
        {
            Gizmos.color = Occupied(Direction) ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, Direction * (castDistance + 0.2f));
            // Visualisasi BoxCast
            Gizmos.color = Color.yellow;
            Vector3 castEnd = transform.position + (Vector3)(Direction * castDistance);
            Gizmos.DrawWireCube(castEnd, Vector3.one * castSize);
        }
    }
}