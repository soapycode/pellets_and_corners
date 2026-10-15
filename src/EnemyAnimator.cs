using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private const float FRAME_TIME = 0.2f;

    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Sprite[] deadSprites;
    [SerializeField] private int initialSpriteIndex;
    [SerializeField] private Sprite frightenedSprite;
    [SerializeField] private Sprite blinkingSprite;

    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;

    private SpriteDir facing;
    private uint currFrame;
    private bool frightened;
    private bool blinking;
    private bool dead;
    private float frameTimer;

    private enum SpriteDir
    {
        DOWN, LEFT, RIGHT, UP
    };

    public void ToggleFrightenedState(bool newValue)
    {
        frightened = newValue;

        if (frightened)
            currFrame = 0;
    }

    public void ToggleBlinkingState(bool newValue)
    {
        blinking = newValue;

        if (!blinking)
            currFrame = 0;
    }

    public void ToggleDeadState(bool newValue)
    {
        dead = newValue;
        ToggleFrightenedState(false);
    }

    public void SetDirection(Vector3 dir)
    {
        if (dir.y < 0)
            facing = SpriteDir.DOWN;
        else if (dir.y > 0)
            facing = SpriteDir.UP;
        else if (dir.x < 0)
            facing = SpriteDir.LEFT;
        else if (dir.x > 0)
            facing = SpriteDir.RIGHT;
    }

    void Start()
    {
        Debug.Assert(sprites.Length == 4);
        Debug.Assert(deadSprites.Length == 4);
        Debug.Assert(initialSpriteIndex >= 0 && initialSpriteIndex < sprites.Length);
        Debug.Assert(transform.childCount == 1);

        gameManager = FindObjectOfType<GameManager>();
        spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();

        gameManager.LevelReset += Reset;

        Reset();
    }

    void Update()
    {
        if (dead)
            spriteRenderer.sprite = deadSprites[(int)facing];
        else if (frightened && !gameManager.Paused)
        {
            if (blinking)
                frameTimer += Time.deltaTime;

            if (blinking && frameTimer >= FRAME_TIME)
            {
                currFrame = (currFrame + 1) % 2;
                frameTimer = 0.0f;
            }

            if (currFrame == 1)
                spriteRenderer.sprite = blinkingSprite;
            else
                spriteRenderer.sprite = frightenedSprite;
        }
        else if (!gameManager.Paused)
            spriteRenderer.sprite = sprites[(int)facing];
    }

    private void Reset()
    {
        spriteRenderer.sprite = sprites[initialSpriteIndex];
        facing = (SpriteDir)initialSpriteIndex;
        frightened = false;
        dead = false;
        frameTimer = 0.0f;
        currFrame = 0;
    }
}
