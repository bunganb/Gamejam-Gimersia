using UnityEngine;

public class Food : MonoBehaviour
{
    public int points = 10;

    protected virtual void Eat()
    {
        GameManager.Instance.FoodEaten(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Sheep")) {
            Eat();
        }
    }
}