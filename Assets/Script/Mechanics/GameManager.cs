using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public Sheep[] sheep;
    public Wolf wolf;
    public Transform foods;
    private int sheepMultiplier = 1;
    public int score { get; private set; }
    public int lives { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager Instance created!");
        }
        else
        {
            Debug.LogWarning("Multiple GameManager instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

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
        foreach (Transform food in this.foods)
        {
            food.gameObject.SetActive(true);
        }
        ResetState();
    }

    private void ResetState()
    {
        for (int i = 0; i < this.sheep.Length; i++)
        {
            this.sheep[i].gameObject.SetActive(true);
        }
        
        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(true);
        }
    }

    private void GameOver()
    {
        for (int i = 0; i < this.sheep.Length; i++)
        {
            this.sheep[i].gameObject.SetActive(false);
        }
        
        if (this.wolf != null)
        {
            this.wolf.gameObject.SetActive(false);
        }
    }

    private void SetScore(int score)
    {
        this.score = score;
        Debug.Log($"Score: {this.score}");
    }

    private void SetLives(int lives)
    {
        this.lives = lives;
    }

    public void FoodEaten(Food food)
    {
        Debug.Log($"<color=green>GameManager.FoodEaten called for {food.name}</color>");
        
        food.gameObject.SetActive(false);
        SetScore(score + food.points);

        if (!HasRemainingPellets())
        {
            if (wolf != null)
            {
                wolf.gameObject.SetActive(false);
            }
            Invoke(nameof(NewRound), 3f);
        }
    }

    void CheckWinCondition()
    {
        int activeFoods = 0;
        foreach (Transform f in foods)
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
    
    private bool HasRemainingPellets()
    {
        foreach (Transform food in foods)
        {
            if (food.gameObject.activeSelf)
            {
                return true;
            }
        }

        return false;
    }
    
    public void FoodPowerEaten(PowerFood food)
    {
        FoodEaten(food);
    }
    
    public void SheepEaten(Sheep sheep)
    {
        int points = sheep.points * sheepMultiplier;
        SetScore(score + points);
        Debug.Log($"<color=red>Sheep eaten! +{points} points. Total: {score}</color>");
    }
}