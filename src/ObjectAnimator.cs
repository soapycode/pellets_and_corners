using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectAnimator : MonoBehaviour
{
    public Sprite[] spritesFrame0;
    public Sprite[] spritesFrame1;

    public SpriteRenderer spriteRenderer;

    private enum SpriteDir {
        DOWN, RIGHT, LEFT, UP
    };

    private GameManager gameManager;

    private SpriteDir facingDir;

    private const float ANIMATION_DELAY = 0.1f;
    private float animationCounter;

    private int currFrame;

    private bool isAnimating;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(spritesFrame0.Length == 4);
        Debug.Assert(spritesFrame1.Length == 4);

        facingDir = SpriteDir.RIGHT;
        animationCounter = 0.0f;
        currFrame = 0;
        isAnimating = true;

        gameManager = GameObject.FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.Paused) return;
        
        if (isAnimating)
            animationCounter += Time.deltaTime;

        if (animationCounter >= ANIMATION_DELAY)
        {
            currFrame = (currFrame + 1) % 2;
            animationCounter = 0.0f;
        }

        UpdateSprite();
    }

    private void UpdateSprite() {
        if (currFrame == 0)
            spriteRenderer.sprite = spritesFrame0[(int)facingDir];
        else if (currFrame == 1)
            spriteRenderer.sprite = spritesFrame1[(int)facingDir];
    }

    public void SetDirection(Vector2 dir)
    {
        if (dir.y < 0)
            facingDir = SpriteDir.DOWN;
        else if (dir.y > 0)
            facingDir = SpriteDir.UP;
        else if (dir.x < 0)
            facingDir = SpriteDir.LEFT;
        else if (dir.x > 0)
            facingDir = SpriteDir.RIGHT;
        
        UpdateSprite();
    }

    public void SetPlaying(bool isPlaying)
    {
        isAnimating = isPlaying;
        
        if (!isPlaying)
        {
            currFrame = 0;
            animationCounter = 0;
        }
    }
}
