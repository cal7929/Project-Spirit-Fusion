using UnityEngine;

public enum FighterState
{
    Idle,
    Moving,
    Jumping,
    Coruching,
    Dashing,
    Attacking,
    Hitstun,
    Blockstun,
    Knockdown,
    Dead
}

public class Fighter : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 1000;
    public int currentHealth;

    [Header("Fighter State")]
    public FighterState currentState = FighterState.Idle;

    //Stance and state are seperate and used for different purposes
    [Header("Attack Stance")]
    public AttackStance currentStance = AttackStance.Standing;

    [Header("Facing Direction")]
    public int facingDir = 1;

    private Transform opponentDir;

    //Frame based stun timers
    private int hitstunFrames = 0;
    private int blockstunFrames = 0;
    private int knockdownFrames = 0;

    private Rigidbody2D rb;

    private InputReader inputReader;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        inputReader = GetComponent<InputReader>();
    }

    void Update()
    {
        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (currentState == FighterState.Hitstun)
        {
            hitstunFrames--;
            if (hitstunFrames <= 0)
            {
                SetState(FighterState.Idle);
            }
        }

        if (currentState == FighterState.Blockstun)
        {
            blockstunFrames--;
            if (blockstunFrames <= 0)
            {
                SetState(FighterState.Idle);
            }
        }

        if (currentState == FighterState.Knockdown)
        {
            knockdownFrames--;
            if (knockdownFrames <= 0)
            {
                SetState(FighterState.Idle);
            }
        }
    }

    //Chip damage from specials will be in here later.
    public void TakeDamage(int damage, int hitstun, Vector2 knockback, AttackData.AttackType attackType)
    {
        if (currentState == FighterState.Dead) return;

        if (IsBlocking(attackType))
        {
            //Blocked hits do no damage, half hitstun as blockstun, softer pushback.
            SetState(FighterState.Blockstun);
            blockstunFrames = hitstun / 2;
            rb.linearVelocity = knockback / 2;

            Debug.Log(gameObject.name + " blocked. HP: " + currentHealth);
            return;
        }

        //If attack isn't blocked

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log(gameObject.name + " took " + damage + " damage. HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            SetState(FighterState.Dead);
            rb.linearVelocity = Vector2.zero;
            Debug.Log(gameObject.name + "'s spirit has been slain...");
            return;
        }

        SetState(FighterState.Hitstun);
        hitstunFrames = hitstun;
        rb.linearVelocity = knockback;
    }

    //Checks if you are blocking: meaning you are holding away from the opponent,
    //while in a a neutral state in the correct stance to block your opponent's attack.
    public bool IsBlocking(AttackData.AttackType attackType)
    {
        if (opponentDir == null) return false;
        if (currentStance == AttackStance.Jumping) return false;
        if (!IsNeutralState()) return false;

        //Backward always means away from the opponent regardless of which side you're on.
        bool holdingAway = InputReader.IsBackward(inputReader.Latest.direction);

        if (!holdingAway) return false;

        switch (attackType)
        {
            case AttackData.AttackType.Low:
                return currentStance == AttackStance.Crouching;
            case AttackData.AttackType.Overhead:
                return currentStance == AttackStance.Standing;
            default:
                return true;
        }
    }

    //Neutral means you are free to act and not mid-attack. 
    bool IsNeutralState()
    {
        return currentState == FighterState.Idle
            || currentState == FighterState.Moving;
    }

    //Only method that can change state to prevent accidental changes.
    public void SetState(FighterState newState)
    {
        currentState = newState;
    }

    //Same pattern as SetState, but for stance. Called by Movement every
    //physics step, not by AttackController.
    public void SetStance(AttackStance newStance)
    {
        currentStance = newStance;
    }

    private AttackStance? lockedStance = null;

    //Handles effective stance so that you don't stand up mid crouch attack or crouch mid stand attack
    //(will likely not be needed once things are based on animations instead.)
    public AttackStance EffectiveStance => lockedStance ?? currentStance;

    //Called by AttackController when an attack starts, locking
    //EffectiveStance to that attack's own requiredStance for its whole
    //duration.
    public void LockStance(AttackStance stance)
    {
        lockedStance = stance;
    }

    //Called by AttackController when an attack ends without being replaced
    //by a new one, releasing EffectiveStance back to tracking live input.
    public void UnlockStance()
    {
        lockedStance = null;
    }

    //Useful checking method for other scripts
    public bool CanAct()
    {
        //Fighter can act if:
        return currentState != FighterState.Hitstun
            && currentState != FighterState.Blockstun
            && currentState != FighterState.Knockdown
            && currentState != FighterState.Dead;
    }

    public void SetOpponent(Transform opponentTransform)
    {
        opponentDir = opponentTransform;
    }

    void UpdateFacing()
    {
        if (opponentDir == null) return;

        //1 = Facing Right, -1 = Facing Left
        if (opponentDir.position.x > transform.position.x)
        {
            facingDir = 1;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            facingDir = -1;
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
}