using UnityEngine;
using UnityEngine.InputSystem;

public enum FighterState
{
    Idle,
    Moving,
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

    private Transform opponentDir;

    //Timers, read internally
    private float hitstunTimer = 0f;
    private float blockstunTimer = 0f;
    private float knockdownTimer = 0f;

    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        UpdateFacing();

        if (currentState == FighterState.Hitstun)
        {
            hitstunTimer -= Time.deltaTime;
            if (hitstunTimer <= 0f)
            {
                SetState(FighterState.Idle);
            }
        }

        if (currentState == FighterState.Blockstun)
        {
            blockstunTimer -= Time.deltaTime;
            if (blockstunTimer <= 0f)
            {
                SetState(FighterState.Idle);
            }
        }

        if (currentState == FighterState.Knockdown)
        {
            knockdownTimer -= Time.deltaTime;
            if (knockdownTimer <= 0f)
            {
                SetState(FighterState.Idle);
            }
        }
    }

    public void TakeDamage(int damage, float hitStun, Vector2 knockback)
    {
        if (currentState == FighterState.Dead) return;

        if (IsBlocking())
        {
            //Blocked hits deal no damage, shorter stun, softer pushback. (chip damage from specials will be implemented later)
            SetState(FighterState.Blockstun);
            blockstunTimer = hitStun * 0.5f;
            rb.linearVelocity = knockback * 0.5f;

            Debug.Log(gameObject.name + " blocked " + damage + " damage. HP: " + currentHealth);
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
        hitstunTimer = hitStun;
        rb.linearVelocity = knockback;
    }

    //Only method that can change state to prevent accidental changes
    public void SetState(FighterState newState)
    {
        currentState = newState;
    }

    //Convenience check used by Movement and Attack
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

        //Blocking = holding away from the opponent (holding "back").
        if (opponentIsRight && Keyboard.current.aKey.isPressed) return true;
        if (!opponentIsRight && Keyboard.current.dKey.isPressed) return true;

        return false;
    }
}