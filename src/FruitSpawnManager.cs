using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawnManager : MonoBehaviour
{
    private const float FRUIT_DURATION_MIN = 9.0f;
    private const float FRUIT_DURATION_MAX = 10.0f;
    
    private float[] FRUIT_SPAWN_THRESHOLDS = new float[] {0.7f, 0.4f};

    [SerializeField] private GameObject[] fruitPrefabs;
    [SerializeField] private Vector3 fruitSpawnPos = new Vector3(1, 1, 0);

    private GameManager gameManager;
    private uint numFruitSpawns;
    GameObject currentFruit;

    void Start()
    {
        Debug.Assert(fruitPrefabs.Length > 0);

        gameManager = FindObjectOfType<GameManager>();
        gameManager.LevelReset += RemoveFruit;
        
        ResetFruitSpawns();
    }

    void Update()
    {
        if (numFruitSpawns >= FRUIT_SPAWN_THRESHOLDS.Length || currentFruit != null || gameManager.Paused)
            return;

        float pelletRatio = (float)gameManager.GetCurrentPelletCount() / gameManager.GetTotalPelletCount();

        if (pelletRatio < FRUIT_SPAWN_THRESHOLDS[numFruitSpawns])
        {
            ++numFruitSpawns;

            int fruitIndex = Random.Range(0, fruitPrefabs.Length);
            currentFruit = Instantiate(fruitPrefabs[fruitIndex], fruitSpawnPos, Quaternion.identity);

            StartCoroutine("DespawnFruit");
        }
    }

    private IEnumerator DespawnFruit()
    {
        float currentTimer = 0.0f;
        float despawnTimer = Random.Range(FRUIT_DURATION_MIN, FRUIT_DURATION_MAX);

        while (currentTimer < despawnTimer)
        {
            if (gameManager.Paused)
                yield return null;

            currentTimer += Time.deltaTime;

            yield return null;
        }

        RemoveFruit();
    }

    public void RemoveFruit()
    {
        if (currentFruit != null)
            Destroy(currentFruit);

        currentFruit = null;
    }

    public void ResetFruitSpawns()
    {
        RemoveFruit();

        StopAllCoroutines();

        numFruitSpawns = 0;
    }
}
