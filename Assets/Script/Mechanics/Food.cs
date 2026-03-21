// Food.cs
using UnityEngine;

public class Food : MonoBehaviour
{
    public int points = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Sheep>() == null) return;

        Debug.Log($"[Food:{name}] Dimakan oleh {other.name}");
        Eat();
    }

    public virtual void Eat()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.FoodEaten(this);
        else
            Debug.LogError($"[Food:{name}] ❌ GameManager null!");
    }
}