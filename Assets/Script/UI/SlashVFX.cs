using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SlashVFX : MonoBehaviour
{
    public static SlashVFX Instance { get; private set; } // Add singleton
    
    [Header("Claw Children")]
    public Image[] clawImages;
    
    [Header("Animation Settings")]
    public float delayBetweenClaws = 0.05f;
    public float fillDuration = 0.2f;
    public AnimationCurve fillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Zoom Settings")]
    public float startScale = 2f;
    public float endScale = 1f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Fade Settings")]
    public float fadeOutDelay = 0.3f;
    public float fadeOutDuration = 0.3f;
    
    [Header("Rotation")]
    public bool randomRotation = true;
    public float minRotation = -30f;
    public float maxRotation = 30f;
    
    [Header("Queue Settings")]
    public bool allowMultipleSimultaneous = false;
    public int maxSimultaneousEffects = 3;
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    private RectTransform rectTransform;
    private Queue<bool> effectQueue = new Queue<bool>();
    private int activeEffects = 0;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple SlashVFX instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        rectTransform = GetComponent<RectTransform>();
        
        foreach (Image claw in clawImages)
        {
            if (claw != null)
                claw.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayEffect()
    {
        if (showDebugLogs)
            Debug.Log("SlashVFX: PlayEffect called!");
        
        if (allowMultipleSimultaneous)
        {
            // Allow multiple effects
            if (activeEffects < maxSimultaneousEffects)
            {
                StartCoroutine(AnimateAllClaws());
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("SlashVFX: Max simultaneous effects reached, skipping");
            }
        }
        else
        {
            // Only one effect at a time (restart if playing)
            if (activeEffects > 0)
            {
                StopAllCoroutines();
                activeEffects = 0;
            }
            
            gameObject.SetActive(true);
            StartCoroutine(AnimateAllClaws());
        }
    }

    private IEnumerator AnimateAllClaws()
    {
        activeEffects++;
        
        if (clawImages == null || clawImages.Length == 0)
        {
            Debug.LogWarning("SlashVFX: No claw images assigned!");
            activeEffects--;
            yield break;
        }
        
        // Reset all claws
        foreach (Image claw in clawImages)
        {
            if (claw != null)
            {
                claw.gameObject.SetActive(false);
                claw.fillAmount = 0;
                Color color = claw.color;
                color.a = 1f;
                claw.color = color;
            }
        }
        
        rectTransform.localScale = Vector3.one * startScale;
        
        if (randomRotation)
        {
            float randomAngle = Random.Range(minRotation, maxRotation);
            rectTransform.rotation = Quaternion.Euler(0, 0, randomAngle);
        }
        
        foreach (Image claw in clawImages)
        {
            if (claw != null)
                claw.gameObject.SetActive(true);
        }
        
        float totalAnimationTime = fillDuration + (delayBetweenClaws * (clawImages.Length - 1));
        float elapsed = 0f;
        
        while (elapsed < totalAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / totalAnimationTime;
            
            float scale = Mathf.Lerp(startScale, endScale, zoomCurve.Evaluate(t));
            rectTransform.localScale = Vector3.one * scale;
            
            for (int i = 0; i < clawImages.Length; i++)
            {
                if (clawImages[i] != null)
                {
                    float clawStartTime = i * delayBetweenClaws;
                    float clawElapsed = elapsed - clawStartTime;
                    
                    if (clawElapsed >= 0)
                    {
                        float clawT = Mathf.Clamp01(clawElapsed / fillDuration);
                        clawImages[i].fillAmount = fillCurve.Evaluate(clawT);
                    }
                }
            }
            
            yield return null;
        }
        
        foreach (Image claw in clawImages)
        {
            if (claw != null)
                claw.fillAmount = 1f;
        }
        
        rectTransform.localScale = Vector3.one * endScale;
        yield return new WaitForSeconds(fadeOutDelay);
        
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            foreach (Image claw in clawImages)
            {
                if (claw != null)
                {
                    Color color = claw.color;
                    color.a = 1f - t;
                    claw.color = color;
                }
            }
            
            yield return null;
        }
        
        foreach (Image claw in clawImages)
        {
            if (claw != null)
                claw.gameObject.SetActive(false);
        }
        
        gameObject.SetActive(false);
        rectTransform.rotation = Quaternion.identity;
        
        activeEffects--;
        
        if (showDebugLogs)
            Debug.Log("SlashVFX: Animation complete!");
    }
}