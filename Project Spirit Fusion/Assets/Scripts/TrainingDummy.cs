using UnityEngine;
using UnityEngine.EventSystems;

public class TrainingDummy : MonoBehaviour
{
    [Header("Auto Reset")]
    public bool autoReset = true;
    public float resetDelay = 1.5f;

    private Fighter fighter;
    private float deadTimer;
    private Vector3 startPosition;

    void Start()
    {
        fighter = GetComponent<Fighter>();
        startPosition = transform.position;
    }

    void Update()
    {
        if (!autoReset) return;

        if (fighter.currentState == FighterState.Dead)
        {
            deadTimer += Time.deltaTime;
            if (deadTimer >= resetDelay)
            {
                ResetDummy();
            }
        }
        else
        {
            deadTimer = 0f;
        }
    }

    void ResetDummy()
    {
        fighter.currentHealth = fighter.maxHealth;
        fighter.SetState(FighterState.Idle);
        transform.position = startPosition;
        deadTimer = 0f;
    }
}
