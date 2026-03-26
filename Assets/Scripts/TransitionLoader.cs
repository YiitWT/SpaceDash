using UnityEngine;

public class TransitionLoader : MonoBehaviour
{
    public bool isStarter;
    public bool isEnding;

    [Header("Clips")]
    public AnimationClip transitionStartClip;
    public AnimationClip transitionEndClip;

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        gameObject.SetActive(false);
    }

    public void AnimateTransition()
    {
        if (animator == null)
        {
            Debug.LogError("Animator component is missing on TransitionLoader object.");
            return;
        }

        gameObject.SetActive(true);

        if (isStarter)
        {
            if (transitionStartClip == null)
            {
                Debug.LogError("transitionStartClip is not assigned.");
                return;
            }

            animator.Play(transitionStartClip.name);
        }
        else if (isEnding)
        {
            if (transitionEndClip == null)
            {
                Debug.LogError("transitionEndClip is not assigned.");
                return;
            }

            animator.Play(transitionEndClip.name);
        }
        else
        {
            Debug.LogError("Enable either isStarter or isEnding.");
        }
    }
}