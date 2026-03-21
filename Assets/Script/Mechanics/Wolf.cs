// Wolf.cs
using UnityEngine;

[RequireComponent(typeof(Movement))]
public class Wolf : MonoBehaviour
{
    private Movement _movement;
    private Vector2 _bufferedInput = Vector2.zero;

    public bool InputEnabled { get; set; } = true;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _movement = GetComponent<Movement>();

        if (!_movement.freeMovement)
        {
            _movement.freeMovement = true;
            Debug.LogWarning("[Wolf] freeMovement di-set TRUE otomatis. " +
                             "Sebaiknya set manual di Inspector.");
        }
    }

    private void Update()
    {
        if (!InputEnabled) return;

        Vector2 input = GetCardinalInput();
        if (input == Vector2.zero) return;

        _bufferedInput = input;
        _movement.SetDirection(input);
    }

    private void FixedUpdate()
    {
        if (!InputEnabled || _bufferedInput == Vector2.zero) return;

        if (_movement.Direction != _bufferedInput)
            _movement.SetDirection(_bufferedInput);
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    public void DisableInput()
    {
        InputEnabled = false;
        _bufferedInput = Vector2.zero;
        Debug.Log("[Wolf] Input disabled");
    }

    public void EnableInput()
    {
        InputEnabled = true;
        Debug.Log("[Wolf] Input enabled");
    }

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    private Vector2 GetCardinalInput()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) return Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) return Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) return Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) return Vector2.right;
        return Vector2.zero;
    }
}