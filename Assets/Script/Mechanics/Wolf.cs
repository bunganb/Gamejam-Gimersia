using UnityEngine;

[RequireComponent(typeof(Movement))]
public class Wolf : MonoBehaviour
{
    private Movement movement;

    private void Awake()
    {
        movement = GetComponent<Movement>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            movement.SetDirection(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.S))
            movement.SetDirection(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.A))
            movement.SetDirection(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.D))
            movement.SetDirection(Vector2.right);
    }
}