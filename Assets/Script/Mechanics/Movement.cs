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

    [Header("Collision")]
    [SerializeField] private float nodeCheckRadius = 0.3f;

    public Rigidbody2D Rb { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 NextDirection { get; private set; }
    public Vector3 StartingPosition { get; private set; }
    public bool IsAtNode { get; private set; }

    public UnityEvent<Vector2> OnDirectionChanged; // Event baru

    public UnityEvent OnEnteredNode;
    public UnityEvent OnExitedNode;

    private Collider2D characterCollider;
    private Node currentNode;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        characterCollider = GetComponent<Collider2D>();
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
        Rb.linearVelocity = Vector2.zero; // gunakan velocity untuk kompatibilitas
        enabled = true;
        UpdateNodeStatus();
    }

    private void FixedUpdate()
    {
        if (Direction == Vector2.zero)
        {
            Rb.linearVelocity = Vector2.zero;
            return;
        }
        Rb.linearVelocity = Direction * (speed * speedMultiplier);
    }

    private void Update()
    {
        UpdateNodeStatus();

        if (IsAtNode && NextDirection != Vector2.zero)
        {
            // Coba terapkan next direction, jika gagal, hapus buffer
            bool success = TrySetDirection(NextDirection);
            if (!success)
                NextDirection = Vector2.zero;
        }
    }

    private void UpdateNodeStatus()
    {
        bool wasAtNode = IsAtNode;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, nodeCheckRadius, nodeLayer);
        IsAtNode = hits.Length > 0;

        if (IsAtNode != wasAtNode)
        {
            if (IsAtNode)
            {
                currentNode = hits[0].GetComponent<Node>();
                OnEnteredNode?.Invoke();
            }
            else
            {
                currentNode = null;
                OnExitedNode?.Invoke();
            }
        }
    }

    public void SetDirection(Vector2 newDirection, bool forced = false)
    {
        if (forced || (IsAtNode && !Occupied(newDirection)))
        {
            Direction = newDirection;
            NextDirection = Vector2.zero;
            OnDirectionChanged?.Invoke(Direction); // panggil event
        }
        else
        {
            NextDirection = newDirection;
        }
    }

    private bool TrySetDirection(Vector2 newDirection)
    {
        if (IsAtNode && !Occupied(newDirection))
        {
            Direction = newDirection;
            OnDirectionChanged?.Invoke(Direction);
            return true;
        }
        return false;
    }

    public bool Occupied(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;
        float radius = characterCollider.bounds.extents.x;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, dir, 0.6f, obstacleLayer);
        return hit.collider != null;
    }

    public Node GetCurrentNode() => currentNode;
}