using UnityEngine;

//Most of this script won't be used in our demo, as we are making a training mode basically.
//However if we want to implement actual gameplay down the line, much of this can be uncommented.
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

    [Header("Runtime State")]
    public MatchState currentState = MatchState.RoundStart;
    public float currentTime;
    public int roundsWonA;
    public int roundsWonB;

    private Vector3 startPosA;
    private Vector3 startPosB;
    private float stateTimer;

    void Start()
    {
        if (fighterA == null || fighterB == null)
        {
            Debug.LogError("GameManager is missing a fighter reference - assign both in the Inspector.");
            enabled = false;
            return;
        }

        fighterA.SetOpponent(fighterB.transform);
        fighterB.SetOpponent(fighterA.transform);

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
                break;
        }
        
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
            Debug.Log(fighterA.name + " wins the round.");
        }
        else if (fighterB.currentHealth > fighterA.currentHealth)
        {
            roundsWonB++;
            Debug.Log(fighterB.name + " wins the round.");
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

    //Disables Movement and AttackController during round start and round end
    //pauses so neither player can act just before or after a round (normal fighting game round start/end protocol).
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
