using UnityEngine;

public class BlackHole : MonoBehaviour
{
    public bool scoreable = true;
    [SerializeField] GameObject deathParticles;
    [SerializeField] Animator animator;
    CircleCollider2D col;

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();
    }

    bool AnimatorIsPlaying() { return animator.GetCurrentAnimatorStateInfo(0).length > animator.GetCurrentAnimatorStateInfo(0).normalizedTime; }

    private void Update()
    {
        if (!scoreable)
        {          
            animator.Play("DeathAnimation");

            if(transform.localScale.x <= 0)
                Disable();

            if (!AnimatorIsPlaying())
            {
                deathParticles.SetActive(true);
            }
        }
    }

    private void Disable()
    {
        col.enabled = false;
    }
}
