using UnityEngine;

public enum MatchState
{
    RoundStart,
    Fighting,
    RoundEnd,
    MatchEnd
}

public class GameManager : MonoBehaviour
{
    [Header("Fighters")]
    public Fighter fighterA;
    public Fighter fighterB;

    [Header("Round Settings")]
    public float roundTime = 99f;
    public int roundsToWin = 2;
    public float roundStartDelay = 1f;
    public float roundEndDelay = 2f;

    //Prevents fighters from colliding with eachother in awkward ways, also prevents them from standing on eachother
    [Header("Pushbox Settings")]
    public float pushboxWidth = 1f;
    public float pushboxHeightTolerance = 1.5f;

    [Header("Runtime State")]
    public MatchState currentState = MatchState.RoundStart;
    public float currentTime;
    public int roundsWonA;
    public int roundsWonB;

    private Vector3 startPosA;
    private Vector3 startPosB;
    private float stateTimer;

    //Cached for pushbox use in FixedUpdate (avoids GetComponent every frame).
    private Rigidbody2D rbA;
    private Rigidbody2D rbB;

    void Start()
    {
        if (fighterA == null || fighterB == null)
        {
            Debug.LogError("GameManager is missing a fighter reference, assign both in the Inspector.");
            enabled = false;
            return;
        }

        fighterA.SetOpponent(fighterB.transform);
        fighterB.SetOpponent(fighterA.transform);

        //Fighters body colliders should not physically interact with each other.
        //Hits are still detected by the separate hitbox system.
        IgnoreCollisionBetween(fighterA, fighterB);

        rbA = fighterA.GetComponent<Rigidbody2D>();
        rbB = fighterB.GetComponent<Rigidbody2D>();

        startPosA = fighterA.transform.position;
        startPosB = fighterB.transform.position;

        StartRound();
    }

    void Update()
    {
        switch (currentState)
        {
            case MatchState.RoundStart:
                TickRoundStart();
                break;
            case MatchState.Fighting:
                TickFighting();
                break;
            case MatchState.RoundEnd:
                TickRoundEnd();
                break;
            case MatchState.MatchEnd:
                //Once more detail is implemented things will go here (graphics etc.)
                break;
        }
    }

    void FixedUpdate()
    {
        ResolvePushbox();
    }

    void TickRoundStart()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            SetFightersEnabled(true);
            currentState = MatchState.Fighting;
        }
    }

    void TickFighting()
    {
        currentTime -= Time.deltaTime;

        bool aDead = fighterA.currentState == FighterState.Dead;
        bool bDead = fighterB.currentState == FighterState.Dead;

        if (aDead || bDead || currentTime <= 0f)
        {
            EndRound(aDead, bDead);
        }
    }

    void EndRound(bool aDead, bool bDead)
    {
        SetFightersEnabled(false);
        currentState = MatchState.RoundEnd;
        stateTimer = roundEndDelay;

        if (aDead && bDead)
        {
            Debug.Log("Double KO, round draw.");
        }
        else if (aDead)
        {
            roundsWonB++;
            Debug.Log(fighterB.name + " wins the round.");
        }
        else if (bDead)
        {
            roundsWonA++;
            Debug.Log(fighterA.name + " wins the round.");
        }
        else if (fighterA.currentHealth > fighterB.currentHealth)
        {
            roundsWonA++;
            Debug.Log("Time's Up, " + fighterA.name + " wins the round.");
        }
        else if (fighterB.currentHealth > fighterA.currentHealth)
        {
            roundsWonB++;
            Debug.Log("Time's Up, " + fighterB.name + " wins the round.");
        }
        else
        {
            Debug.Log("Time-out, round draw.");
        }
    }

    void TickRoundEnd()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        if (roundsWonA >= roundsToWin || roundsWonB >= roundsToWin)
        {
            currentState = MatchState.MatchEnd;
            string winnerName = roundsWonA > roundsWonB ? fighterA.name : fighterB.name;
            Debug.Log(winnerName + " wins the match!");
        }
        else
        {
            StartRound();
        }
    }

    void StartRound()
    {
        ResetFighter(fighterA, startPosA);
        ResetFighter(fighterB, startPosB);

        SetFightersEnabled(false);
        currentTime = roundTime;
        stateTimer = roundStartDelay;
        currentState = MatchState.RoundStart;
    }

    void ResetFighter(Fighter fighter, Vector3 startPos)
    {
        fighter.currentHealth = fighter.maxHealth;
        fighter.SetState(FighterState.Idle);
        fighter.transform.position = startPos;

        Rigidbody2D rb = fighter.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    //Prevents fighters from overlapping horizontally.
    //Skips when one fighter is clearly jumping over the other (vertical gap check), or when either fighter is dead/knocked down.
    void ResolvePushbox()
    {
        if (rbA == null || rbB == null) return;
        if (fighterA.currentState == FighterState.Dead || fighterB.currentState == FighterState.Dead) return;
        if (fighterA.currentState == FighterState.Knockdown || fighterB.currentState == FighterState.Knockdown) return;

        Vector2 posA = rbA.position;
        Vector2 posB = rbB.position;

        //Skip the push if one fighter is mid-jump.
        float verticalGap = Mathf.Abs(posA.y - posB.y);
        if (verticalGap > pushboxHeightTolerance) return;

        float horizontalGap = posB.x - posA.x;
        float overlap = pushboxWidth - Mathf.Abs(horizontalGap);
        if (overlap <= 0f) return;

        //Push both fighters equally away from each other.
        float pushDir = horizontalGap >= 0f ? 1f : -1f;
        float pushAmount = overlap * 0.5f;

        rbA.position = posA - new Vector2(pushDir * pushAmount, 0f);
        rbB.position = posB + new Vector2(pushDir * pushAmount, 0f);
    }

    void IgnoreCollisionBetween(Fighter a, Fighter b)
    {
        Collider2D colA = a.GetComponent<Collider2D>();
        Collider2D colB = b.GetComponent<Collider2D>();

        if (colA != null && colB != null)
            Physics2D.IgnoreCollision(colA, colB);
    }

    //Disables Movement and AttackController during round start and round end
    void SetFightersEnabled(bool isEnabled)
    {
        SetFighterEnabled(fighterA, isEnabled);
        SetFighterEnabled(fighterB, isEnabled);
    }

    void SetFighterEnabled(Fighter fighter, bool isEnabled)
    {
        Movement movement = fighter.GetComponent<Movement>();
        if (movement != null) movement.enabled = isEnabled;

        AttackController attackController = fighter.GetComponent<AttackController>();
        if (attackController != null) attackController.enabled = isEnabled;
    }
}