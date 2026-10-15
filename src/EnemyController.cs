using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyController : MonoBehaviour
{
    public enum GhostState
    {
        ENTERING_GHOST_HOUSE, IN_GHOST_HOUSE, LEAVING_GHOST_HOUSE, DEAD, NORMAL
    };

    [SerializeField] private GhostState initialState = GhostState.IN_GHOST_HOUSE;
    [SerializeField] private Tilemap mazeMap;

    private const float NORMAL_SPEED = 5.0f;
    private const float SLOW_SPEED = 3.0f;
    private const float FAST_SPEED = 8.0f;

    private Vector3 targetTilePos;
    private Vector3 nextTilePos;
    private Vector3 currentDir;
    private float speed;
    private bool changeDirection; // flag to let an enemy know that it should change direction at the next tile
    private bool frightened;

    private GameManager gameManager;
    private IEnemyPersonality enemyPersonality;
    private EnemyAnimator animator;

    // the ghost's state is separate from the enemy mode in the game manager, and is used to describe the action the
    // ghost is currently performing
    private GhostState state;

    public GhostState InitialState => initialState;
    public GhostState State => state;
    public bool Killable => frightened;

    public void Die()
    {
        state = GhostState.DEAD;
        speed = FAST_SPEED;
        animator.ToggleDeadState(true);
        frightened = false;
    }

    public void EnterGhostHouse()
    {
        animator.ToggleDeadState(false);
        state = GhostState.IN_GHOST_HOUSE;
        speed = SLOW_SPEED;
    }
    public void LeaveGhostHouse()
    {
        state = GhostState.LEAVING_GHOST_HOUSE;
        speed = SLOW_SPEED;
    }

    public void EnemyModeChanged(GameManager.EnemyMode newMode, GameManager.EnemyMode prevMode)
    {        
        if (prevMode == GameManager.EnemyMode.CHASE || prevMode == GameManager.EnemyMode.SCATTER)
            changeDirection = true;
        
        if (prevMode == GameManager.EnemyMode.FRIGHTENED)
        {
            frightened = false;
            speed = NORMAL_SPEED;
            animator.ToggleFrightenedState(false);
            animator.ToggleBlinkingState(false);
        }
        else if (newMode == GameManager.EnemyMode.FRIGHTENED && state != GhostState.DEAD)
        {
            frightened = true;
            speed = SLOW_SPEED;
            animator.ToggleFrightenedState(true);
        }
    }

    void Start()
    {
        gameManager = GameObject.FindObjectOfType<GameManager>();
        enemyPersonality = GetComponent<IEnemyPersonality>();
        animator = GetComponent<EnemyAnimator>();

        GetComponent<ScreenWrap>().BoundsEntered += UpdateTargetAfterWarp;

        gameManager.LevelReset += Reset;
        Reset();
    }

    void Update()
    {
        if (gameManager.Paused || state == GhostState.IN_GHOST_HOUSE) return;

        switch(state)
        {
            case GhostState.ENTERING_GHOST_HOUSE:
            {
                // NOTE: we assume that when we enter this state, the enemy is positioned at the ghost house exit

                float frameDistance = speed*Time.deltaTime;

                float yDiff = gameManager.GhostHousePos.y - transform.position.y;
                if (Mathf.Abs(yDiff) <= frameDistance)
                {
                    transform.position = gameManager.GhostHousePos;

                    // TODO: move to the appropriate x position in the ghost house
                    
                    EnterGhostHouse();
                }
                else
                {
                    transform.Translate(Vector3.down*frameDistance);
                    animator.SetDirection(Vector3.down);
                }
            } break;

            case GhostState.LEAVING_GHOST_HOUSE:
            {
                Vector2 exitTile = gameManager.GhostExitPos;
                targetTilePos = exitTile;

                Vector3Int currTile = CurrentTileIndex();

                const float positionError = 0.01f;
                float xDiff = targetTilePos.x - transform.position.x;

                float frameDistance = speed*Time.deltaTime;

                if (Mathf.Abs(xDiff) <= positionError) {
                    float yDiff = targetTilePos.y - transform.position.y;
                    
                    if (yDiff < frameDistance)
                    {
                        transform.position = targetTilePos;
                        currentDir = Vector3.left;
                        nextTilePos = mazeMap.GetCellCenterWorld(CurrentTileIndex() + new Vector3Int((int)currentDir.x, (int)currentDir.y, 0));

                        state = GhostState.NORMAL;

                        if (frightened)
                            speed = SLOW_SPEED;
                        else
                            speed = NORMAL_SPEED;
                    }
                    else
                    {
                        transform.Translate(Vector3.up*frameDistance);
                    }

                    animator.SetDirection(Vector3.up);
                }
                else if (Mathf.Abs(xDiff) < frameDistance)
                {
                    Vector3 movement = new Vector3(xDiff, frameDistance - Mathf.Abs(xDiff), 0);
                    transform.Translate(movement);
                }
                else
                {
                    if (xDiff > 0)
                        transform.Translate(Vector3.right*frameDistance);
                    else
                        transform.Translate(Vector3.left*frameDistance);
                }
            } break;
            
            case GhostState.DEAD:
            {
                speed = FAST_SPEED;
                changeDirection = false;

                float error = 0.1f;

                if (Vector3.Distance(transform.position, gameManager.GhostExitPos) <= error)
                {
                    transform.position = gameManager.GhostExitPos;
                    state = GhostState.ENTERING_GHOST_HOUSE;
                }
                else
                {
                    // we still want to execute the normal movement code
                    goto case GhostState.NORMAL;
                }
            } break;
            
            case GhostState.NORMAL:
            {
                if (state == GhostState.DEAD)
                    targetTilePos = gameManager.GhostExitPos;
                else
                    targetTilePos = enemyPersonality.TargetPos();

                float frameDistance = speed*Time.deltaTime;
                float distanceToNextTile = Vector3.Distance(nextTilePos, transform.position);

                if (distanceToNextTile < frameDistance)
                {
                    // we have reached the next tile this frame
                    transform.position = nextTilePos;

                    frameDistance -= distanceToNextTile;

                    // decide which tile to target next

                    Vector3Int currentTile = CurrentTileIndex();
                    Vector3 nextPos = Vector3.zero;

                    if (changeDirection)
                    {
                        changeDirection = false;

                        Vector3 reversedDir = currentDir * -1.0f;
                        nextPos = mazeMap.GetCellCenterWorld(currentTile + new Vector3Int((int)reversedDir.x, (int)reversedDir.y, 0));
                    }
                    else
                    {
                        // we check tiles adjacent to our current tile and add them as candidates if they satisfy two conditions:
                        //  1. there is not a wall in the tile
                        //  2. we did not just come from that tile
                        List<Vector3> nextPosCandidates = new List<Vector3>();

                        Vector3Int leftTile = currentTile + new Vector3Int(-1, 0, 0);
                        if (!mazeMap.HasTile(leftTile) && currentDir.x != 1)
                            nextPosCandidates.Add(mazeMap.GetCellCenterWorld(leftTile));

                        Vector3Int rightTile = currentTile + new Vector3Int(1, 0, 0);
                        if (!mazeMap.HasTile(rightTile) && currentDir.x != -1)
                            nextPosCandidates.Add(mazeMap.GetCellCenterWorld(rightTile));

                        Vector3Int upTile = currentTile + new Vector3Int(0, 1, 0);
                        if (!mazeMap.HasTile(upTile) && currentDir.y != -1)
                            nextPosCandidates.Add(mazeMap.GetCellCenterWorld(upTile));

                        Vector3Int downTile = currentTile + new Vector3Int(0, -1, 0);
                        if (!mazeMap.HasTile(downTile) && currentDir.y != 1)
                            nextPosCandidates.Add(mazeMap.GetCellCenterWorld(downTile));

                        Debug.Assert(nextPosCandidates.Count != 0);

                        // from our candidate list, we choose our next pos as the tile pos with the shorteset straight line distance to our target

                        if (frightened && gameManager.CurrentEnemyMode == GameManager.EnemyMode.FRIGHTENED)
                        {
                            // in frightened mode, ghosts randomly choose their next tile
                            int randomIndex = Random.Range(0, nextPosCandidates.Count);
                            nextPos = nextPosCandidates[randomIndex];
                        }
                        else
                        {
                            float minDistance = float.PositiveInfinity;
                            nextPos = Vector3.positiveInfinity;

                            foreach (Vector3 pos in nextPosCandidates)
                            {
                                float targetDistance = Vector3.Distance(targetTilePos, pos);
                                if (targetDistance < minDistance)
                                {
                                    minDistance = targetDistance;
                                    nextPos = pos;
                                }
                            }
                        }
                    }

                    // update our tracking variables
                    nextTilePos = nextPos;
                    currentDir = (nextTilePos - transform.position).normalized;
                    animator.SetDirection(currentDir);
                }
                
                transform.Translate(currentDir*frameDistance);
            } break;
        }
    }

    private void Reset()
    {
        state = initialState;
        currentDir = Vector3.left;
        speed = NORMAL_SPEED;
        changeDirection = false;
        frightened = false;
        nextTilePos = mazeMap.GetCellCenterWorld(CurrentTileIndex() + new Vector3Int((int)currentDir.x, (int)currentDir.y, 0));
    }

    private Vector3Int CurrentTileIndex()
    {
        return mazeMap.WorldToCell(transform.position);
    }

    private void UpdateTargetAfterWarp(Vector2 boundsDir)
    {
        nextTilePos = transform.position + (Vector3)boundsDir;
    }
}
