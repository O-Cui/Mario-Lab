using UnityEngine;

public class QuestionBoxInteractable : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D questionBoxBody;
    public Transform questionBoxTransform;
    public SpriteRenderer questionBoxRenderer;
    public SpriteRenderer coinRenderer;
    public Sprite disabledSprite;
    public AudioSource audioSource;
    public AudioClip coinSound;

    [System.NonSerialized]
    public bool used;

    public void HitFromBelow(Collision2D collision)
    {
        if (used || !collision.gameObject.CompareTag("Player") ||
            collision.transform.position.y >= questionBoxTransform.position.y)
        {
            return;
        }

        for (int index = 0; index < collision.contactCount; index++)
        {
            if (Mathf.Abs(collision.GetContact(index).normal.y) > 0.5f)
            {
                used = true;
                animator.Play("question-box-coin", 0, 0f);
                return;
            }
        }
    }

    // Called by the final Animation Event after the coin returns to the box.
    public void FinishQuestionBox()
    {
        audioSource.PlayOneShot(coinSound);
        questionBoxRenderer.sprite = disabledSprite;
        questionBoxBody.bodyType = RigidbodyType2D.Static;
        questionBoxBody.linearVelocity = Vector2.zero;
        questionBoxTransform.localPosition = Vector3.zero;

        Color coinColor = coinRenderer.color;
        coinColor.a = 0f;
        coinRenderer.color = coinColor;
    }
}
