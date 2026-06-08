using UnityEngine;

//Like with attack, once we start making distinct characters,
//this will become abstract. All fighters will be children of this one.

public enum FighterState
{
    Idle,
    Moving,
    Attacking,
    Hitstun,
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
    public int facingDirection = 1;

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
        //TReduce health by attack damage, prevents it from dropping below 0.
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log(gameObject.name + " took " + damage + " damage. HP: " + currentHealth);

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

        //Apply's hitstun
        SetState(FighterState.Hitstun);
        hitstunTimer = hitStun;

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
}
