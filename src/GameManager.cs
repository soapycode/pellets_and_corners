using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public delegate void LevelResetHandler();
    public delegate void LevelStartedHandler();
    public delegate void ScoreUpdatedHandler(uint newScore);
    public delegate void LevelChangedHandler(uint newLevel);
    public delegate void PauseHandler(bool paused);
    public delegate void LivesChangedHandler(int numLives);
    public delegate void EnemyDefeatedHandler(Vector3 pos, uint score);
    public delegate void GameOverHandler();

    public event LevelResetHandler LevelReset;
    public event LevelStartedHandler LevelStarted;
    public event ScoreUpdatedHandler ScoreUpdated;
    public event LevelChangedHandler LevelChanged;
    public event PauseHandler PausedStateChange;
    public event LivesChangedHandler LivesChanged;
    public event EnemyDefeatedHandler EnemyDefeated;
    public event GameOverHandler GameOver;

    private const int MAX_LIVES = 3;
    private const uint BASE_FRUIT_POINTS = 200;
    private const uint BASE_GHOST_KILL_POINTS = 400;
    private const float BASE_FRIGHTENED_MODE_LENGTH = 8.0f;
    private const float FRIGHTENED_BLINKING_THRESHOLD = 2.0f;
    private float[] BASE_SCATTER_WAVE_LENGTHS = new float[] {7.0f, 7.0f, 5.0f, 5.0f};
    private float[] BASE_CHASE_WAVE_LENGTHS = new float[] {20.0f, 20.0f, 20.0f, 0.0f};
    private float[] ENEMY_RELEASED_THRESHOLDS = new float[] {1.0f, 1.0f, 0.85f, 0.66f};

    public enum EnemyMode
    {
        CHASE, SCATTER, FRIGHTENED
    };

    [SerializeField] private Vector3 ghostExitPos = new Vector3(1, 5, 0);
    [SerializeField] private Vector3 ghostHousePos = new Vector3(1, 3, 0);
    [SerializeField] private GameObject pelletMapPrefab;

    private GameObject pelletMapObject;
    private Tilemap pelletMap;
    private uint currentLevel;
    private uint score;
    private bool paused;
    private bool levelStarted;
    private int numLives;
    private int totalPelletCount;
    private int currentPelletCount;
    private int wavesElapsed;
    private float waveTimer;
    private float frightenedTimer;
    private uint consecutiveKills;

    private GameObject player;
    private EnemyMode previousEnemyMode;
    private EnemyMode currentEnemyMode;
    private GameObject[] enemies;
    private List<GameObject> ghostHouseOccupants;
    private FruitSpawnManager fruitSpawner;

    public bool Paused => paused;
    public EnemyMode CurrentEnemyMode => currentEnemyMode;
    public Vector2 GhostExitPos => ghostExitPos;
    public Vector2 GhostHousePos => ghostHousePos;

    // TODO: instead of this, just make some functions to do want I usually use the pellet map for?
    public Tilemap PelletMap => pelletMap;

    // Start is called before the first frame update
    void Start()
    {
        ghostHouseOccupants = new List<GameObject>(4);
        enemies = new GameObject[4];

        GameObject enemiesObject = GameObject.Find("Enemies");
        Debug.Assert(enemiesObject.transform.childCount == enemies.Length);

        for (int i = 0; i < enemiesObject.transform.childCount; ++i)
        {
            enemies[i] = enemiesObject.transform.GetChild(i).gameObject;
        }
        
        player = GameObject.Find("Player");

        fruitSpawner = GetComponent<FruitSpawnManager>();

        ResetGame();
    }

    // Update is called once per frame
    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return)) && levelStarted)
        {
            paused = !paused;
            PausedStateChange(paused);
        }

        currentPelletCount = GetCurrentPelletCount();
        if (currentPelletCount <= 0)
        {
            paused = true;
            SetupNextLevel();
        }

        if (ghostHouseOccupants.Count != 0)
        {
            EnemyController frontEnemy = ghostHouseOccupants[0].GetComponent<EnemyController>();

            if (frontEnemy.State == EnemyController.GhostState.NORMAL)
            {
                ghostHouseOccupants.RemoveAt(0);
                
                if (ghostHouseOccupants.Count > 0)
                    frontEnemy = ghostHouseOccupants[0].GetComponent<EnemyController>();
            }

            // release ghosts from the ghost house if the pellet eaten thresholds are crossed
            if (ghostHouseOccupants.Count > 0)
            {
                int thresholdIndex = ENEMY_RELEASED_THRESHOLDS.Length - ghostHouseOccupants.Count;

                float eatenPelletsRatio = (float)currentPelletCount/(float)totalPelletCount;
                float nextThresholdValue = ENEMY_RELEASED_THRESHOLDS[thresholdIndex];

                if (frontEnemy.State == EnemyController.GhostState.IN_GHOST_HOUSE && eatenPelletsRatio < nextThresholdValue)
                {
                    frontEnemy.LeaveGhostHouse();
                }
            }
        }
    }

    public void ResetGame()
    {
        score = 0;
        currentLevel = 0;
        numLives = MAX_LIVES;

        if (ScoreUpdated != null)
            ScoreUpdated(0);
        
        if (LivesChanged != null)
            LivesChanged(numLives);

        SetupNextLevel();
    }

    private void SetupNextLevel()
    {
        ++currentLevel;

        if (LevelChanged != null)
            LevelChanged(currentLevel);

        // delete old pellet maps if they exist
        if (pelletMap != null)
        {
            pelletMap.ClearAllTiles();
            Destroy(pelletMapObject);
        }

        pelletMapObject = Instantiate(pelletMapPrefab);
        pelletMap = pelletMapObject.GetComponentInChildren<Tilemap>();

        totalPelletCount = GetCurrentPelletCount();
        currentPelletCount = totalPelletCount;

        fruitSpawner.ResetFruitSpawns();

        ResetLevel();
    }

    private void ResetLevel()
    {
        StopAllCoroutines();

        player.transform.position = new Vector3(1, -1, 0);

        paused = true;
        levelStarted = false;

        frightenedTimer = 0.0f;
        consecutiveKills = 0;

        // the ghosts start in scatter mode
        currentEnemyMode = EnemyMode.SCATTER;
        previousEnemyMode = currentEnemyMode;

        wavesElapsed = 0;
        waveTimer = 0.0f;

        StartCoroutine("EnemyWaveRoutine");

        // reset enemies
        ghostHouseOccupants.Clear();

        enemies[0].transform.position = ghostExitPos;
        enemies[1].transform.position = ghostHousePos;
        enemies[2].transform.position = ghostHousePos + Vector3.left;
        enemies[3].transform.position = ghostHousePos + Vector3.right;

        for (int i = 0; i < enemies.Length; ++i)
        {
            EnemyController enemyController = enemies[i].GetComponent<EnemyController>();

            if (enemyController.InitialState == EnemyController.GhostState.IN_GHOST_HOUSE)
                ghostHouseOccupants.Add(enemies[i]);
        }
        
        if (LevelReset != null)
            LevelReset();
        
        // start the game on a small delay
        Invoke("StartLevel", 1.0f);
    }

    private void StartLevel()
    {
        levelStarted = true;
        paused = false;

        ghostHouseOccupants[0].GetComponent<EnemyController>().LeaveGhostHouse();

        if (LevelStarted != null)
            LevelStarted();
    }

    private void ChangeEnemyMode(EnemyMode newMode)
    {
        if (currentEnemyMode != newMode) {
            previousEnemyMode = currentEnemyMode;
            currentEnemyMode = newMode;
        }

        // update the enemies
        for (int i = 0; i < enemies.Length; ++i)
        {
            EnemyController enemyController = enemies[i].GetComponent<EnemyController>();
            enemyController.EnemyModeChanged(currentEnemyMode, previousEnemyMode);
        }
    }

    private IEnumerator EnemyWaveRoutine()
    {
        bool wavesOver = false;
        while (!wavesOver)
        {
            if (paused || currentEnemyMode == EnemyMode.FRIGHTENED)
            {
                yield return null;
                continue;
            }
            
            waveTimer += Time.deltaTime;

            if (currentEnemyMode == EnemyMode.SCATTER && waveTimer >= BASE_SCATTER_WAVE_LENGTHS[wavesElapsed])
            {
                ChangeEnemyMode(EnemyMode.CHASE);
                waveTimer = 0.0f;
            }
            else if (currentEnemyMode == EnemyMode.CHASE && waveTimer >= BASE_CHASE_WAVE_LENGTHS[wavesElapsed])
            {
                ++wavesElapsed;

                if (wavesElapsed >= BASE_SCATTER_WAVE_LENGTHS.Length)
                    wavesOver = true;
                else
                {
                    ChangeEnemyMode(EnemyMode.SCATTER);
                    waveTimer = 0.0f;
                }
            }

            yield return null;
        }
    }

    private IEnumerator FrightenedModeRoutine()
    {
        frightenedTimer = 0.0f;

        while (frightenedTimer < BASE_FRIGHTENED_MODE_LENGTH)
        {
            if (paused)
            {
                yield return null;
                continue;
            }

            frightenedTimer += Time.deltaTime;

            if (BASE_FRIGHTENED_MODE_LENGTH - frightenedTimer <= FRIGHTENED_BLINKING_THRESHOLD)
            {
                foreach (GameObject enemy in enemies)
                {
                    enemy.GetComponent<EnemyAnimator>().ToggleBlinkingState(true);
                }
            }

            yield return null;
        }

        ChangeEnemyMode(previousEnemyMode);
    }

    private void Unpause()
    {
        paused = false;

        if (PausedStateChange != null)
            PausedStateChange(paused);
    }

    public void AddScore(uint amount)
    {
        score += amount;
        ScoreUpdated(score);
    }

    public void LoseLife()
    {
        --numLives;

        if (LivesChanged != null)
            LivesChanged(numLives);

        if (numLives > 0)
        {
            ResetLevel();
        }
        else
        {
            paused = true;

            if (GameOver != null)
                GameOver();
        }
    }

    public void EnemyKilled(GameObject enemy)
    {
        ++consecutiveKills;
        uint scoreToAdd = BASE_GHOST_KILL_POINTS*consecutiveKills;
        AddScore(scoreToAdd);

        // insert dead enemy at the beginning of the list so that it leaves first
        ghostHouseOccupants.Insert(0, enemy);

        Debug.Assert(ghostHouseOccupants.Count <= 4);

        if (EnemyDefeated != null)
            EnemyDefeated(enemy.transform.position, scoreToAdd);
        
        // pause briefly to add more weight to the ghost kill
        paused = true;
        Invoke("Unpause", 0.3f);
    }

    public void EnterFrightenedMode()
    {
        // first, reset all aspects of frightened mode
        StopCoroutine("FrightenedModeRoutine");
        foreach(var enemy in enemies)
        {
            enemy.GetComponent<EnemyAnimator>().ToggleBlinkingState(false);
        }

        // now we start up the new frightened mode
        ChangeEnemyMode(EnemyMode.FRIGHTENED);
        
        consecutiveKills = 0;
        StartCoroutine("FrightenedModeRoutine");
    }

    public int GetCurrentPelletCount()
    {
        int result = 0;

        TileBase[] pelletTiles = pelletMap.GetTilesBlock(pelletMap.cellBounds);
        for (int i = 0; i < pelletTiles.Length; ++i)
        {
            if (pelletTiles[i] != null)
                ++result;
        }

        return result;
    }

    public int GetTotalPelletCount()
    {
        return totalPelletCount;
    }

    public void FruitEaten()
    {
        fruitSpawner.RemoveFruit();
        AddScore(BASE_FRUIT_POINTS);
    }
}
