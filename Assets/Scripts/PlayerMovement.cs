using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 30f;
    public float maxSpeed = 6.5f;
    public float upSpeed = 9.5f;

    public TextMeshProUGUI scoreText;
    public GameObject enemies;
    public JumpOverGoomba jumpOverGoomba;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreText;
    public Transform gameCamera;

    // Animation and audio components configured on Mario.
    public Animator marioAnimator;
    public AudioSource marioAudio;
    public AudioClip marioDeath;
    public float deathImpulse = 15f;

    [System.NonSerialized]
    public bool alive = true;

    private Rigidbody2D marioBody;
    private SpriteRenderer marioSprite;
    private bool faceRightState = true;
    private bool onGroundState = true;
    private Vector3 startPosition;

    public bool IsGrounded => onGroundState;

    private void Start()
    {
        Application.targetFrameRate = 30;
        Time.timeScale = 1f;
        marioBody = GetComponent<Rigidbody2D>();
        marioSprite = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        marioAnimator.SetBool("onGround", onGroundState);
    }

    private void Update()
    {
        if (!alive)
        {
            return;
        }

        if (Input.GetKeyDown("a") && faceRightState)
        {
            faceRightState = false;
            marioSprite.flipX = true;
            if (marioBody.linearVelocity.x > 0.1f)
            {
                marioAnimator.SetTrigger("onSkid");
            }
        }

        if (Input.GetKeyDown("d") && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
            if (marioBody.linearVelocity.x < -0.1f)
            {
                marioAnimator.SetTrigger("onSkid");
            }
        }

        marioAnimator.SetFloat("xSpeed", Mathf.Abs(marioBody.linearVelocity.x));
    }

    private void FixedUpdate()
    {
        if (!alive)
        {
            return;
        }

        float moveHorizontal = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(moveHorizontal) > 0f)
        {
            Vector2 movement = new Vector2(moveHorizontal, 0f);
            if (marioBody.linearVelocity.magnitude < maxSpeed)
            {
                marioBody.AddForce(movement * speed);
            }
        }

        if (Input.GetKeyUp("a") || Input.GetKeyUp("d"))
        {
            StopHorizontalMovement();
        }

        if (Input.GetKeyDown("space") && onGroundState)
        {
            marioBody.AddForce(Vector2.up * upSpeed, ForceMode2D.Impulse);
            onGroundState = false;
            marioAnimator.SetBool("onGround", onGroundState);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !onGroundState)
        {
            onGroundState = true;
            marioAnimator.SetBool("onGround", onGroundState);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Enemy") || !alive)
        {
            return;
        }

        Debug.Log("Collided with goomba!");
        marioAnimator.Play("mario-die", 0, 0f);
        marioAudio.PlayOneShot(marioDeath);
        alive = false;
    }

    public void RestartButtonCallback(int input)
    {
        Debug.Log("Restart!");
        ResetGame();
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void ResetGame()
    {
        marioBody.transform.position = startPosition;
        marioBody.linearVelocity = Vector2.zero;
        marioBody.angularVelocity = 0f;
        faceRightState = true;
        marioSprite.flipX = false;
        onGroundState = true;
        marioAnimator.SetBool("onGround", onGroundState);
        marioAnimator.SetFloat("xSpeed", 0f);
        marioAnimator.ResetTrigger("onSkid");
        marioAnimator.SetTrigger("gameRestart");
        alive = true;

        if (scoreText != null)
        {
            scoreText.text = "Score: 0";
        }

        if (enemies != null)
        {
            foreach (Transform eachChild in enemies.transform)
            {
                EnemyMovement enemyMovement = eachChild.GetComponent<EnemyMovement>();
                if (enemyMovement != null)
                {
                    eachChild.localPosition = enemyMovement.startPosition;
                    enemyMovement.ResetEnemy();
                }
            }
        }

        if (jumpOverGoomba != null)
        {
            jumpOverGoomba.score = 0;
            jumpOverGoomba.ResetScore();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "FINAL SCORE\n000000";
        }

        gameCamera.position = new Vector3(0f, 0f, -10f);
    }

    private void StopHorizontalMovement()
    {
        Vector2 velocity = marioBody.linearVelocity;
        velocity.x = 0f;
        marioBody.linearVelocity = velocity;
    }

    // Invoked by an Animation Event at the start of mario-jump.
    public void PlayJumpSound()
    {
        marioAudio.PlayOneShot(marioAudio.clip);
    }

    // Invoked by an Animation Event near the start of mario-die.
    public void PlayDeathImpulse()
    {
        marioBody.AddForce(Vector2.up * deathImpulse, ForceMode2D.Impulse);
    }

    // Invoked by the final Animation Event in mario-die.
    public void GameOverScene()
    {
        if (gameOverScoreText != null)
        {
            int finalScore = jumpOverGoomba != null ? jumpOverGoomba.score : 0;
            gameOverScoreText.text = "FINAL SCORE\n" + finalScore.ToString("D6");
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}
