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
    public bool showDebugLogs = false;

    private Sheep sheep;
    private Movement movement;
    private enum State { Eating, Panicking }
    private State currentState;
    private float panicTimer;
    private Vector2 currentDirection;
    private Transform targetFood;

    private int foodLayerIndex;
    private int wolfLayerIndex;

    private List<Transform> activeFoods = new List<Transform>();
    private Queue<Node> recentNodes = new Queue<Node>();
    private const int MAX_RECENT_NODES = 3;

    private void Awake()
    {
        sheep = GetComponent<Sheep>();
        movement = GetComponent<Movement>();
        currentState = State.Eating;

        foodLayerIndex = LayerMask.NameToLayer("Food");
        wolfLayerIndex = LayerMask.NameToLayer("Wolf");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFoodEaten += OnFoodEaten;
            UpdateActiveFoodsCache();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFoodEaten -= OnFoodEaten;
    }

    private void Start()
    {
        Wolf wolf = FindFirstObjectByType<Wolf>();
        if (wolf != null)
            wolfTransform = wolf.transform;
        else
            Debug.LogWarning("Wolf not found in scene!");

        movement.speedMultiplier = normalSpeed;

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        currentDirection = directions[Random.Range(0, directions.Length)];
        movement.SetDirection(currentDirection);

        FindNearestFood();
    }

    private void Update()
    {
        CheckForWolf();

        switch (currentState)
        {
            case State.Eating:
                EatingBehavior();
                break;
            case State.Panicking:
                PanicBehavior();
                break;
        }
    }

    private void OnFoodEaten(Transform food)
    {
        activeFoods.Remove(food);
        if (targetFood == food) targetFood = null;
    }

    private void UpdateActiveFoodsCache()
    {
        activeFoods.Clear();
        if (GameManager.Instance != null && GameManager.Instance.foodsParent != null)
        {
            foreach (Transform food in GameManager.Instance.foodsParent)
            {
                if (food.gameObject.activeSelf && food.gameObject.layer == foodLayerIndex)
                    activeFoods.Add(food);
            }
        }
    }

    private void CheckForWolf()
    {
        if (wolfTransform == null) return;

        float distanceToWolf = Vector2.Distance(transform.position, wolfTransform.position);

        if (distanceToWolf <= detectionRange)
        {
            if (currentState != State.Panicking)
                EnterPanicState();
        }
        else
        {
            if (currentState == State.Panicking)
            {
                panicTimer -= Time.deltaTime;
                if (panicTimer <= 0f)
                    EnterEatingState();
            }
        }
    }

    private void EnterPanicState()
    {
        currentState = State.Panicking;
        panicTimer = panicDuration;
        movement.speedMultiplier = panicSpeed;
        AudioManager.Instance?.PlaySFX("Sheep Screaming");

        recentNodes.Clear();

        if (showDebugLogs) Debug.Log($"{name} PANICKING!");

        if (wolfTransform != null)
        {
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Vector2 bestDir = GetBestCardinalDirection(fleeDir);
            if (!movement.Occupied(bestDir))
                movement.SetDirection(bestDir);
            else
                bestDir = GetAlternativeFleeDirection(fleeDir);
            currentDirection = bestDir;
        }
    }

    private Vector2 GetAlternativeFleeDirection(Vector2 fleeDir)
    {
        Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        float bestScore = float.MinValue;
        Vector2 best = currentDirection;

        foreach (Vector2 dir in directions)
        {
            if (!movement.Occupied(dir))
            {
                float score = Vector2.Dot(dir, fleeDir);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = dir;
                }
            }
        }
        return best;
    }

    private void EnterEatingState()
    {
        currentState = State.Eating;
        movement.speedMultiplier = normalSpeed;
        targetFood = null;
        if (showDebugLogs) Debug.Log($"{name} back to EATING");
    }

    private void EatingBehavior()
    {
        if (targetFood == null || !targetFood.gameObject.activeSelf)
            targetFood = FindNearestFood();

        if (movement.IsAtNode)
        {
            Node currentNode = movement.GetCurrentNode();
            if (currentNode != null && currentNode.availableDirections.Count > 0)
            {
                if (recentNodes.Count > 0 && recentNodes.Peek() == currentNode)
                    return;

                Vector2 newDirection = ChooseBestDirectionToFood(currentNode);
                if (newDirection != Vector2.zero)
                {
                    movement.SetDirection(newDirection);
                    currentDirection = newDirection;
                    AddRecentNode(currentNode);
                    if (showDebugLogs) Debug.Log($"{name} chose {newDirection}");
                }
            }
        }
    }

    private void PanicBehavior()
    {
        if (!movement.IsAtNode) return;
        Node currentNode = movement.GetCurrentNode();
        if (currentNode == null || currentNode.availableDirections.Count == 0) return;

        if (recentNodes.Count > 0 && recentNodes.Peek() == currentNode) return;

        if (wolfTransform != null)
        {
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Vector2 newDirection = ChooseFleeDirection(currentNode, fleeDir);
            if (newDirection != Vector2.zero)
            {
                movement.SetDirection(newDirection);
                currentDirection = newDirection;
                AddRecentNode(currentNode);
            }
        }
    }

    private void AddRecentNode(Node node)
    {
        recentNodes.Enqueue(node);
        while (recentNodes.Count > MAX_RECENT_NODES)
            recentNodes.Dequeue();
    }

    private bool IsRecentNode(Node node)
    {
        foreach (Node n in recentNodes)
            if (n == node) return true;
        return false;
    }

    private Vector2 ChooseBestDirectionToFood(Node node)
    {
        List<Vector2> forwardDirections = GetForwardDirections(node);
        if (forwardDirections.Count == 0)
        {
            recentNodes.Clear();
            return node.availableDirections[Random.Range(0, node.availableDirections.Count)];
        }

        Vector2 bestDir = forwardDirections[0];
        float bestScore = float.MinValue;

        foreach (Vector2 dir in forwardDirections)
        {
            float score = 0f;

            Node targetNode = GetNodeInDirection(node, dir);
            if (targetNode != null && IsRecentNode(targetNode))
                score -= 100f;

            score += FoodBonus(dir, node.transform.position) * 10f;

            if (targetFood != null)
            {
                Vector2 toTarget = ((Vector2)targetFood.position - (Vector2)node.transform.position).normalized;
                score += Vector2.Dot(dir, toTarget) * 5f;
            }

            score -= WolfPenalty(dir) * 0.5f;

            if (dir == currentDirection)
                score += 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private Node GetNodeInDirection(Node fromNode, Vector2 dir)
    {
        float distance = 2f; // jarak antar node (sesuaikan dengan level)
        Vector3 targetPos = fromNode.transform.position + (Vector3)(dir * distance);
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 0.5f);
        return hits.Length > 0 ? hits[0].GetComponent<Node>() : null;
    }

    private List<Vector2> GetForwardDirections(Node node)
    {
        List<Vector2> forward = new List<Vector2>();
        Vector2 backward = -currentDirection;
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
            Node targetNode = GetNodeInDirection(node, dir);
            if (targetNode != null && IsRecentNode(targetNode))
                score -= 100f;
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    private float FoodBonus(Vector2 direction, Vector3 fromPos)
    {
        float bonus = 0f;
        foreach (Transform food in activeFoods)
        {
            Vector2 toFood = ((Vector2)food.position - (Vector2)fromPos).normalized;
            float alignment = Vector2.Dot(direction, toFood);
            if (alignment > 0.5f)
            {
                float distance = Vector2.Distance(fromPos, food.position);
                if (distance < foodSearchRadius)
                {
                    float distFactor = 1f - (distance / foodSearchRadius);
                    bonus += alignment * distFactor;
                }
            }
        }
        return bonus;
    }

    private float WolfPenalty(Vector2 direction)
    {
        if (wolfTransform == null) return 0f;
        Vector2 toWolf = ((Vector2)wolfTransform.position - (Vector2)transform.position).normalized;
        float dot = Vector2.Dot(direction, toWolf);
        return dot > 0 ? dot : 0f;
    }

    private Vector2 GetBestCardinalDirection(Vector2 target)
    {
        Vector2[] cardinals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        Vector2 best = cardinals[0];
        float bestDot = float.MinValue;
        foreach (Vector2 dir in cardinals)
        {
            float dot = Vector2.Dot(dir, target);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = dir;
            }
        }
        return best;
    }

    private Transform FindNearestFood()
    {
        if (activeFoods.Count == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (Transform food in activeFoods)
        {
            float dist = Vector2.Distance(transform.position, food.position);
            if (dist < minDist && dist < foodSearchRadius)
            {
                minDist = dist;
                nearest = food;
            }
        }
        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (targetFood != null && currentState == State.Eating)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetFood.position);
        }
        if (currentState == State.Panicking && wolfTransform != null)
        {
            Gizmos.color = Color.red;
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Gizmos.DrawRay(transform.position, fleeDir * 2f);
        }
    }
}