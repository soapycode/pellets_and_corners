using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// this personality selects the target position as two tiles in front of the player,
// which is then offset by the vector between that tile and the red enemy's position

public class FicklePersonality : MonoBehaviour, IEnemyPersonality
{
    [SerializeField] private Tilemap mazeMap;
    [SerializeField] private GameObject redEnemy;

    private GameManager gameManager;
    private GameObject player;
    private MazeMover playerMover;

    private Vector3Int scatterTargetTile = new Vector3Int(11, -10, 0);

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        player = GameObject.Find("Player");
        playerMover = player.GetComponent<MazeMover>();
    }

    public Vector3 TargetPos()
    {
        Vector3Int targetTile = Vector3Int.zero;

        if (gameManager.CurrentEnemyMode == GameManager.EnemyMode.CHASE)
        {
            Vector3Int redEnemyTile = mazeMap.WorldToCell(redEnemy.transform.position);
            Vector3Int playerTile = mazeMap.WorldToCell(player.transform.position);
            Vector3 playerDir = playerMover.Direction;

            Vector3Int initialTargetTile = playerTile + new Vector3Int((int)playerDir.x, (int)playerDir.y, 0)*2;
            
            Vector3Int indexOffset = initialTargetTile - redEnemyTile;

            targetTile = initialTargetTile + indexOffset;
        }
        else
        {
            targetTile = scatterTargetTile;
        }

        return mazeMap.GetCellCenterWorld(targetTile);
    }
}
