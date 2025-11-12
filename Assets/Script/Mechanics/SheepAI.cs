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
    
    // Track recent nodes to prevent immediate ping-pong
    private Queue<Vector3> recentNodes = new Queue<Vector3>();
    private const int MAX_RECENT_NODES = 3;
    
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
        AudioManager.Instance.PlaySFX("Sheep Screaming");
        // Clear recent nodes when panicking (can revisit when escaping)
        recentNodes.Clear();
        
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} is PANICKING!");
        
        // IMMEDIATELY change direction away from wolf
        if (wolfTransform != null)
        {
            Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)wolfTransform.position).normalized;
            Vector2 bestFleeDir = GetBestCardinalDirection(fleeDirection);
            
            if (!movement.Occupied(bestFleeDir))
            {
                movement.SetDirection(bestFleeDir);
                currentDirection = bestFleeDir;
                
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} IMMEDIATELY fleeing: {bestFleeDir}");
            }
            else
            {
                // Try alternative directions
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
                    Vector2 newDirection = ChooseBestDirectionToFood(node);
                    
                    if (newDirection != Vector2.zero)
                    {
                        movement.SetDirection(newDirection);
                        currentDirection = newDirection;
                        
                        // Add this node to recent nodes history
                        AddRecentNode(transform.position);
                        
                        lastNodePosition = transform.position;
                        isFirstDecision = false;
                        
                        if (showDebugLogs)
                            Debug.Log($"{gameObject.name} chose: {newDirection}, recent nodes: {recentNodes.Count}");
                    }
                }
            }
        }
    }
    
    private void PanicBehavior()
    {
        // When panicking, CAN go backward to escape!
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
    
    private void AddRecentNode(Vector3 nodePosition)
    {
        recentNodes.Enqueue(nodePosition);
        
        // Keep only last N nodes
        while (recentNodes.Count > MAX_RECENT_NODES)
        {
            recentNodes.Dequeue();
        }
    }
    
    private bool IsRecentNode(Vector3 position)
    {
        foreach (Vector3 recentNode in recentNodes)
        {
            if (Vector3.Distance(position, recentNode) < 0.5f)
            {
                return true;
            }
        }
        return false;
    }
    
    private Vector2 ChooseBestDirectionToFood(Node node)
    {
        // Get forward directions (NO BACKWARD in eating state)
        List<Vector2> forwardDirections = GetForwardDirections(node);
        
        // If no forward directions (dead end), allow any direction
        if (forwardDirections.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log($"{gameObject.name} DEAD END - turning around");
            
            // Clear recent nodes at dead end
            recentNodes.Clear();
            return node.availableDirections[Random.Range(0, node.availableDirections.Count)];
        }
        
        // Score each forward direction
        Vector2 bestDirection = forwardDirections[0];
        float bestScore = float.MinValue;
        
        foreach (Vector2 direction in forwardDirections)
        {
            float score = 0f;
            
            // Check where this direction would lead
            Vector3 potentialPosition = node.transform.position + (Vector3)(direction * 2f);
            
            // HUGE penalty for directions leading to recent nodes
            if (IsRecentNode(potentialPosition))
            {
                score -= 100f; // Massive penalty to prevent ping-pong
                if (showDebugLogs)
                    Debug.Log($"  {direction}: RECENT NODE PENALTY!");
            }
            
            // Factor 1: Food bonus (HIGHEST PRIORITY - look for food in this direction)
            score += FoodBonus(direction, node.transform.position) * 10.0f;
            
            // Factor 2: Target food alignment (if we have a target) - ALSO HIGH PRIORITY
            if (targetFood != null)
            {
                Vector2 toTarget = ((Vector2)targetFood.position - (Vector2)node.transform.position).normalized;
                float alignment = Vector2.Dot(direction, toTarget);
                score += alignment * 5.0f;
            }
            
            // Factor 3: Avoid wolf direction (small penalty)
            score -= WolfPenalty(direction) * 0.5f;
            
            // Factor 4: Small bonus for continuing straight (smooth movement)
            if (direction == currentDirection)
            {
                score += 0.3f;
            }
            
            if (showDebugLogs)
                Debug.Log($"  {direction}: score = {score:F2}");
            
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }
        
        return bestDirection;
    }
    
    private List<Vector2> GetForwardDirections(Node node)
    {
        // In eating state: BLOCK BACKWARD
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
        // In panic state: CAN go any direction (including backward)
        Vector2 bestDirection = node.availableDirections[0];
        float bestScore = float.MinValue;
        
        foreach (Vector2 direction in node.availableDirections)
        {
            // Score based on how well it points away from wolf
            float score = Vector2.Dot(direction, fleeDirection);
            
            // Big penalty if going toward wolf
            score -= WolfPenalty(direction) * 3.0f;
            
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }
        
        return bestDirection;
    }
    
    private float FoodBonus(Vector2 direction, Vector3 fromPosition)
    {
        if (GameManager.Instance == null || GameManager.Instance.foods == null)
            return 0f;
        
        float bonus = 0f;
        
        // Check food in this direction (within a cone)
        foreach (Transform food in GameManager.Instance.foods)
        {
            if (!food.gameObject.activeSelf) continue;
            
            Vector2 toFood = ((Vector2)food.position - (Vector2)fromPosition).normalized;
            float alignment = Vector2.Dot(direction, toFood);
            
            // Only count food that's in this direction (stricter cone)
            if (alignment > 0.5f) // 60 degree cone (stricter)
            {
                float distance = Vector2.Distance(fromPosition, food.position);
                
                // Closer food = higher bonus
                if (distance < foodSearchRadius)
                {
                    float distanceBonus = (1f - (distance / foodSearchRadius));
                    bonus += alignment * distanceBonus;
                }
            }
        }
        
        return bonus;
    }
    
    private float WolfPenalty(Vector2 direction)
    {
        if (wolfTransform == null) 
            return 0f;
        
        Vector2 toWolf = ((Vector2)wolfTransform.position - (Vector2)transform.position).normalized;
        float dot = Vector2.Dot(direction, toWolf);
        
        // Only penalize if going toward wolf
        if (dot <= 0f)
            return 0f;
        
        return dot;
    }
    
    private Vector2 GetBestCardinalDirection(Vector2 targetDirection)
    {
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
        
        // Draw recent nodes
        if (recentNodes != null)
        {
            foreach (Vector3 recentNode in recentNodes)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(recentNode, 0.3f);
            }
        }
        
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
}