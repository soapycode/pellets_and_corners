using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// this personality chooses the target tile to be the same tile as the player

public class ChasePersonality : MonoBehaviour, IEnemyPersonality
{
    [SerializeField] private Tilemap mazeMap;

    private GameManager gameManager;
    private GameObject player;

    private Vector3Int scatterTargetTile = new Vector3Int(10, 10, 0);

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        player = GameObject.Find("Player");
    }

    public Vector3 TargetPos()
    {
        Vector3Int tileIndex = Vector3Int.zero;

        if (gameManager.CurrentEnemyMode == GameManager.EnemyMode.CHASE)
        {
            tileIndex = mazeMap.WorldToCell(player.transform.position);
        }
        else
        {
            tileIndex = scatterTargetTile;
        }

        return mazeMap.GetCellCenterWorld(tileIndex);
    }
}
