using UnityEngine;

[RequireComponent(typeof(Movement))]
public class Wolf : MonoBehaviour
{
    private Movement _movement;

    private void Awake()
    {
        _movement = GetComponent<Movement>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            _movement.SetDirection(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.S))
            _movement.SetDirection(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.A))
            _movement.SetDirection(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.D))
            _movement.SetDirection(Vector2.right);
    }
}