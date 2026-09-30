using UnityEngine;

public class QuestionBoxHitRelay : MonoBehaviour
{
    public QuestionBoxInteractable questionBox;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        questionBox.HitFromBelow(collision);
    }
}
