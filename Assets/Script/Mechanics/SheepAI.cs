// SheepAI.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Sheep), typeof(Movement))]
public class SheepAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 5f;
    public float normalSpeed = 1f;
    public float panicSpeed = 2f;
    public float panicDuration = 3f;
    public float foodSearchRadius = 15f;

    [Header("References")]
    public Transform wolfTransform;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Sheep _sheep;
    private Movement _movement;

    private enum State { Eating, Panicking }
    private State _state = State.Eating;

    private float _panicTimer;
    private Vector2 _currentDirection;
    private Transform _targetFood;
    private int _foodLayer;

    private List<Transform> _activeFoods = new();
    private Queue<Node> _recentNodes = new();
    private const int MAX_RECENT = 3;

    private float _debugTimer;
    private Vector3 _lastPos;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _sheep = GetComponent<Sheep>();
        _movement = GetComponent<Movement>();
        _foodLayer = LayerMask.NameToLayer("Food");

        Debug.Log($"[SheepAI:{name}] Awake — " +
                  $"movement: {(_movement != null ? "✅" : "❌ NULL")}, " +
                  $"sheep: {(_sheep != null ? "✅" : "❌ NULL")}");
    }

    private void Start()
    {
        Debug.Log($"[SheepAI:{name}] ===== START BEGIN =====");

        SnapToGrid();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFoodEaten += OnFoodEaten;
            UpdateFoodsCache();
            Debug.Log($"[SheepAI:{name}] ✅ Subscribed, foods: {_activeFoods.Count}");
        }
        else
        {
            Debug.LogError($"[SheepAI:{name}] ❌ GameManager null di Start!");
        }

        Wolf wolf = FindFirstObjectByType<Wolf>();
        if (wolf != null)
            wolfTransform = wolf.transform;
        else
            Debug.LogWarning($"[SheepAI:{name}] ⚠️ Wolf tidak ditemukan!");

        _movement.speedMultiplier = normalSpeed;

        // ✅ DEBUG: Cek detail Rigidbody sebelum mulai bergerak
        Debug.Log($"[SheepAI:{name}] Rigidbody detail — " +
                  $"bodyType: {_movement.Rb.bodyType}, " +
                  $"constraints: {_movement.Rb.constraints}, " +
                  $"isKinematic: {_movement.Rb.isKinematic}, " +
                  $"gravityScale: {_movement.Rb.gravityScale}, " +
                  $"mass: {_movement.Rb.mass}, " +
                  $"linearDamping: {_movement.Rb.linearDamping}");

        // ✅ DEBUG: Cek semua collider pada GameObject ini
        Collider2D[] allCols = GetComponents<Collider2D>();
        Debug.Log($"[SheepAI:{name}] Colliders ditemukan: {allCols.Length}");
        foreach (Collider2D c in allCols)
        {
            string sizeInfo = c is BoxCollider2D b
                ? $"Box({b.size.x:F2}x{b.size.y:F2})"
                : c is CircleCollider2D ci
                    ? $"Circle(r={ci.radius:F2})"
                    : c.GetType().Name;

            Debug.Log($"[SheepAI:{name}]   Collider: {sizeInfo}, " +
                      $"isTrigger: {c.isTrigger}, " +
                      $"enabled: {c.enabled}, " +
                      $"offset: {c.offset}");
        }

        // ✅ DEBUG: Cek overlap obstacle di posisi saat ini
        Collider2D[] obstacles = Physics2D.OverlapCircleAll(
            transform.position, 0.4f, _movement.obstacleLayer);
        if (obstacles.Length > 0)
        {
            Debug.LogError($"[SheepAI:{name}] ❌ OVERLAP DENGAN OBSTACLE DI POSISI SPAWN!");
            foreach (Collider2D o in obstacles)
                Debug.LogError($"[SheepAI:{name}]   → {o.name} " +
                               $"(layer: {LayerMask.LayerToName(o.gameObject.layer)}, " +
                               $"pos: {o.transform.position})");
        }
        else
        {
            Debug.Log($"[SheepAI:{name}] ✅ Tidak ada obstacle overlap di posisi spawn");
        }

        // Cek 4 arah
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        var freeDirs = new List<Vector2>();
        string dirLog = "";

        foreach (Vector2 d in dirs)
        {
            bool blocked = _movement.Occupied(d);
            dirLog += $"{DirectionName(d)}={blocked} | ";
            if (!blocked) freeDirs.Add(d);
        }

        Debug.Log($"[SheepAI:{name}] Arah tersedia: {freeDirs.Count}/4 — {dirLog}");

        if (freeDirs.Count == 0)
        {
            Debug.LogError($"[SheepAI:{name}] ❌ Semua arah blocked! " +
                           $"castSize={_movement.GetType().GetField("castSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_movement)} " +
                           $"Coba kecilkan Cast Size di Movement Inspector.");
            StartCoroutine(RetryDirectionAfterDelay());
            return;
        }

        _currentDirection = freeDirs[Random.Range(0, freeDirs.Count)];
        _movement.SetDirection(_currentDirection, forced: true);

        _targetFood = FindNearestFood();
        _lastPos = transform.position;

        Debug.Log($"[SheepAI:{name}] Start — " +
                  $"direction: {_currentDirection}, " +
                  $"foods: {_activeFoods.Count}, " +
                  $"target: {(_targetFood != null ? _targetFood.name : "none")}");

        Debug.Log($"[SheepAI:{name}] ===== START END =====");

        StartCoroutine(CheckVelocityAfterStart());
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFoodEaten -= OnFoodEaten;
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    private void Update()
    {
        CheckForWolf();

        switch (_state)
        {
            case State.Eating: EatingBehavior(); break;
            case State.Panicking: PanicBehavior(); break;
        }

        DebugPositionLog();
    }

    // ─────────────────────────────────────────────
    //  STATE MACHINE
    // ─────────────────────────────────────────────

    private void CheckForWolf()
    {
        if (wolfTransform == null) return;

        float dist = Vector2.Distance(transform.position, wolfTransform.position);

        if (dist <= detectionRange)
        {
            if (_state != State.Panicking) EnterPanic();
        }
        else if (_state == State.Panicking)
        {
            _panicTimer -= Time.deltaTime;
            if (_panicTimer <= 0f) EnterEating();
        }
    }

    private void EnterPanic()
    {
        _state = State.Panicking;
        _panicTimer = panicDuration;
        _movement.speedMultiplier = panicSpeed;
        _recentNodes.Clear();

        AudioManager.Instance?.PlaySFX("Sheep Screaming");

        if (wolfTransform != null)
        {
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Vector2 bestDir = GetBestCardinal(fleeDir);

            if (!_movement.Occupied(bestDir))
                _movement.SetDirection(bestDir, forced: true);
            else
                _movement.SetDirection(GetAlternativeFlee(fleeDir), forced: true);

            _currentDirection = _movement.Direction;
        }

        if (showDebugLogs) Debug.Log($"[SheepAI:{name}] 😱 PANIC!");
    }

    private void EnterEating()
    {
        _state = State.Eating;
        _movement.speedMultiplier = normalSpeed;
        _targetFood = null;
        if (showDebugLogs) Debug.Log($"[SheepAI:{name}] 🍃 EATING");
    }

    private void EatingBehavior()
    {
        if (_targetFood == null || !_targetFood.gameObject.activeSelf)
        {
            _targetFood = FindNearestFood();
            if (showDebugLogs && _targetFood != null)
                Debug.Log($"[SheepAI:{name}] 🎯 Target: {_targetFood.name}");
        }

        if (!_movement.IsAtNode) return;

        Node node = _movement.GetCurrentNode();
        if (node == null || node.availableDirections.Count == 0) return;

        Vector2 best = ChooseBestToFood(node);

        if (best == _movement.Direction)
        {
            AddRecentNode(node);
            return;
        }

        if (best != Vector2.zero)
        {
            _movement.SetDirection(best);
            _currentDirection = best;
            AddRecentNode(node);
            if (showDebugLogs) Debug.Log($"[SheepAI:{name}] 🧭 → {best}");
        }
    }

    private void PanicBehavior()
    {
        if (!_movement.IsAtNode) return;

        Node node = _movement.GetCurrentNode();
        if (node == null || node.availableDirections.Count == 0) return;
        if (wolfTransform == null) return;

        // Guard: skip node yang sudah diproses
        if (IsRecentNode(node)) return;

        Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
        Vector2 best = ChooseFlee(node, fleeDir);

        if (best != Vector2.zero)
        {
            _movement.SetDirection(best);
            _currentDirection = best;
            AddRecentNode(node);
            if (showDebugLogs) Debug.Log($"[SheepAI:{name}] 💨 → {best}");
        }
    }

    // ─────────────────────────────────────────────
    //  COROUTINES
    // ─────────────────────────────────────────────

    private IEnumerator CheckVelocityAfterStart()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        float mag = _movement.Rb.linearVelocity.magnitude;

        Debug.Log($"[SheepAI:{name}] 🔍 Velocity check setelah start — " +
                  $"velocity: {_movement.Rb.linearVelocity}, " +
                  $"magnitude: {mag:F2}, " +
                  $"Direction: {_movement.Direction}, " +
                  $"pos: {transform.position}");

        if (mag < 0.01f)
        {
            Debug.LogError($"[SheepAI:{name}] ❌ Velocity = 0 setelah physics update!\n" +
                           $"Cek kemungkinan:\n" +
                           $"1. Rigidbody constraints Freeze Position\n" +
                           $"2. Physics collider overlap wall\n" +
                           $"3. Movement.enabled = false\n" +
                           $"4. speed atau speedMultiplier = 0");
        }
        else
        {
            Debug.Log($"[SheepAI:{name}] ✅ Bergerak — velocity: {mag:F2}");
        }

        // ✅ Cek apakah posisi benar-benar berubah setelah 10 frame
        Vector3 posBefore = transform.position;
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Vector3 posAfter = transform.position;
        float moved = Vector3.Distance(posBefore, posAfter);

        Debug.Log($"[SheepAI:{name}] 🔍 Posisi setelah 10 FixedUpdate — " +
                  $"before: {posBefore}, after: {posAfter}, moved: {moved:F4}");

        if (moved < 0.01f && _movement.Direction != Vector2.zero)
        {
            Debug.LogError($"[SheepAI:{name}] ❌ POSISI TIDAK BERUBAH meski Direction aktif!\n" +
                           $"velocity: {_movement.Rb.linearVelocity}\n" +
                           $"Ini hampir pasti karena physics collider menabrak wall.\n" +
                           $"→ Kecilkan physics collider (non-trigger) di Inspector!\n" +
                           $"→ Coba size: 0.5x0.5 atau lebih kecil");

            // Cek overlap obstacle tepat di posisi sekarang
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(
                transform.position, 0.45f, _movement.obstacleLayer);

            if (overlaps.Length > 0)
            {
                Debug.LogError($"[SheepAI:{name}] ❌ KONFIRMASI: Collider overlap dengan {overlaps.Length} obstacle:");
                foreach (Collider2D o in overlaps)
                    Debug.LogError($"  → {o.name} di {o.transform.position}");
            }
            else
            {
                Debug.LogError($"[SheepAI:{name}] ❌ Tidak ada overlap terdeteksi tapi tetap stuck.\n" +
                               $"Cek: apakah ada script lain yang set velocity=0 setiap frame?\n" +
                               $"Cek: apakah Rigidbody constraints freeze position?");
            }
        }
    }

    private IEnumerator RetryDirectionAfterDelay()
    {
        Debug.Log($"[SheepAI:{name}] ⏳ Retry arah dalam 5 FixedUpdate...");

        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        var freeDirs = new List<Vector2>();
        string dirLog = "";

        foreach (Vector2 d in dirs)
        {
            bool blocked = _movement.Occupied(d);
            dirLog += $"{DirectionName(d)}={blocked} | ";
            if (!blocked) freeDirs.Add(d);
        }

        Debug.Log($"[SheepAI:{name}] Retry arah check — {dirLog}");

        if (freeDirs.Count == 0)
        {
            Debug.LogError($"[SheepAI:{name}] ❌ Masih blocked setelah delay!\n" +
                           $"Solusi: Pindahkan posisi spawn Sheep di Inspector ke tengah lorong.");
            yield break;
        }

        _currentDirection = freeDirs[Random.Range(0, freeDirs.Count)];
        _movement.SetDirection(_currentDirection, forced: true);
        _targetFood = FindNearestFood();

        Debug.Log($"[SheepAI:{name}] ✅ Retry berhasil — direction: {_currentDirection}");

        StartCoroutine(CheckVelocityAfterStart());
    }

    // ─────────────────────────────────────────────
    //  DIRECTION SCORING
    // ─────────────────────────────────────────────

    private Vector2 ChooseBestToFood(Node node)
    {
        var forward = GetForwardDirs(node);
        if (forward.Count == 0)
        {
            _recentNodes.Clear();
            return node.availableDirections[Random.Range(0, node.availableDirections.Count)];
        }

        Vector2 best = forward[0];
        float bestScore = float.MinValue;

        foreach (Vector2 dir in forward)
        {
            float score = 0f;

            Node next = GetNodeInDir(node, dir);
            if (next != null && IsRecentNode(next)) score -= 100f;

            score += FoodBonus(dir, node.transform.position) * 10f;

            if (_targetFood != null)
            {
                Vector2 toTarget = ((Vector2)_targetFood.position
                                 - (Vector2)node.transform.position).normalized;
                score += Vector2.Dot(dir, toTarget) * 5f;
            }

            score -= WolfPenalty(dir) * 8f;

            if (dir == _currentDirection) score += 0.3f;

            if (score > bestScore) { bestScore = score; best = dir; }
        }

        return best;
    }

    private Vector2 ChooseFlee(Node node, Vector2 fleeDir)
    {
        Vector2 best = node.availableDirections[0];
        float bestScore = float.MinValue;

        foreach (Vector2 dir in node.availableDirections)
        {
            float score = Vector2.Dot(dir, fleeDir);
            Node next = GetNodeInDir(node, dir);
            if (next != null && IsRecentNode(next)) score -= 100f;
            if (score > bestScore) { bestScore = score; best = dir; }
        }

        return best;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void SnapToGrid()
    {
        float snap = 0.5f;
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / snap) * snap;
        pos.y = Mathf.Round(pos.y / snap) * snap;

        Vector2 snapped = new Vector2(pos.x, pos.y);

        // Jika masih blocked, coba offset 0.5
        if (IsPositionBlocked(snapped))
        {
            Debug.LogWarning($"[SheepAI:{name}] ⚠️ Posisi snap awal blocked, coba offset...");

            Vector2[] offsets = {
                new Vector2( 0.5f,  0),
                new Vector2(-0.5f,  0),
                new Vector2( 0,  0.5f),
                new Vector2( 0, -0.5f),
                new Vector2( 0.5f,  0.5f),
                new Vector2(-0.5f,  0.5f),
                new Vector2( 0.5f, -0.5f),
                new Vector2(-0.5f, -0.5f),
            };

            bool found = false;
            foreach (Vector2 offset in offsets)
            {
                Vector2 candidate = snapped + offset;
                if (!IsPositionBlocked(candidate))
                {
                    Debug.Log($"[SheepAI:{name}] ✅ Offset ditemukan: {offset} → {candidate}");
                    snapped = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
                Debug.LogError($"[SheepAI:{name}] ❌ Semua offset blocked! " +
                               $"Pindahkan posisi spawn secara manual di Inspector.");
        }

        transform.position = new Vector3(snapped.x, snapped.y, 0);
        _movement.Rb.position = snapped;
        Physics2D.SyncTransforms();

        Debug.Log($"[SheepAI:{name}] 📌 Snap final → {snapped}");
    }

    private bool IsPositionBlocked(Vector2 pos)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, 0.2f, _movement.obstacleLayer);
        return hit != null;
    }

    private void UpdateFoodsCache()
    {
        _activeFoods.Clear();
        if (GameManager.Instance?.foodsParent == null)
        {
            Debug.LogError($"[SheepAI:{name}] ❌ foodsParent null!");
            return;
        }

        foreach (Transform food in GameManager.Instance.foodsParent)
            if (food.gameObject.activeSelf)
                _activeFoods.Add(food);

        Debug.Log($"[SheepAI:{name}] ✅ Foods cached: {_activeFoods.Count}");
    }

    private void OnFoodEaten(Transform food)
    {
        _activeFoods.Remove(food);
        if (_targetFood == food) _targetFood = null;
    }

    private Transform FindNearestFood()
    {
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (Transform food in _activeFoods)
        {
            float d = Vector2.Distance(transform.position, food.position);
            if (d < minDist && d < foodSearchRadius) { minDist = d; nearest = food; }
        }

        return nearest;
    }

    private float FoodBonus(Vector2 dir, Vector3 from)
    {
        float bonus = 0f;
        foreach (Transform food in _activeFoods)
        {
            Vector2 toFood = ((Vector2)food.position - (Vector2)from).normalized;
            float align = Vector2.Dot(dir, toFood);
            if (align > 0.5f)
            {
                float dist = Vector2.Distance(from, food.position);
                if (dist < foodSearchRadius)
                    bonus += align * (1f - dist / foodSearchRadius);
            }
        }
        return bonus;
    }

    private float WolfPenalty(Vector2 dir)
    {
        if (wolfTransform == null) return 0f;
        Vector2 toWolf = ((Vector2)wolfTransform.position
                       - (Vector2)transform.position).normalized;
        return Mathf.Max(0f, Vector2.Dot(dir, toWolf));
    }

    private List<Vector2> GetForwardDirs(Node node)
    {
        var result = new List<Vector2>();
        Vector2 backward = -_currentDirection;
        foreach (Vector2 d in node.availableDirections)
            if (d != backward) result.Add(d);
        return result;
    }

    private Node GetNodeInDir(Node from, Vector2 dir, float dist = 2f)
    {
        Vector3 pos = from.transform.position + (Vector3)(dir * dist);
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.5f);
        foreach (var h in hits)
        {
            Node n = h.GetComponent<Node>();
            if (n != null) return n;
        }
        return null;
    }

    private Vector2 GetBestCardinal(Vector2 target)
    {
        Vector2[] cardinals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        Vector2 best = cardinals[0];
        float bestDot = float.MinValue;
        foreach (Vector2 d in cardinals)
        {
            float dot = Vector2.Dot(d, target);
            if (dot > bestDot) { bestDot = dot; best = d; }
        }
        return best;
    }

    private Vector2 GetAlternativeFlee(Vector2 fleeDir)
    {
        Vector2[] dirs = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        Vector2 best = _currentDirection;
        float bestScore = float.MinValue;
        foreach (Vector2 d in dirs)
        {
            if (_movement.Occupied(d)) continue;
            float score = Vector2.Dot(d, fleeDir);
            if (score > bestScore) { bestScore = score; best = d; }
        }
        return best;
    }

    private void AddRecentNode(Node node)
    {
        _recentNodes.Enqueue(node);
        while (_recentNodes.Count > MAX_RECENT) _recentNodes.Dequeue();
    }

    private bool IsRecentNode(Node node)
    {
        foreach (Node n in _recentNodes)
            if (n == node) return true;
        return false;
    }

    private string DirectionName(Vector2 d)
    {
        if (d == Vector2.up) return "↑";
        if (d == Vector2.down) return "↓";
        if (d == Vector2.left) return "←";
        if (d == Vector2.right) return "→";
        return d.ToString();
    }

    private void DebugPositionLog()
    {
        _debugTimer += Time.deltaTime;
        if (_debugTimer < 2f) return;
        _debugTimer = 0f;

        Vector3 pos = transform.position;
        float moved = Vector3.Distance(pos, _lastPos);

        Debug.Log($"[SheepAI:{name}] 📍 Pos: {pos} | " +
                  $"Moved: {moved:F3} | " +
                  $"Velocity: {_movement.Rb.linearVelocity} | " +
                  $"Direction: {_movement.Direction} | " +
                  $"State: {_state} | " +
                  $"IsAtNode: {_movement.IsAtNode}");

        if (moved < 0.01f && _movement.Direction != Vector2.zero)
        {
            Debug.LogWarning($"[SheepAI:{name}] ⚠️ Posisi tidak berubah! " +
                             $"velocity: {_movement.Rb.linearVelocity.magnitude:F2} — " +
                             $"PHYSICS COLLIDER TERLALU BESAR, kecilkan di Inspector.");
        }

        _lastPos = pos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (_targetFood != null && _state == State.Eating)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _targetFood.position);
        }

        if (_state == State.Panicking && wolfTransform != null)
        {
            Gizmos.color = Color.red;
            Vector2 flee = ((Vector2)transform.position
                         - (Vector2)wolfTransform.position).normalized;
            Gizmos.DrawRay(transform.position, flee * 3f);
        }
    }
}