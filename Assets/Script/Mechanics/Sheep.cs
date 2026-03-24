using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Movement))]
public class Sheep : MonoBehaviour
{
    public Movement Movement { get; private set; }
    public int points = 10;
    public bool IsDead { get; private set; }

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Animator _animator;
    private Collider2D _collider;

    private void Awake()
    {
        Movement = GetComponent<Movement>();
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        ResetState();
    }

    public void ResetState()
    {
        IsDead = false;
        gameObject.SetActive(true);
        Movement.ResetState();

        if (_animator != null)
        {
            _animator.SetBool("IsDead", false);
            _animator.Rebind();
        }

        if (Movement != null)
            Movement.enabled = true;

        if (_collider != null)
            _collider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead) return;

        int otherLayer = other.gameObject.layer;
        if (otherLayer == LayerMask.NameToLayer("Food"))
        {
            Food food = other.GetComponent<Food>();
            if (food != null) food.Eat();
        }
        else if (otherLayer == LayerMask.NameToLayer("Wolf"))
        {
            HandleWolfContact();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wolf"))
        {
            HandleWolfContact();
        }
    }

    private void HandleWolfContact()
    {
        IsDead = true;
        GameManager.Instance?.SheepEaten(this);
        Die();
    }

    public void Die()
    {
        if (IsDead == false) IsDead = true; // guard

        if (_animator != null)
            _animator.SetBool("IsDead", true);

        if (Movement != null)
            Movement.enabled = false;

        if (_collider != null)
            _collider.enabled = false;

        AudioManager.Instance?.PlaySFX("Slash");
        SlashVFX.Instance?.PlayEffect();

        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        // Tunggu 1 frame untuk animasi mulai, lalu tunggu durasi animasi
        yield return null;
        yield return new WaitForSeconds(1.5f);
        if (IsDead && gameObject.activeInHierarchy)
            gameObject.SetActive(false);
    }
}