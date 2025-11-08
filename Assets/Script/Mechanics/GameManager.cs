using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Sheep[] sheep;
    public Wolf wolf;
    public Transform foods;
    public int score { get; private set; }
    public int lives { get; private set; }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        SetScore(0);
        SetLives(3);
        NewRound();
    }

    private void NewRound()
    {
        foreach (Transform foods in this.foods)
        {
            foods.gameObject.SetActive(true);
        }
        ResetState();
    }

    private void ResetState()
    {
        for (int i = 0; i < this.sheep.Length; i++)
        {
            this.sheep[i].gameObject.SetActive(true);
        }
        this.wolf.gameObject.SetActive(true);
    }

    private void GameOver()
    {
        for (int i = 0; i < this.sheep.Length; i++)
        {
            this.sheep[i].gameObject.SetActive(false);
        }
        this.wolf.gameObject.SetActive(false);
    }

    private void SetScore(int score)
    {
        this.score = score;
    }

    private void SetLives(int lives)
    {
        this.lives = lives;
    }

    public void SheepEaten(Sheep sheep)
    {
        SetScore(this.score + sheep.points);
    }
    public void FoodEaten(Food food)
    {
        score += food.points;
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        int activeFoods = 0;
        foreach(Transform f in foods)
        {
            if (f.gameObject.activeSelf)
                activeFoods++;
        }

        if (activeFoods == 0)
            SheepWin();
    }

    void SheepWin()
    {
        Debug.Log("Sheep win!");
    }

}
