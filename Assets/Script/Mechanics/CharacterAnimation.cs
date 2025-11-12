using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    private Animator animator;
    private Movement movement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponentInParent<Movement>(); // 💡 ambil dari parent, bukan GetComponent
    }

    private void Update()
    {
        if (movement == null) return;

        // Misalnya kamu punya parameter MoveX, MoveY, dan IsMoving
        animator.SetFloat("MoveX", movement.direction.x);
        animator.SetFloat("MoveY", movement.direction.y);
        animator.SetBool("IsMoving", movement.direction != Vector2.zero);
    }
}