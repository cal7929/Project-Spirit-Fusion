using UnityEngine;
using UnityEngine.InputSystem;

public enum FighterState
{
    Idle,
    Moving,
    Crouching,
    Attacking,
    Hitstun,
    Blockstun,
    Knockdown,
    Dead
}

public class Fighter : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Fighter State")]
    public FighterState currentState = FighterState.Idle;

    [Header("Facing Direction")]
    public int facingDir = 1;

    //Set by Movement every frame so other systems can check airborne status.
    [HideInInspector] public bool isAirborne = false;

    private Transform opponentDir;

    //Frame-based stun timers. Decremented in FixedUpdate, one tick per physics step, which matches how AttackController counts frames.
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
    public void TakeDamage(int damage, int hitstun, Vector2 knockback)
    {
        if (currentState == FighterState.Dead) return;

        if (IsBlocking())
        {
            //Blocked hits: no damage, half hitstun as blockstun, softer pushback.
            SetState(FighterState.Blockstun);
            blockstunFrames = hitstun / 2;
            rb.linearVelocity = knockback * 0.5f;

            Debug.Log(gameObject.name + " blocked. HP: " + currentHealth);
            return;
        }

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

    //Only method that can change state to prevent accidental changes.
    public void SetState(FighterState newState)
    {
        currentState = newState;
    }

    //Useful checking method for other scripts
    public bool CanAct()
    {
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

    public bool IsBlocking()
    {
        if (opponentDir == null) return false;

        bool opponentIsRight = opponentDir.position.x > transform.position.x;

        //Always block when moving away from your opponent
        if (opponentIsRight && Keyboard.current.aKey.isPressed) return true;
        if (!opponentIsRight && Keyboard.current.dKey.isPressed) return true;

        return false;
    }
}