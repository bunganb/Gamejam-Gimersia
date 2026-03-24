using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterAnimation : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");

    private Animator animator;
    private Movement movement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<Movement>(); // Cari di GameObject yang sama
        if (movement == null)
            movement = GetComponentInParent<Movement>(); // fallback

        if (movement != null)
            movement.OnDirectionChanged.AddListener(OnDirectionChanged);
        else
            Debug.LogError("Movement not found!", this);
    }

    private void OnDirectionChanged(Vector2 dir)
    {
        animator.SetFloat(MoveX, dir.x);
        animator.SetFloat(MoveY, dir.y);
        animator.SetBool(IsMoving, dir != Vector2.zero);
    }

    private void OnDestroy()
    {
        if (movement != null)
            movement.OnDirectionChanged.RemoveListener(OnDirectionChanged);
    }
}