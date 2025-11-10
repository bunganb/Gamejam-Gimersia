using UnityEngine;
using System.Collections.Generic;

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
    
    private Sheep sheep;
    private Movement movement;
    private SheepState currentState;
    private float panicTimer;
    private Vector2 currentDirection;
    private Transform targetFood;
    
    private int foodLayerIndex;
    private int wolfLayerIndex;
    
    private float foodSearchTimer = 0f;
    private float foodSearchInterval = 0.5f;
    
    private Vector3 lastNodePosition;
    private const float NEW_NODE_THRESHOLD = 0.8f;
    private bool isFirstDecision = true;
    private Dictionary<Node, Vector2> lastChosenDirection = new Dictionary<Node, Vector2>();
    private const float HISTORY_PENALTY = 0.8f;

    
    private void Awake()
    {
        sheep = GetComponent<Sheep>();
        movement = GetComponent<Movement>();
        currentState = SheepState.Eating;
        
        foodLayerIndex = LayerMask.NameToLayer("Food");
        wolfLayerIndex = LayerMask.NameToLayer("Wolf");
    }
    
    private void Start()
    {
        // Auto-find wolf
        if (wolfTransform == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.layer == wolfLayerIndex)
                {
                    wolfTransform = obj.transform;
                    if (showDebugLogs)
                        Debug.Log($"{gameObject.name} found wolf: {obj.name}");
                    break;
                }
            }
        }
        
        movement.speedMultiplier = normalSpeed;
        
        // Initialize direction
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        currentDirection = directions[Random.Range(0, directions.Length)];
        movement.SetDirection(currentDirection);
        
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} Initial direction: {currentDirection}");
        
        // Set lastNodePosition far away
        lastNodePosition = transform.position + new Vector3(999f, 999f, 0f);
        
        // Find initial food
        targetFood = FindNearestFood();
        if (showDebugLogs && targetFood != null)
            Debug.Log($"{gameObject.name} initial target: {targetFood.name}");
    }
    
    private void Update()
    {
        CheckForWolf();
        
        switch (currentState)
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
        
        float distanceToWolf = Vector2.Distance(transform.position, wolfTransform.position);
        
        if (distanceToWolf <= detectionRange)
        {
            if (currentState != SheepState.Panicking)
            {
                EnterPanicState();
            }
        }
        else
        {
            if (currentState == SheepState.Panicking)
            {
                panicTimer -= Time.deltaTime;
                if (panicTimer <= 0f)
                {
                    EnterEatingState();
                }
            }
        }
    }
    
    private void EnterPanicState()
    {
        currentState = SheepState.Panicking;
        panicTimer = panicDuration;
        movement.speedMultiplier = panicSpeed;
        
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} is PANICKING!");
        
        // IMMEDIATELY change direction away from wolf!
        if (wolfTransform != null)
        {
            Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Vector2 bestFleeDir = GetBestCardinalDirection(fleeDirection);
            
            // Check if this direction is not blocked
            if (!movement.Occupied(bestFleeDir))
            {
                movement.SetDirection(bestFleeDir);
                currentDirection = bestFleeDir;
                
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} IMMEDIATELY fleeing: {bestFleeDir}");
            }
            else
            {
                // If best direction is blocked, try other directions
                Vector2[] alternatives = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
                float bestScore = float.MinValue;
                Vector2 bestAvailable = currentDirection;
                
                foreach (Vector2 dir in alternatives)
                {
                    if (!movement.Occupied(dir))
                    {
                        float score = Vector2.Dot(dir, fleeDirection);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAvailable = dir;
                        }
                    }
                }
                
                movement.SetDirection(bestAvailable);
                currentDirection = bestAvailable;
                
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} Fleeing (alternative): {bestAvailable}");
            }
        }
    }
    
    private void EnterEatingState()
    {
        currentState = SheepState.Eating;
        movement.speedMultiplier = normalSpeed;
        targetFood = null;
        
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} is back to EATING");
    }
    
    private void EatingBehavior()
    {
        // Update food search timer
        foodSearchTimer -= Time.deltaTime;
        
        if (foodSearchTimer <= 0f || targetFood == null || !targetFood.gameObject.activeSelf)
        {
            Transform newTarget = FindNearestFood();
            
            if (newTarget != targetFood)
            {
                targetFood = newTarget;
                if (showDebugLogs && targetFood != null)
                    Debug.Log($"{gameObject.name} new target: {targetFood.name}");
            }
            
            foodSearchTimer = foodSearchInterval;
        }
        
        // ONLY make decisions at nodes
        if (IsAtNode())
        {
            float distanceFromLastNode = Vector3.Distance(transform.position, lastNodePosition);
            
            if (isFirstDecision || distanceFromLastNode > NEW_NODE_THRESHOLD)
            {
                Node node = GetCurrentNode();
                if (node != null && node.availableDirections.Count > 0)
                {
                    Vector2 newDirection = Vector2.zero;
                    
                    if (targetFood != null)
                    {
                        newDirection = ChooseBestDirectionNoBacktrack(node, targetFood.position);
                    }
                    else
                    {
                        newDirection = ChooseRandomDirectionNoBacktrack(node);
                    }
                    
                    if (newDirection != Vector2.zero)
                    {
                        movement.SetDirection(newDirection);
                        currentDirection = newDirection;
                        lastNodePosition = transform.position;
                        isFirstDecision = false;
                        
                        if (showDebugLogs)
                            Debug.Log($"{gameObject.name} chose: {newDirection}");
                    }
                }
            }
        }
    }
    
    private void PanicBehavior()
    {
        // Continue adjusting direction at nodes
        if (IsAtNode() && wolfTransform != null)
        {
            float distanceFromLastNode = Vector3.Distance(transform.position, lastNodePosition);
            
            if (distanceFromLastNode > NEW_NODE_THRESHOLD)
            {
                Node node = GetCurrentNode();
                if (node != null && node.availableDirections.Count > 0)
                {
                    Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
                    Vector2 newDirection = ChooseFleeDirection(node, fleeDirection);
                    
                    if (newDirection != Vector2.zero)
                    {
                        movement.SetDirection(newDirection);
                        currentDirection = newDirection;
                        lastNodePosition = transform.position;
                        
                        if (showDebugLogs)
                            Debug.Log($"{gameObject.name} fleeing at node: {newDirection}");
                    }
                }
            }
        }
    }
    
    private Vector2 ChooseBestDirectionNoBacktrack(Node node, Vector3 targetPosition)
    {
        List<Vector2> forwardDirections = GetForwardDirections(node);

        if (forwardDirections.Count == 0)
            return node.availableDirections[Random.Range(0, node.availableDirections.Count)];

        Vector2 bestDirection = forwardDirections[0];
        float bestScore = float.MinValue;
        Vector2 toTarget = ((Vector2)targetPosition - (Vector2)transform.position).normalized;

        foreach (Vector2 direction in forwardDirections)
        {
            float score = Vector2.Dot(direction, toTarget);

            // History penalty
            if (lastChosenDirection.ContainsKey(node) && lastChosenDirection[node] == direction)
            {
                score -= HISTORY_PENALTY;
            }

            // Wolf penalty
            float penalty = WolfPenalty(direction) * 1.2f;
            score -= penalty;

            // Food bonus
            score += FoodBonus(direction, node); // tambah prioritas jalur menuju food

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }

        lastChosenDirection[node] = bestDirection;

        return bestDirection;
    }
    
    private Vector2 ChooseRandomDirectionNoBacktrack(Node node)
    {
        List<Vector2> forwardDirections = GetForwardDirections(node);
        
        if (forwardDirections.Count == 0)
        {
            return node.availableDirections[Random.Range(0, node.availableDirections.Count)];
        }
        
        return forwardDirections[Random.Range(0, forwardDirections.Count)];
    }
    
    private List<Vector2> GetForwardDirections(Node node)
    {
        List<Vector2> forwardDirections = new List<Vector2>();
        Vector2 backward = -currentDirection;
        
        foreach (Vector2 direction in node.availableDirections)
        {
            if (direction != backward)
            {
                forwardDirections.Add(direction);
            }
        }
        
        return forwardDirections;
    }
    
    private Vector2 ChooseFleeDirection(Node node, Vector2 fleeDirection)
    {
        Vector2 bestDirection = node.availableDirections[0];
        float bestScore = float.MinValue;

        foreach (Vector2 direction in node.availableDirections)
        {
            float score = Vector2.Dot(direction, fleeDirection);

            // Penalti besar jika arah sama seperti terakhir di node ini
            if (lastChosenDirection.ContainsKey(node) && lastChosenDirection[node] == direction)
                score -= HISTORY_PENALTY;

            // Penalti jika mengarah ke wolf
            float penalty = WolfPenalty(direction) * 2.0f;
            score -= penalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }

        lastChosenDirection[node] = bestDirection;

        return bestDirection;
    }
    
    private Vector2 GetBestCardinalDirection(Vector2 targetDirection)
    {
        // Get the cardinal direction (up/down/left/right) closest to target direction
        Vector2[] cardinals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        Vector2 best = cardinals[0];
        float bestDot = float.MinValue;
        
        foreach (Vector2 cardinal in cardinals)
        {
            float dot = Vector2.Dot(cardinal, targetDirection);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = cardinal;
            }
        }
        
        return best;
    }
    
    private Transform FindNearestFood()
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;
        
        if (GameManager.Instance != null && GameManager.Instance.foods != null)
        {
            foreach (Transform food in GameManager.Instance.foods)
            {
                if (food.gameObject.activeSelf && food.gameObject.layer == foodLayerIndex)
                {
                    float distance = Vector2.Distance(transform.position, food.position);
                    
                    if (distance < minDistance && distance < foodSearchRadius)
                    {
                        minDistance = distance;
                        nearest = food;
                    }
                }
            }
            
            return nearest;
        }
        
        // Fallback
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == foodLayerIndex && obj.activeSelf)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                
                if (distance < minDistance && distance < foodSearchRadius)
                {
                    minDistance = distance;
                    nearest = obj.transform;
                }
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
        {
            Node node = hits[0].GetComponent<Node>();
            return node;
        }
        return null;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (targetFood != null && currentState == SheepState.Eating)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetFood.position);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetFood.position, 0.5f);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, currentDirection * 1.5f);
        
        if (wolfTransform != null && currentState == SheepState.Panicking)
        {
            Gizmos.color = Color.red;
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Gizmos.DrawRay(transform.position, fleeDir * 2f);
        }
    }
    private float FoodBonus(Vector2 direction, Node node)
    {
        if (GameManager.Instance == null || GameManager.Instance.foods == null)
            return 0f;

        float bonus = 0f;
        foreach (Transform food in GameManager.Instance.foods)
        {
            if (!food.gameObject.activeSelf) continue;

            Vector2 toFood = ((Vector2)food.position - (Vector2)node.transform.position).normalized;
            float dot = Vector2.Dot(direction, toFood);

            // Bonus hanya jika arah mendekati food
            if (dot > 0f)
                bonus += dot; // bisa dikalikan faktor misal 1.5f untuk lebih agresif
        }

        return bonus;
    }

    private float WolfPenalty(Vector2 direction)
    {
        if (wolfTransform == null) 
            return 0f;

        Vector2 toWolf = ((Vector2)wolfTransform.position - (Vector2)transform.position).normalized;
    
        float dot = Vector2.Dot(direction, toWolf);

        if (dot <= 0f)
            return 0f;

        return dot; 
    }
}