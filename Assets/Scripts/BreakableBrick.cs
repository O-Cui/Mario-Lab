using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class BreakableBrick : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player") || collision.transform.position.y >= transform.position.y)
        {
            return;
        }

        for (int index = 0; index < collision.contactCount; index++)
        {
            if (Mathf.Abs(collision.GetContact(index).normal.y) > 0.5f)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
