using UnityEngine;

public class Food : MonoBehaviour
{
    public int points = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Jika domba menyentuh food
        if (collision.CompareTag("Sheep"))
        {
            GameManager gm = FindAnyObjectByType<GameManager>();
            gm.FoodEaten(this);

            gameObject.SetActive(false); // Nonaktifkan food
        }
    }
}