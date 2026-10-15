using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// The target tile depends on the enemy's distance from the player
// if the distance is over 8 tiles away, the target tile is the same as the player's tile
// otherwise, the target tile is the scatter target tile

public class IgnorePersonality : MonoBehaviour, IEnemyPersonality
{
    [SerializeField] private Tilemap mazeMap;

    private GameManager gameManager;
    private GameObject player;

    private Vector3Int scatterTargetTile = new Vector3Int(-11, -10, 0);

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        player = GameObject.Find("Player");
    }

    public Vector3 TargetPos()
    {
        Vector3Int targetTile = Vector3Int.zero;

        if (gameManager.CurrentEnemyMode == GameManager.EnemyMode.CHASE)
        {
            Vector3Int playerTile = mazeMap.WorldToCell(player.transform.position);
            Vector3Int enemyTile = mazeMap.WorldToCell(transform.position);

            float distanceFromPlayer = Vector3Int.Distance(playerTile, enemyTile);

            if (distanceFromPlayer > 8.0)
            {
                targetTile = playerTile;
            }
            else
            {
                targetTile = scatterTargetTile;
            }
        }
        else
        {
            targetTile = scatterTargetTile;
        }

        return mazeMap.GetCellCenterWorld(targetTile);
    }
}
