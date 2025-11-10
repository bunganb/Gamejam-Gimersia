using UnityEngine;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Movement))]
public class Sheep : MonoBehaviour
{
    public Movement movement { get; private set; }
    public Transform target;
    public int points = 10;
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    private void Awake()
    {
        movement = GetComponent<Movement>();
    }

    private void Start()
    {
        ResetState();
        
        // Debug: Check sheep setup
        Collider2D[] colliders = GetComponents<Collider2D>();
        
        bool hasTrigger = false;
        bool hasNonTrigger = false;
        
        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
            {
                hasTrigger = true;
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} has TRIGGER collider: {col.GetType().Name}");
            }
            else
            {
                hasNonTrigger = true;
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} has NON-TRIGGER collider: {col.GetType().Name}");
            }
        }
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} Setup:");
            Debug.Log($"  - Has Trigger: {hasTrigger}");
            Debug.Log($"  - Has Non-Trigger: {hasNonTrigger}");
            Debug.Log($"  - Has Rigidbody2D: {rb != null}");
            if (rb != null)
            {
                Debug.Log($"  - Rigidbody Type: {rb.bodyType}");
                Debug.Log($"  - Is Kinematic: {rb.isKinematic}");
            }
            Debug.Log($"  - Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }

    public void ResetState()
    {
        gameObject.SetActive(true);
        movement.ResetState();
    }

    public void SetPosition(Vector3 position)
    {
        position.z = transform.position.z;
        transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} TRIGGER with {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        
        if (other.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            Food food = other.GetComponent<Food>();
            if (food != null)
            {
                food.Eat();
            }
        }
        
        // Also check for wolf collision via trigger
        if (other.gameObject.layer == LayerMask.NameToLayer("Wolf"))
        {
            if (showDebugLogs)
                Debug.Log($"<color=red>{gameObject.name} caught by wolf via TRIGGER!</color>");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SheepEaten(this);
            }
            gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} COLLISION with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");

        // Sheep caught by wolf
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wolf") || 
            collision.gameObject.CompareTag("Player"))
        {
            if (showDebugLogs)
                Debug.Log($"<color=red>{gameObject.name} caught by wolf via COLLISION!</color>");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SheepEaten(this);
            }
            gameObject.SetActive(false);
        }
    }
}