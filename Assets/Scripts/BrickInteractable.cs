using UnityEngine;

public class BrickInteractable : MonoBehaviour
{
    public bool containsCoin;
    public Animator animator;
    public SpriteRenderer coinRenderer;
    public AudioSource audioSource;
    public AudioClip bumpSound;
    public AudioClip coinSound;

    [System.NonSerialized]
    public bool coinCollected;

    private bool bouncing;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (bouncing || !collision.gameObject.CompareTag("Player") ||
            collision.transform.position.y >= transform.position.y)
        {
            return;
        }

        for (int index = 0; index < collision.contactCount; index++)
        {
            if (Mathf.Abs(collision.GetContact(index).normal.y) > 0.5f)
            {
                bouncing = true;
                audioSource.PlayOneShot(bumpSound);
                string animationName = containsCoin && !coinCollected ? "brick-coin" : "brick-bounce";
                animator.Play(animationName, 0, 0f);
                return;
            }
        }
    }

    // Called by an Animation Event when the coin returns to the brick.
    public void CollectCoin()
    {
        if (!coinCollected)
        {
            coinCollected = true;
            audioSource.PlayOneShot(coinSound);
        }
    }

    // Called by the final Animation Event so another hit can bounce the brick once.
    public void FinishBounce()
    {
        bouncing = false;
        Color coinColor = coinRenderer.color;
        coinColor.a = 0f;
        coinRenderer.color = coinColor;
        animator.Play("brick-idle", 0, 0f);
    }
}
