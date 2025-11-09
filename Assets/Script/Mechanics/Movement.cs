using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    public float speed = 4f;
    public float speedMultiplier = 1f;
    public Vector2 initialDirection;
    public LayerMask obstacleLayer;

    public Rigidbody2D rb { get; private set; }
    public Vector2 direction { get; private set; }
    public Vector2 nextDirection { get; private set; }
    public Vector3 startingPosition { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startingPosition = transform.position;
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        speedMultiplier = 1f;
        direction = initialDirection;
        nextDirection = Vector2.zero;
        transform.position = startingPosition;
        rb.isKinematic = false;
        enabled = true;
    }

    private void Update()
    {
        
    }


    private void FixedUpdate()
    {
        Vector2 position = rb.position;
        Vector2 translation = speed * speedMultiplier * Time.fixedDeltaTime * direction;

        rb.MovePosition(position + translation);
    }

    public void SetDirection(Vector2 direction, bool forced = false)
    {

        if (forced || !Occupied(direction))
        {
            this.direction = direction;
            nextDirection = Vector2.zero;
        }
        else
        {
            nextDirection = direction;
        }
    }

    public bool Occupied(Vector2 direction)
    {
        Vector2 size = GetComponent<Collider2D>().bounds.size;
        Vector2 boxSize = direction.x != 0 ? new Vector2(size.x * 0.9f, size.y * 0.6f) : new Vector2(size.x * 0.6f, size.y * 0.9f);
        RaycastHit2D hit = Physics2D.BoxCast(rb.position, boxSize, 0f, direction, 0.1f, obstacleLayer);
        return hit.collider != null;

    }

}