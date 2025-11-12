using UnityEngine;

public class SlashVFXManager : MonoBehaviour
{
    public static SlashVFXManager Instance { get; private set; }
    
    [Header("VFX Reference")]
    public SlashVFX slashVFX;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    public void PlaySlashEffect()
    {
        if (slashVFX != null)
        {
            slashVFX.PlayEffect();
        }
        else
        {
            Debug.LogWarning("SlashVFXManager: No SlashVFX assigned!");
        }
    }
}