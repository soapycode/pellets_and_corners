using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public Vector2 Direction => dir;

    private Vector2 dir;
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        dir = Vector2.zero;
        gameManager = GameObject.FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalAxis = 0.0f;
        float verticalAxis = 0.0f;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            verticalAxis = 1.0f;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            verticalAxis = -1.0f;
        
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            horizontalAxis = 1.0f;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            horizontalAxis = -1.0f;

        Vector2 newDir = Vector2.zero;
        if (horizontalAxis != 0)
            newDir.x = horizontalAxis;
        else if (verticalAxis != 0)
            newDir.y = verticalAxis;

        dir = newDir.normalized;

        Vector3Int tileIndex = gameManager.PelletMap.WorldToCell(transform.position);
        TileBase tile = gameManager.PelletMap.GetTile(tileIndex);
        if (tile)
        {
            gameManager.PelletMap.SetTile(tileIndex, null);
            uint score = 0;

            if (tile.name.Contains("normal"))
            {
                score = 25;
            }
            else if (tile.name.Contains("super"))
            {
                score = 75;
                gameManager.EnterFrightenedMode();
            }

            gameManager.AddScore(score);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager.Paused)
            return;

        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.gameObject.GetComponent<EnemyController>();

            if (gameManager.CurrentEnemyMode == GameManager.EnemyMode.FRIGHTENED && enemy.Killable)
            {
                enemy.Die();
                gameManager.EnemyKilled(enemy.gameObject);
            }
            else if (!enemy.Killable && enemy.State != EnemyController.GhostState.DEAD && enemy.State != EnemyController.GhostState.ENTERING_GHOST_HOUSE)
            {
                //TODO: play death animation
                gameManager.LoseLife();
                dir = Vector2.zero;
            }
        }
        else if (other.CompareTag("Fruit"))
        {
            gameManager.FruitEaten();
        }
    }
}
