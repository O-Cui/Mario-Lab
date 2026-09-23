using TMPro;
using UnityEngine;

public class JumpOverGoomba : MonoBehaviour
{
    public Transform enemyLocation;
    public TextMeshProUGUI scoreText;
    private bool onGroundState;

    [System.NonSerialized]
    public int score = 0;

    private bool countScoreState = false;
    public Vector3 boxSize;
    public float maxDistance;
    public LayerMask layerMask;

    private void Start()
    {
        onGroundState = OnGroundCheck();
        RefreshScoreText();
    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown("space") && OnGroundCheck())
        {
            onGroundState = false;
            countScoreState = true;
        }

        if (!onGroundState && countScoreState && enemyLocation != null)
        {
            if (Mathf.Abs(transform.position.x - enemyLocation.position.x) < 0.5f)
            {
                countScoreState = false;
                score++;
                RefreshScoreText();
                Debug.Log(score);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGroundState = true;
        }
    }

    public bool OnGroundCheck()
    {
        return Physics2D.BoxCast(transform.position, boxSize, 0f, -transform.up, maxDistance, layerMask);
    }

    public void ResetScore()
    {
        score = 0;
        countScoreState = false;
        onGroundState = OnGroundCheck();
        RefreshScoreText();
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(transform.position - transform.up * maxDistance, boxSize);
    }
}
