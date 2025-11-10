using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Movement))]
public class CharacterAnimation : MonoBehaviour
{
    private Animator animator;
    private Movement movement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<Movement>();
    }

    private void Update()
    {
        if (animator == null || movement == null)
            return;

        // Ambil arah dari Movement
        Vector2 dir = movement.direction;

        // Kirim arah ke Animator
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);

        // Tambahkan parameter IsMoving
        bool isMoving = dir.sqrMagnitude > 0.01f;  // periksa jika masih ada arah
        animator.SetBool("IsMoving", isMoving);
    }
}