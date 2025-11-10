using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteFollower : MonoBehaviour
{
    public Transform player;
    public Camera cam;
    public Volume volume; 

    private Vignette vignette;

    void Start()
    {
        if (volume == null)
            volume = GetComponentInParent<Volume>();

        if (volume == null)
        {
            Debug.LogError("No Volume component found! Please assign it in the Inspector.", this);
            return;
        }

        if (volume.profile.TryGet(out Vignette v))
        {
            vignette = v;
        }
        else
        {
            Debug.LogWarning("No Vignette override found in the Volume profile.", this);
        }
    }

    void Update()
    {
        if (vignette == null) return;

        Vector3 viewportPos = cam.WorldToViewportPoint(player.position);

        vignette.center.value = new Vector2(viewportPos.x, viewportPos.y);
    }
}