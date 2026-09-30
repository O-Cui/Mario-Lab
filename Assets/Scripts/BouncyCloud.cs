using UnityEngine;

public class BouncyCloud : MonoBehaviour
{
    public float bounceMultiplier = 2f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerBody = collision.rigidbody;
        if (playerBody == null)
        {
            return;
        }

        Vector2 velocity = playerBody.linearVelocity;
        velocity.y = Mathf.Abs(velocity.y) * bounceMultiplier;
        playerBody.linearVelocity = velocity;
    }
}
