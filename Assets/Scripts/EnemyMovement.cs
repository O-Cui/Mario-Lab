using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class EnemyMovement : MonoBehaviour
{
    private float originalX;
    public float maxOffset = 5f;
    public float enemyPatrolTime = 2f;
    private int moveRight = -1;
    private Vector2 velocity;
    private Rigidbody2D enemyBody;

    public Vector3 startPosition { get; private set; }

    private void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
        startPosition = transform.localPosition;
        originalX = transform.position.x;
        ComputeVelocity();
    }

    private void ComputeVelocity()
    {
        velocity = new Vector2(moveRight * maxOffset / enemyPatrolTime, 0f);
    }

    private void MoveGoomba()
    {
        enemyBody.MovePosition(enemyBody.position + velocity * Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        if (Mathf.Abs(enemyBody.position.x - originalX) < maxOffset)
        {
            MoveGoomba();
        }
        else
        {
            moveRight *= -1;
            ComputeVelocity();
            MoveGoomba();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);
    }

    public void ResetEnemy()
    {
        transform.localPosition = startPosition;
        enemyBody.position = transform.position;
        enemyBody.linearVelocity = Vector2.zero;
        originalX = transform.position.x;
        moveRight = -1;
        ComputeVelocity();
    }
}
