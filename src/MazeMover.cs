using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MazeMover : MonoBehaviour
{
    public Tilemap mazeMap;

    private PlayerController controller;
    private ObjectAnimator objectAnimator;
    private GameManager gameManager;

    private float speed = 5.0f;

    private Vector2 currDir;
    private Vector2 bufferedDir;

    private Vector2 targetPos;

    public Vector2 Direction => currDir;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        objectAnimator = GetComponent<ObjectAnimator>();
        gameManager = GameObject.FindObjectOfType<GameManager>();

        gameManager.LevelReset += Reset;

        GetComponent<ScreenWrap>().BoundsEntered += UpdateTargetAfterWarp;

        Reset();
    }

    void Update()
    {
        if (gameManager.Paused) return;
        
        if (controller.Direction == Vector2.zero)
        {
            // see if we can move in the buffered direction first
            if (bufferedDir != Vector2.zero && !IsTileInDir(bufferedDir))
            {
                currDir = bufferedDir;
                bufferedDir = Vector2.zero;
            }
        }
        else // the controller updated so we need to handle that
        {
            if (!IsTileInDir(controller.Direction))
            {
                currDir = controller.Direction;
                bufferedDir = Vector2.zero;

                Vector3 truncatedPos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);

                Vector3Int tileIndex = mazeMap.WorldToCell(truncatedPos);
                Vector3Int nextTile = tileIndex + new Vector3Int((int)currDir.x, (int)currDir.y, 0);
                
                targetPos = mazeMap.GetCellCenterWorld(nextTile);
            }
            else
            {
                bufferedDir = controller.Direction;
            }
        }

        objectAnimator.SetDirection(currDir);
        objectAnimator.SetPlaying(true);

        float moveDistance = speed*Time.deltaTime;
        Vector2 toTargetDir = targetPos - (Vector2)transform.position;
        if (toTargetDir.magnitude < moveDistance)
        {
            float distanceRemainder = moveDistance - toTargetDir.magnitude;
            transform.position = targetPos;
            moveDistance = distanceRemainder;

            // need new target position
            if (!IsTileInDir(currDir))
            {
                Vector3Int tileIndex = mazeMap.WorldToCell(transform.position);
                Vector3Int nextTile = new Vector3Int((int)(tileIndex.x + 1*currDir.x), (int)(tileIndex.y + 1*currDir.y), 0);
                
                targetPos = mazeMap.GetCellCenterWorld(nextTile);
            }
            else
            {
                objectAnimator.SetPlaying(false);
            }
        }
        else
        {
            transform.Translate(toTargetDir.normalized*speed*Time.deltaTime);
        }
    }

    private void Reset()
    {
        currDir = Vector2.right;
        bufferedDir = Vector2.zero;
        targetPos = transform.position;

        Vector3Int tileIndex = mazeMap.WorldToCell(transform.position + (Vector3)currDir);
        if (!mazeMap.HasTile(tileIndex))
            targetPos = mazeMap.GetCellCenterWorld(tileIndex);
        else
            currDir = Vector2.zero;
        
        if (objectAnimator != null)
            objectAnimator.SetDirection(currDir);
    }

    private bool IsTileInDir(Vector2 dir)
    {
        // assumes tiles are 1 unit in diameter
        Vector3 truncatedPos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
        Vector3Int tileIndex = mazeMap.WorldToCell(truncatedPos + (Vector3)dir);
        return mazeMap.HasTile(tileIndex);
    }

    private void UpdateTargetAfterWarp(Vector2 boundsDir)
    {
        targetPos = (Vector2)transform.position + boundsDir;
    }
}
