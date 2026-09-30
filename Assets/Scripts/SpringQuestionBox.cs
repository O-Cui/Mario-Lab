using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class SpringQuestionBox : MonoBehaviour
{
    public Sprite emptySprite;

    private bool used;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (used || !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (collision.transform.position.y >= transform.position.y)
        {
            return;
        }

        for (int index = 0; index < collision.contactCount; index++)
        {
            if (Mathf.Abs(collision.GetContact(index).normal.y) > 0.5f)
            {
                GetComponent<SpriteRenderer>().sprite = emptySprite;
                used = true;
                return;
            }
        }
    }
}
