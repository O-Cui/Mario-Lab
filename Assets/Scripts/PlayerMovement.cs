using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 30f;
    public float maxSpeed = 6.5f;
    public float upSpeed = 9.5f;

    public TextMeshProUGUI scoreText;
    public GameObject enemies;
    public JumpOverGoomba jumpOverGoomba;
    public GameObject gameOverPanel;

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
    }

    private void Update()
    {
        if (Input.GetKeyDown("a") && faceRightState)
        {
            faceRightState = false;
            marioSprite.flipX = true;
        }

        if (Input.GetKeyDown("d") && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
        }
    }

    private void FixedUpdate()
    {
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
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGroundState = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Enemy"))
        {
            return;
        }

        Debug.Log("Collided with goomba!");
        marioBody.linearVelocity = Vector2.zero;
        TextMeshProUGUI gameOverText = gameOverPanel != null
            ? gameOverPanel.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        if (gameOverText != null)
        {
            int finalScore = jumpOverGoomba != null ? jumpOverGoomba.score : 0;
            gameOverText.text = "GAME OVER\nScore: " + finalScore + "\nPRESS RESTART";
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        Time.timeScale = 0f;
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
    }

    private void StopHorizontalMovement()
    {
        Vector2 velocity = marioBody.linearVelocity;
        velocity.x = 0f;
        marioBody.linearVelocity = velocity;
    }
}
