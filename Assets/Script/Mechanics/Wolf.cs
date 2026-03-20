using UnityEngine;

[RequireComponent(typeof(Movement))]
public class Wolf : MonoBehaviour
{
    private Movement movement;

    private Vector2 _bufferedInput = Vector2.zero;
    private bool _hasBufferedInput = false;

    public bool InputEnable { get; set; } = true;

    private void Awake()
    {
        movement = GetComponent<Movement>();
    }

    private void Update()
    {
        if(!InputEnable) return;

        Vector2 input = GetInputDirection();
        if(input != Vector2.zero)
        {
            if(movement.Direction == Vector2.zero)
            {
                movement.SetDirection(input);
            }
            else
            {
                _bufferedInput = input;
                _hasBufferedInput = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if(_hasBufferedInput)
        {
            movement.SetDirection(_bufferedInput);
            _hasBufferedInput = false;
        }
    }

    private Vector2 GetInputDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(horizontal, vertical).normalized;
        return input;
    }

    public void DisableInput() => InputEnable = false;
    public void EnableInput() => InputEnable = true;
}