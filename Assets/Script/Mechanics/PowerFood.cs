using UnityEngine;

public class PowerFood : Food
{
    public float duration = 8f;
    public override void Eat()
    {
        GameManager.Instance.FoodPowerEaten(this);
    }
}
