// Sheep.cs
using System.Collections;
using UnityEngine;

public class Sheep : MonoBehaviour
{
    [Header("Stats")]
    public int points = 10;

    [Header("Colliders")]
    [SerializeField] private Collider2D physicsCollider;
    [SerializeField] private Collider2D triggerCollider;

    [Header("Detection")]
    [SerializeField] private LayerMask wolfLayer;

    [Header("Death Animation")]
    [SerializeField] private string deathBoolName = "IsDead";
    [SerializeField] private string deathStateName = "Sheep_Dead";
    [SerializeField] private float fallbackDeathDuration = 1f;

    public bool IsDead { get; private set; } = false;

    private Animator _animator;
    private SheepAI _ai;
    private Movement _movement;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _ai = GetComponent<SheepAI>();
        _movement = GetComponent<Movement>();

        if (physicsCollider == null || triggerCollider == null)
            AutoDetectColliders();
    }

    // ─────────────────────────────────────────────
    //  COLLISION
    // ─────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isWolf = (wolfLayer.value & (1 << other.gameObject.layer)) != 0
                   || other.GetComponent<Wolf>() != null;

        if (!isWolf) return;

        Debug.Log($"[Sheep:{name}] 🐺 Wolf terdeteksi!");
        Die();
    }

    // ─────────────────────────────────────────────
    //  DIE
    // ─────────────────────────────────────────────

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Debug.Log($"[Sheep:{name}] 💀 Die()");

        if (physicsCollider != null) physicsCollider.enabled = false;
        if (triggerCollider != null) triggerCollider.enabled = false;

        if (_ai != null) _ai.enabled = false;
        if (_movement != null)
        {
            _movement.enabled = false;
            _movement.Rb.linearVelocity = Vector2.zero;
            _movement.Rb.bodyType = RigidbodyType2D.Static;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SheepEaten(this);
        else
            Debug.LogError($"[Sheep:{name}] ❌ GameManager null!");

        StartCoroutine(PlayDeathAndDeactivate());
    }

    // ─────────────────────────────────────────────
    //  RESET
    // ─────────────────────────────────────────────

    public void ResetSheep()
    {
        IsDead = false;

        if (physicsCollider != null) physicsCollider.enabled = true;
        if (triggerCollider != null) triggerCollider.enabled = true;
        if (_ai != null) _ai.enabled = true;
        if (_movement != null)
        {
            _movement.enabled = true;
            _movement.Rb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (_animator != null)
        {
            _animator.SetBool(deathBoolName, false);
            _animator.Rebind();
            _animator.Update(0f);
        }

        Debug.Log($"[Sheep:{name}] 🔄 Reset");
    }

    // ─────────────────────────────────────────────
    //  COROUTINE
    // ─────────────────────────────────────────────

    private IEnumerator PlayDeathAndDeactivate()
    {
        if (_animator == null)
        {
            Deactivate();
            yield break;
        }

        if (!HasParameter(deathBoolName, AnimatorControllerParameterType.Bool))
        {
            Debug.LogWarning($"[Sheep:{name}] ⚠️ Parameter '{deathBoolName}' tidak ditemukan. " +
                             $"Parameter tersedia: {GetAllParameterNames()}");
            yield return new WaitForSeconds(fallbackDeathDuration);
            Deactivate();
            yield break;
        }

        _animator.SetBool(deathBoolName, true);
        Debug.Log($"[Sheep:{name}] 🎬 '{deathBoolName}' = true");

        // Tunggu 2 frame agar Animator transisi
        yield return null;
        yield return null;

        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);

        if (!info.IsName(deathStateName))
        {
            Debug.LogWarning($"[Sheep:{name}] ⚠️ State '{deathStateName}' tidak aktif! " +
                             $"Cek nama state di Animator.");
            yield return new WaitForSeconds(fallbackDeathDuration);
            Deactivate();
            yield break;
        }

        // Tunggu animasi selesai
        while (true)
        {
            info = _animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsName(deathStateName) && info.normalizedTime >= 1f) break;
            if (!info.IsName(deathStateName)) break;

            yield return null;
        }

        Deactivate();
    }

    private void Deactivate()
    {
        Debug.Log($"[Sheep:{name}] 🚫 Deactivated");
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void AutoDetectColliders()
    {
        Collider2D[] cols = GetComponents<Collider2D>();

        if (cols.Length < 2)
        {
            Debug.LogWarning($"[Sheep:{name}] ⚠️ Kurang dari 2 collider! Ditemukan: {cols.Length}");
            if (cols.Length == 1) physicsCollider = cols[0];
            return;
        }

        foreach (Collider2D col in cols)
        {
            if (col.isTrigger) triggerCollider = col;
            else physicsCollider = col;
        }
    }

    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (_animator == null) return false;
        foreach (AnimatorControllerParameter p in _animator.parameters)
            if (p.name == paramName && p.type == type) return true;
        return false;
    }

    private string GetAllParameterNames()
    {
        if (_animator == null) return "animator null";
        var sb = new System.Text.StringBuilder();
        foreach (AnimatorControllerParameter p in _animator.parameters)
            sb.Append($"{p.name}({p.type}) ");
        return sb.ToString();
    }
}