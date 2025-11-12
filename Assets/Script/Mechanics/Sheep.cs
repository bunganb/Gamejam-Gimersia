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

    // Death flag untuk mencegah multiple calls
    private bool isDead = false;

    // Public property untuk diakses GameManager
    public bool IsDead
    {
        get { return isDead; }
    }

    private void Awake()
    {
        movement = GetComponent<Movement>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        isDead = false;
        gameObject.SetActive(true);
        movement.ResetState();

        // Reset animator state
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.Rebind();
        }

        // Aktifkan komponen yang dimatikan di Die()
        if (movement != null)
            movement.enabled = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
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

        // Cek sudah mati atau belum
        if (isDead) return;

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

            isDead = true;

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

        if (isDead) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wolf"))
        {
            isDead = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SheepEaten(this);
            }
            Die();
        }
    }

    public void Die()
    {
        // Double check
        if (isDead == false)
        {
            isDead = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }

        if (movement != null)
            movement.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Play slash sound
        AudioManager.Instance.PlaySFX("Slash");

        // Trigger UI Slash VFX
        if (SlashVFX.Instance != null)
        {
            SlashVFX.Instance.PlayEffect();
        }

        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        yield return null;
        yield return new WaitForSeconds(1.5f);

        // Pastikan masih dalam state mati sebelum disable
        if (isDead && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }
}