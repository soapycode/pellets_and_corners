using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// this personality chooses the target tile to be 4 tiles ahead of where the player is facing

public class AmbushPersonality : MonoBehaviour, IEnemyPersonality
{
    [SerializeField] private Tilemap mazeMap;

    private GameManager gameManager;
    private GameObject player;
    private MazeMover playerMover;

    private Vector3Int scatterTargetTile = new Vector3Int(-10, 10, 0);

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        player = GameObject.Find("Player");
        playerMover = player.GetComponent<MazeMover>();
    }

    public Vector3 TargetPos()
    {
        Vector3Int tileIndex = Vector3Int.zero;

        if (gameManager.CurrentEnemyMode == GameManager.EnemyMode.CHASE)
        {
            Vector3Int initialIndex = mazeMap.WorldToCell(player.transform.position);
            Vector3 dir = playerMover.Direction;

            // NOTE: this fixes the bug in Pacman where the target is different when the player is facing up
            tileIndex = initialIndex + new Vector3Int((int)dir.x, (int)dir.y, 0)*4;
        }
        else
        {
            tileIndex = scatterTargetTile;
        }

        return mazeMap.GetCellCenterWorld(tileIndex);
    }
}
