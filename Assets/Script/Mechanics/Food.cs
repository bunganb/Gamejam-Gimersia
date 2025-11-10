using UnityEngine;

public class Food : MonoBehaviour
{
    public int points = 10;

    private void Start()
    {
        Collider2D collider = GetComponent<Collider2D>();
    }

    public virtual void Eat()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FoodEaten(this);
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Sheep"))
        {
            Eat();
        }
    }
}