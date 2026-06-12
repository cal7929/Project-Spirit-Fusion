using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

//Like with attack, once we start making distinct characters,
//this will become abstract. All fighters will be children of this one.

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
    public bool isBlocking = false;

    [Header("Facing Direction")]
    [SerializeField]
    public int facingDir;

    private Transform opponentDir;

    //Hitsun and knockdown, read internally
    private float hitstunTimer = 0f;
    private float knockdownTimer = 0f;

    private Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
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
        //Reduce health by attack damage, prevents it from dropping below 0.
        if (!IsBlocking())
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            //Apply's hitstun
            SetState(FighterState.Hitstun);
            hitstunTimer = hitStun;

            Debug.Log(gameObject.name + " took " + damage + " damage. HP: " + currentHealth);
        }
        else
        {
            Debug.Log(gameObject.name + " blocked " + damage + " damage. HP: " + currentHealth);
        }

        //Manages the fighter dying (may adjust for a more in-depth system later)
        if (currentHealth <= 0)
        {
            SetState(FighterState.Dead);
            rb.linearVelocity = Vector2.zero;
            Debug.Log(gameObject.name + "'s spirit has been slain...");

            //Failsafe in case Clamp doesn't work or it bugs
            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            return;
        }

        

        //Apply's physics knockback
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
        if ( currentState != FighterState.Hitstun 
            && currentState != FighterState.Knockdown 
            && currentState != FighterState.Dead)
        {
            return true;
        }
        else
        {
            return false;
        }
        
    }

    public void SetOpponent(Transform opponentTransform)
    {
        opponentDir = opponentTransform;
    }

    void UpdateFacing()
    {
        if (opponentDir == null)
        {
            return;
        }

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
        if (opponentDir.position.x > transform.position.x && Keyboard.current.aKey.isPressed)
        {
            return true;
        }
        else if (opponentDir.position.x < transform.position.x && Keyboard.current.dKey.isPressed)
        {
            return false;
        }
        else
        {
            return false;
        }
    }
}
