using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PolygonButton : MonoBehaviour, IPointerClickHandler
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Pastikan klik valid dan button aktif
        if (button.interactable)
        {
            // Trigger event onClick
            button.onClick.Invoke();
        }
    }
}