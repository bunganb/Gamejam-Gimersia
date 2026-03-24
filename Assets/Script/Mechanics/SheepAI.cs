using System.Collections.Generic;
using UnityEngine;

public enum SheepState
{
    Eating,
    Panicking
}

[RequireComponent(typeof(Sheep))]
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
    public LayerMask nodeLayer;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Sheep _sheep;
    private Movement _movement;
    private SheepState _currentState;
    private float _panicTimer;
    private Vector2 _currentDirection;
    private Transform _targetFood;

    private int _foodLayer;
    private int _wolfLayer;

    private float _foodSearchTimer;
    private const float FoodSearchInterval = 0.5f;

    private Vector3 _lastNodePosition;
    private const float NewNodeThreshold = 0.8f;
    private bool _isFirstDecision = true;

    private Queue<Vector3> _recentNodes = new Queue<Vector3>();
    private const int MaxRecentNodes = 3;

    private void Awake()
    {
        _sheep = GetComponent<Sheep>();
        _movement = GetComponent<Movement>();
        _currentState = SheepState.Eating;

        _foodLayer = LayerMask.NameToLayer("Food");
        _wolfLayer = LayerMask.NameToLayer("Wolf");
    }

    private void Start()
    {
        // Cari wolf berdasarkan layer
        if (wolfTransform == null)
        {
            // Gunakan FindObjectsByType untuk performa (Unity 2022+)
            // Jika tidak tersedia, fallback ke FindObjectsOfType
            Wolf[] wolves = FindObjectsByType<Wolf>(FindObjectsSortMode.None);
            if (wolves.Length > 0) wolfTransform = wolves[0].transform;
        }

        _movement.speedMultiplier = normalSpeed;

        // Inisialisasi arah acak
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        _currentDirection = directions[Random.Range(0, directions.Length)];
        _movement.SetDirection(_currentDirection);

        if (showDebugLogs)
            Debug.Log($"{name} Initial direction: {_currentDirection}");

        _lastNodePosition = transform.position + Vector3.one * 999f; // offset besar
        _targetFood = FindNearestFood();

        if (showDebugLogs && _targetFood != null)
            Debug.Log($"{name} initial target: {_targetFood.name}");
    }

    private void Update()
    {
        CheckForWolf();

        switch (_currentState)
        {
            case SheepState.Eating:
                EatingBehavior();
                break;
            case SheepState.Panicking:
                PanicBehavior();
                break;
        }
    }

    private void CheckForWolf()
    {
        if (wolfTransform == null) return;

        float dist = Vector2.Distance(transform.position, wolfTransform.position);
        if (dist <= detectionRange)
        {
            if (_currentState != SheepState.Panicking)
                EnterPanicState();
        }
        else
        {
            if (_currentState == SheepState.Panicking)
            {
                _panicTimer -= Time.deltaTime;
                if (_panicTimer <= 0f)
                    EnterEatingState();
            }
        }
    }

    private void EnterPanicState()
    {
        _currentState = SheepState.Panicking;
        _panicTimer = panicDuration;
        _movement.speedMultiplier = panicSpeed;
        AudioManager.Instance.PlaySFX("Sheep Screaming");
        _recentNodes.Clear();

        if (showDebugLogs)
            Debug.Log($"{name} is PANICKING!");

        if (wolfTransform != null)
        {
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Vector2 bestDir = GetBestCardinalDirection(fleeDir);

            if (!_movement.Occupied(bestDir))
            {
                _movement.SetDirection(bestDir);
                _currentDirection = bestDir;
            }
            else
            {
                // Cari alternatif yang tidak terhalang
                Vector2[] alts = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
                float bestScore = float.MinValue;
                Vector2 bestAlt = _currentDirection;
                foreach (Vector2 dir in alts)
                {
                    if (!_movement.Occupied(dir))
                    {
                        float score = Vector2.Dot(dir, fleeDir);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAlt = dir;
                        }
                    }
                }
                _movement.SetDirection(bestAlt);
                _currentDirection = bestAlt;
            }
        }
    }

    private void EnterEatingState()
    {
        _currentState = SheepState.Eating;
        _movement.speedMultiplier = normalSpeed;
        _targetFood = null;
        if (showDebugLogs)
            Debug.Log($"{name} is back to EATING");
    }

    private void EatingBehavior()
    {
        // Update target makanan secara periodik
        _foodSearchTimer -= Time.deltaTime;
        if (_foodSearchTimer <= 0f || _targetFood == null || !_targetFood.gameObject.activeSelf)
        {
            _targetFood = FindNearestFood();
            _foodSearchTimer = FoodSearchInterval;
        }

        // Hanya ambil keputusan di node
        if (!IsAtNode()) return;

        float distFromLastNode = Vector3.Distance(transform.position, _lastNodePosition);
        if (!_isFirstDecision && distFromLastNode < NewNodeThreshold) return;

        Node node = GetCurrentNode();
        if (node == null || node.availableDirections.Count == 0) return;

        Vector2 newDir = ChooseBestDirectionToFood(node);
        if (newDir != Vector2.zero)
        {
            _movement.SetDirection(newDir);
            _currentDirection = newDir;
            AddRecentNode(transform.position);
            _lastNodePosition = transform.position;
            _isFirstDecision = false;
            if (showDebugLogs)
                Debug.Log($"{name} chose: {newDir}");
        }
    }

    private void PanicBehavior()
    {
        if (!IsAtNode() || wolfTransform == null) return;

        float distFromLastNode = Vector3.Distance(transform.position, _lastNodePosition);
        if (distFromLastNode < NewNodeThreshold) return;

        Node node = GetCurrentNode();
        if (node == null || node.availableDirections.Count == 0) return;

        Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
        Vector2 newDir = ChooseFleeDirection(node, fleeDir);
        if (newDir != Vector2.zero)
        {
            _movement.SetDirection(newDir);
            _currentDirection = newDir;
            _lastNodePosition = transform.position;
            if (showDebugLogs)
                Debug.Log($"{name} fleeing at node: {newDir}");
        }
    }

    private void AddRecentNode(Vector3 pos)
    {
        _recentNodes.Enqueue(pos);
        while (_recentNodes.Count > MaxRecentNodes)
            _recentNodes.Dequeue();
    }

    private bool IsRecentNode(Vector3 pos)
    {
        foreach (Vector3 p in _recentNodes)
            if (Vector3.Distance(pos, p) < 0.5f) return true;
        return false;
    }

    private Vector2 ChooseBestDirectionToFood(Node node)
    {
        var forwardDirs = GetForwardDirections(node);
        if (forwardDirs.Count == 0)
        {
            if (showDebugLogs) Debug.Log($"{name} DEAD END - turning around");
            _recentNodes.Clear();
            return node.availableDirections[Random.Range(0, node.availableDirections.Count)];
        }

        Vector2 bestDir = forwardDirs[0];
        float bestScore = float.MinValue;

        foreach (Vector2 dir in forwardDirs)
        {
            float score = 0f;
            Vector3 potentialPos = node.transform.position + (Vector3)(dir * 2f);

            if (IsRecentNode(potentialPos))
                score -= 100f;

            score += FoodBonus(dir, node.transform.position) * 10f;

            if (_targetFood != null)
            {
                Vector2 toTarget = ((Vector2)_targetFood.position - (Vector2)node.transform.position).normalized;
                score += Vector2.Dot(dir, toTarget) * 5f;
            }

            score -= WolfPenalty(dir) * 0.5f;

            if (dir == _currentDirection)
                score += 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private List<Vector2> GetForwardDirections(Node node)
    {
        List<Vector2> forward = new List<Vector2>();
        Vector2 backward = -_currentDirection;
        foreach (Vector2 dir in node.availableDirections)
            if (dir != backward)
                forward.Add(dir);
        return forward;
    }

    private Vector2 ChooseFleeDirection(Node node, Vector2 fleeDir)
    {
        Vector2 bestDir = node.availableDirections[0];
        float bestScore = float.MinValue;
        foreach (Vector2 dir in node.availableDirections)
        {
            float score = Vector2.Dot(dir, fleeDir);
            score -= WolfPenalty(dir) * 3.0f;
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private float FoodBonus(Vector2 dir, Vector3 fromPos)
    {
        if (GameManager.Instance == null || GameManager.Instance.foods == null)
            return 0f;

        float bonus = 0f;
        foreach (Transform food in GameManager.Instance.foods)
        {
            if (!food.gameObject.activeSelf) continue;

            Vector2 toFood = ((Vector2)food.position - (Vector2)fromPos).normalized;
            float alignment = Vector2.Dot(dir, toFood);
            if (alignment > 0.5f) // cone 60°
            {
                float dist = Vector2.Distance(fromPos, food.position);
                if (dist < foodSearchRadius)
                {
                    float distBonus = 1f - (dist / foodSearchRadius);
                    bonus += alignment * distBonus;
                }
            }
        }
        return bonus;
    }

    private float WolfPenalty(Vector2 dir)
    {
        if (wolfTransform == null) return 0f;
        Vector2 toWolf = ((Vector2)wolfTransform.position - (Vector2)transform.position).normalized;
        float dot = Vector2.Dot(dir, toWolf);
        return dot > 0 ? dot : 0f;
    }

    private Vector2 GetBestCardinalDirection(Vector2 target)
    {
        Vector2[] cardinals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        Vector2 best = cardinals[0];
        float bestDot = float.MinValue;
        foreach (Vector2 d in cardinals)
        {
            float dot = Vector2.Dot(d, target);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = d;
            }
        }
        return best;
    }

    private Transform FindNearestFood()
    {
        if (GameManager.Instance == null || GameManager.Instance.foods == null)
            return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (Transform food in GameManager.Instance.foods)
        {
            if (!food.gameObject.activeSelf) continue;
            if (food.gameObject.layer != _foodLayer) continue;

            float dist = Vector2.Distance(transform.position, food.position);
            if (dist < minDist && dist < foodSearchRadius)
            {
                minDist = dist;
                nearest = food;
            }
        }
        return nearest;
    }

    private bool IsAtNode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f, nodeLayer);
        return hits.Length > 0;
    }

    private Node GetCurrentNode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f, nodeLayer);
        if (hits.Length > 0)
            return hits[0].GetComponent<Node>();
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (_recentNodes != null)
        {
            foreach (Vector3 pos in _recentNodes)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }

        if (_targetFood != null && _currentState == SheepState.Eating)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _targetFood.position);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_targetFood.position, 0.5f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, _currentDirection * 1.5f);

        if (wolfTransform != null && _currentState == SheepState.Panicking)
        {
            Gizmos.color = Color.red;
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Gizmos.DrawRay(transform.position, fleeDir * 2f);
        }
    }
}