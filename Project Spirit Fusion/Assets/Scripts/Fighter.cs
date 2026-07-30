using UnityEngine;
using UnityEngine.InputSystem;

public enum FighterState
{
    Idle,
    Moving,
    Crouching,
    Jumping,
    Attacking,
    Hitstun,
    Blockstun,
    Knockdown,
    Dead
}

//Once sprites are implemented, an animation FSM will need to be added here for fighter state, as well as for attacks.

public class Fighter : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 1000;
    public int currentHealth;

    [Header("Fighter State")]
    public FighterState currentState = FighterState.Idle;

    //Tracked separately from currentState on purpose - Movement keeps this up
    //to date every physics step regardless of what currentState is doing, so
    //"was I crouching/jumping" survives even while currentState is Attacking.
    [Header("Attack Stance")]
    public AttackStance currentStance = AttackStance.Standing;

    [Header("Facing Direction")]
    public int facingDir = 1;

    //Set by Movement script every frame so other systems can check airborne status.
    //public bool isAirborne = false;

    private Transform opponentDir;

    //Frame-based stun timers. Decremented in FixedUpdate, one tick per physics step.
    private int hitstunFrames = 0;
    private int blockstunFrames = 0;
    private int knockdownFrames = 0;

    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
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
                SetState(FighterState.Idle);
        }

        if (currentState == FighterState.Blockstun)
        {
            blockstunFrames--;
            if (blockstunFrames <= 0)
                SetState(FighterState.Idle);
        }

        if (currentState == FighterState.Knockdown)
        {
            knockdownFrames--;
            if (knockdownFrames <= 0)
                SetState(FighterState.Idle);
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

        //---If attack isn't blocked---

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

    //Checks if you are blocking: holding away from the opponent, while in a
    //neutral standing/crouching state (not mid-attack, not already stunned,
    //not airborne), and the attack's type is blockable from your current
    //stance (Low only blocks crouching, Overhead only blocks standing).
    public bool IsBlocking(AttackData.AttackType attackType)
    {
        if (opponentDir == null) return false;

        //Can't block in the air, and can't block while already doing
        //something else (attacking, in hitstun/blockstun/knockdown, dead).
        if (currentStance == AttackStance.Jumping) return false;
        if (!IsNeutralState()) return false;

        bool holdingAway = facingDir == 1
            ? Keyboard.current.aKey.isPressed
            : Keyboard.current.dKey.isPressed;

        if (!holdingAway) return false;

        switch (attackType)
        {
            case AttackData.AttackType.Low:
                return currentStance == AttackStance.Crouching;
            case AttackData.AttackType.Overhead:
                return currentStance == AttackStance.Standing;
            default: //High
                return true;
        }
    }

    //Neutral = free to act and not mid-attack. Used by IsBlocking so whiffed
    //attacks (currentState == Attacking) can't also count as blocking.
    bool IsNeutralState()
    {
        return currentState == FighterState.Idle
            || currentState == FighterState.Moving
            || currentState == FighterState.Crouching;
    }

    //Only method that can change state to prevent accidental changes.
    public void SetState(FighterState newState)
    {
        currentState = newState;
    }

    //Same pattern as SetState, but for stance. Called by Movement every
    //physics step (see Movement.UpdateAttackStance), not by AttackController.
    public void SetStance(AttackStance newStance)
    {
        currentStance = newStance;
    }

    //Useful checking method for other scripts
    public bool CanAct()
    {
        //Fighter CAN act if...
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