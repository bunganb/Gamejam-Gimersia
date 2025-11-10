using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefabs for each power-up type")]
    public GameObject openMapPrefab;
    public GameObject howlOfFearPrefab;
    
    [Tooltip("Minimum time between spawns")]
    public float minSpawnInterval = 15f;
    [Tooltip("Maximum time between spawns")]
    public float maxSpawnInterval = 30f;
    
    [Tooltip("Maximum number of active power-ups at once")]
    public int maxActivePowerUps = 2;
    
    [Header("References")]
    [Tooltip("Parent transform containing all nodes")]
    public Transform nodesParent;
    
    private List<Node> availableNodes = new List<Node>();
    private List<GameObject> activePowerUps = new List<GameObject>();
    private float nextSpawnTime;
    
    private void Start()
    {
        // Find all nodes in the scene
        if (nodesParent != null)
        {
            Node[] nodes = nodesParent.GetComponentsInChildren<Node>();
            availableNodes.AddRange(nodes);
        }
        else
        {
            // Fallback: find all nodes in scene
            availableNodes.AddRange(FindObjectsOfType<Node>());
        }
        
        if (availableNodes.Count == 0)
        {
            Debug.LogError("PowerUpSpawner: No nodes found! Cannot spawn power-ups.");
            enabled = false;
            return;
        }
        
        Debug.Log($"PowerUpSpawner: Found {availableNodes.Count} nodes");
        
        // Schedule first spawn
        ScheduleNextSpawn();
    }
    
    private void Update()
    {
        // Clean up destroyed power-ups from list
        activePowerUps.RemoveAll(item => item == null || !item.activeSelf);
        
        // Check if it's time to spawn
        if (Time.time >= nextSpawnTime && activePowerUps.Count < maxActivePowerUps)
        {
            SpawnRandomPowerUp();
            ScheduleNextSpawn();
        }
    }
    
    private void ScheduleNextSpawn()
    {
        float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        nextSpawnTime = Time.time + interval;
        Debug.Log($"Next power-up spawn in {interval:F1} seconds");
    }
    
    private void SpawnRandomPowerUp()
    {
        if (availableNodes.Count == 0) return;
        
        // Pick random node
        Node randomNode = availableNodes[Random.Range(0, availableNodes.Count)];
        
        // Pick random power-up type
        GameObject prefabToSpawn = Random.value > 0.5f ? openMapPrefab : howlOfFearPrefab;
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("PowerUpSpawner: Prefab is null!");
            return;
        }
        
        // Spawn power-up at node position
        GameObject powerUp = Instantiate(prefabToSpawn, randomNode.transform.position, Quaternion.identity);
        powerUp.transform.SetParent(transform); // Organize under spawner
        
        activePowerUps.Add(powerUp);
        
        string powerUpName = prefabToSpawn == openMapPrefab ? "Open Map" : "Howl of Fear";
        Debug.Log($"<color=yellow>Spawned {powerUpName} at node {randomNode.name}</color>");
    }
    
    // Optional: Call this to spawn a specific type
    public void SpawnPowerUpOfType(PowerUpType type)
    {
        if (availableNodes.Count == 0) return;
        
        Node randomNode = availableNodes[Random.Range(0, availableNodes.Count)];
        GameObject prefabToSpawn = type == PowerUpType.OpenMap ? openMapPrefab : howlOfFearPrefab;
        
        if (prefabToSpawn == null) return;
        
        GameObject powerUp = Instantiate(prefabToSpawn, randomNode.transform.position, Quaternion.identity);
        powerUp.transform.SetParent(transform);
        activePowerUps.Add(powerUp);
    }
}