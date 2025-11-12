using UnityEngine;
using TMPro;
using System.Collections.Generic;

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
        // Hitung jumlah Sheep yang MASIH HIDUP (IsDead = false)
        int sheepCount = CountLivingSheep();

        // Hitung jumlah Apple (tetap sama)
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

    int CountLivingSheep()
    {
        int livingSheep = 0;

        // Cari semua GameObject dengan tag "Sheep"
        GameObject[] allSheep = GameObject.FindGameObjectsWithTag("Sheep");

        foreach (GameObject sheepObj in allSheep)
        {
            // Cek komponen Sheep dan status isDead
            Sheep sheepComponent = sheepObj.GetComponent<Sheep>();
            if (sheepComponent != null && !sheepComponent.IsDead)
            {
                livingSheep++;
            }
        }

        return livingSheep;
    }
}