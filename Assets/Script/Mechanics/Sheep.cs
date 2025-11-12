using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Movement))]
public class Sheep : MonoBehaviour
{
    public Movement movement { get; private set; }
    public Transform target;
    public int points = 10;
    private Animator animator;
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    private void Awake()
    {
        movement = GetComponent<Movement>();
        animator = GetComponentInChildren<Animator>();
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
        
        if (other.gameObject.layer == LayerMask.NameToLayer("Wolf"))
        {
            if (showDebugLogs)
                Debug.Log($"<color=red>{gameObject.name} caught by wolf via TRIGGER!</color>");
    
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SheepEaten(this);
            }
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} COLLISION with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wolf"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SheepEaten(this);
            }
            Die(); 
        }

    }
    public void Die()
    {
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }

        if (movement != null)
            movement.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        yield return null;
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
    }



}