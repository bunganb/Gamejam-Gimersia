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
    [SerializeField] private float boxCastSize = 0.75f;
    [SerializeField] private float boxCastDistance = 1.5f;

    public UnityEvent<Vector2> OnDirectionChanged;

    public Rigidbody2D Rb { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 NextDirection { get; private set; }
    public Vector3 StartingPosition { get; private set; }
    public bool IsAtNode { get; private set; }

    [Header("Node Settings")]
    public LayerMask nodeLayer;
    [SerializeField] private float nodeCheckRadius = 0.3f;

    private Node _currentNode;

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
        UpdateNodeStatus();
    }

    private void Update()
    {
        UpdateNodeStatus();

        // Jika ada arah yang diinginkan dan kita di node, coba terapkan
        if (IsAtNode && NextDirection != Vector2.zero)
        {
            SetDirection(NextDirection, forced: false);
        }
    }

    private void UpdateNodeStatus()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, nodeCheckRadius, nodeLayer);
        IsAtNode = hits.Length > 0;
        _currentNode = IsAtNode ? hits[0].GetComponent<Node>() : null;
    }

    private void FixedUpdate()
    {
        Vector2 translation = Direction * speed * speedMultiplier * Time.fixedDeltaTime;
        Rb.MovePosition(Rb.position + translation);
    }

    public void SetDirection(Vector2 newDirection, bool forced = false)
    {
        if (forced)
        {
            Direction = newDirection;
            NextDirection = Vector2.zero;
            OnDirectionChanged?.Invoke(Direction);
            return;
        }

        // Hanya boleh mengubah arah jika di node
        if (IsAtNode)
        {
            if (!Occupied(newDirection))
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
            // Simpan untuk dicoba nanti
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

    public Node GetCurrentNode() => _currentNode;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, nodeCheckRadius);
        if (Direction != Vector2.zero)
        {
            Gizmos.color = Occupied(Direction) ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, Direction * (boxCastDistance + 0.2f));
            Gizmos.color = Color.yellow;
            Vector3 castEnd = transform.position + (Vector3)(Direction * boxCastDistance);
            Gizmos.DrawWireCube(castEnd, Vector3.one * boxCastSize);
        }
    }
}