using UnityEngine;

public enum PowerUpType
{
    OpenMap,      // Reduces vignette to reveal map
    HowlOfFear    // Slows down all sheep
}

public class WolfPowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType;
    public float duration = 4f;

    [Header("Open Map Settings")]
    [Tooltip("Vignette intensity when power-up is active")]
    public float openMapVignetteIntensity = 0.1f;
    [Tooltip("Vignette smoothness when power-up is active")]
    public float openMapVignetteSmoothness = 0.1f;

    [Header("Howl of Fear Settings")]
    [Tooltip("Speed multiplier for sheep (e.g., 0.5 = half speed)")]
    public float fearSpeedMultiplier = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hanya Wolf yang bisa mengambil power-up
        if (other.gameObject.layer == LayerMask.NameToLayer("Wolf"))
        {
            ActivatePowerUp();
            gameObject.SetActive(false);
        }
    }

    private void ActivatePowerUp()
    {
        if (GameManager.Instance != null)
        {
            switch (powerUpType)
            {
                case PowerUpType.OpenMap:
                    GameManager.Instance.ActivateOpenMap(duration, openMapVignetteIntensity, openMapVignetteSmoothness);
                    Debug.Log($"<color=cyan>Wolf collected Open Map power-up! Duration: {duration}s</color>");
                    break;

                case PowerUpType.HowlOfFear:
                    GameManager.Instance.ActivateHowlOfFear(duration, fearSpeedMultiplier);
                    Debug.Log($"<color=orange>Wolf collected Howl of Fear! Sheep slowed to {fearSpeedMultiplier * 100}% speed for {duration}s</color>");
                    break;
            }

            // Aktifkan UI dengan jenis power-up
            if (PowerUpUIManager.Instance != null)
                PowerUpUIManager.Instance.ShowPowerUpDuration(duration, powerUpType);
        }
    }
}
