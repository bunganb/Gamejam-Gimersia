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

    [Header("Collision Settings")]
    [SerializeField] private float nodeCheckRadius = 0.3f;
    [SerializeField] private float castSize = 0.4f;
    [SerializeField] private float castDistance = 0.5f;

    public Rigidbody2D Rb { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 NextDirection { get; private set; }
    public Vector3 StartingPosition { get; private set; }
    public bool IsAtNode { get; private set; }

    public UnityEvent<Vector2> OnDirectionChanged;
    public UnityEvent OnEnteredNode;
    public UnityEvent OnExitedNode;

    private Collider2D _col;
    private Node _currentNode;
    private bool _hasLoggedFirstMove = false;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

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
        NextDirection = Vector2.zero;
        _hasLoggedFirstMove = false;
        transform.position = StartingPosition;
        Rb.linearVelocity = Vector2.zero;
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
        UpdateNodeStatus();

        if (!freeMovement && IsAtNode && NextDirection != Vector2.zero)
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
        if (Direction == Vector2.zero)
        {
            Rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 before = Rb.position;
        Vector2 targetVel = Direction * (speed * speedMultiplier);
        Rb.linearVelocity = targetVel;

        if (!_hasLoggedFirstMove)
        {
            _hasLoggedFirstMove = true;
            Debug.Log($"[Movement:{name}] 🏃 Mulai bergerak — " +
                      $"velocity: {targetVel}, pos: {transform.position}");
        }

        if (Time.frameCount % 60 == 0 && targetVel.magnitude > 0.01f)
        {
            Vector2 after = Rb.position;
            if (Vector2.Distance(before, after) < 0.001f)
            {
                // ✅ Cek SEMUA layer tanpa filter — temukan apa yang sebenarnya memblokir
                RaycastHit2D[] allHits = Physics2D.BoxCastAll(
                    transform.position, Vector2.one * castSize,
                    0f, Direction, castDistance);

                if (allHits.Length > 0)
                {
                    string blockLog = "";
                    foreach (var h in allHits)
                    {
                        bool inObstacleLayer = (obstacleLayer.value & (1 << h.collider.gameObject.layer)) != 0;
                        blockLog += $"\n  → '{h.collider.name}' " +
                                    $"layer: '{LayerMask.LayerToName(h.collider.gameObject.layer)}' " +
                                    $"(index:{h.collider.gameObject.layer}) " +
                                    $"inObstacleLayer:{inObstacleLayer}";
                    }

                    Debug.LogError($"[Movement:{name}] 🔴 STUCK! Pemblokir fisik terdeteksi:{blockLog}\n" +
                                   $"→ Layer yang TIDAK ada di obstacleLayer adalah penyebabnya!\n" +
                                   $"→ Tambahkan layer tersebut ke obstacleLayer di Inspector,\n" +
                                   $"  ATAU matikan collision di Physics 2D Layer Matrix.");
                }
                else
                {
                    // Tidak ada yang terdeteksi BoxCast tapi tetap stuck
                    // → Cek dengan OverlapCircle radius lebih besar
                    Collider2D[] nearby = Physics2D.OverlapCircleAll(
                        transform.position + (Vector3)(Direction * 0.3f), 0.3f);

                    string nearbyLog = "";
                    foreach (var c in nearby)
                    {
                        if (c.gameObject == gameObject) continue; // skip self
                        nearbyLog += $"\n  → '{c.name}' " +
                                     $"layer: '{LayerMask.LayerToName(c.gameObject.layer)}' " +
                                     $"isTrigger: {c.isTrigger}";
                    }

                    if (nearbyLog != "")
                        Debug.LogError($"[Movement:{name}] 🔴 STUCK! Collider dekat di arah {Direction}:{nearbyLog}\n" +
                                       $"→ Matikan collision layer tersebut dengan layer Sheep/Wolf\n" +
                                       $"  di Edit → Project Settings → Physics 2D → Layer Collision Matrix");
                    else
                        Debug.LogError($"[Movement:{name}] 🔴 STUCK! Tidak ada collider terdeteksi.\n" +
                                       $"→ Kemungkinan Tilemap Composite Collider memblokir.\n" +
                                       $"→ Cek layer Tilemap dan tambahkan ke obstacleLayer.");
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    public void SetDirection(Vector2 newDirection, bool forced = false)
    {
        if (newDirection == Vector2.zero) return;

        if (freeMovement)
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
        if (direction == Vector2.zero) return false;

        // ✅ Cek dengan obstacleLayer yang sudah di-set
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            Vector2.one * castSize,
            0f,
            direction,
            castDistance,
            obstacleLayer
        );

        // ✅ DEBUG SEMENTARA: Cek semua layer tanpa filter
        RaycastHit2D hitAll = Physics2D.BoxCast(
            transform.position,
            Vector2.one * castSize,
            0f,
            direction,
            castDistance
        );

        // Jika hitAll ada tapi hit tidak ada → layer salah!
        if (hitAll.collider != null && hit.collider == null)
        {
            Debug.LogError($"[Movement:{name}] ⚠️ LAYER MISMATCH arah {direction}!\n" +
                           $"BoxCast mengenai '{hitAll.collider.name}' " +
                           $"di layer '{LayerMask.LayerToName(hitAll.collider.gameObject.layer)}' " +
                           $"(layer index: {hitAll.collider.gameObject.layer})\n" +
                           $"Tapi obstacleLayer ({obstacleLayer.value}) tidak mendeteksinya!\n" +
                           $"→ Tambahkan layer '{LayerMask.LayerToName(hitAll.collider.gameObject.layer)}' " +
                           $"ke obstacleLayer di Inspector Movement!");
        }

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