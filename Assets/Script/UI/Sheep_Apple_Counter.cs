using UnityEngine;
using TMPro;
using UnityEngine.Tilemaps;

public class SheepAppleCounter : MonoBehaviour
{
    [Header("TMP untuk menampilkan jumlah Sheep")]
    [SerializeField] private TMP_Text sheepCountText;

    [Header("TMP untuk menampilkan jumlah Apple")]
    [SerializeField] private TMP_Text appleCountText;

    private int lastSheepCount = -1;
    private int lastAppleCount = -1;

    void Start()
    {
        UpdateCounts(true);
    }

    void Update()
    {
        UpdateCounts(false);
    }

    void UpdateCounts(bool forceUpdate)
    {
        // Hitung jumlah GameObject dengan tag masing-masing
        int sheepCount = GameObject.FindGameObjectsWithTag("Sheep").Length;
        int appleCount = GameObject.FindGameObjectsWithTag("Apple").Length;

        // Update hanya jika jumlah berubah
        if (forceUpdate || sheepCount != lastSheepCount || appleCount != lastAppleCount)
        {
            lastSheepCount = sheepCount;
            lastAppleCount = appleCount;

            if (sheepCountText != null)
                sheepCountText.text = $"{sheepCount}";

            if (appleCountText != null)
                appleCountText.text = $"{appleCount}";
        }
    }
}