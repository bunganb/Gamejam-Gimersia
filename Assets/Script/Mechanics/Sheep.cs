using UnityEngine;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Movement))]
public class Sheep : MonoBehaviour
{
    public Movement movement { get; private set; }
    public Transform target;
    public int points = 200;

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
        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger) hasTrigger = true;
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
        if (other.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            Food food = other.GetComponent<Food>();
            if (food != null)
            {
                food.Eat();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        // Sheep caught by wolf
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wolf") || 
            collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SheepEaten(this);
            }
            gameObject.SetActive(false);
        }
    }
}