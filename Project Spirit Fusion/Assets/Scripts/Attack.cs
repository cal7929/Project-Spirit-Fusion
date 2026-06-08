using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

//Like with fighter, once we start making distinct characters,
//this will become abstract. All attacks will be children of this one.
//Currently, this is to test an attack/collision system and is not representative
//of the setup for an attack in an actual fighting game.

public class Attack : MonoBehaviour
{
    [Header("Hitbox")]
    public GameObject hitboxObject;

    [Header("Attack Stats")]
    public int damage = 10;
    public float hitstunDuration = 0.4f;    
    public float hitKnockback = 2f;

    [Header("Frame Data")]
    public float attackDuration = 0.15f;    
    public float attackRecovery = 0.4f;     

    private float attackTimer = 0f;
    private float recoveryTimer = 0f;

    private Fighter fighter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fighter = GetComponent<Fighter>();

        if (hitboxObject != null)
        {
            hitboxObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Active frame timer
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        //Recover frame timer
        if (recoveryTimer > 0f)
        {
            recoveryTimer -= Time.deltaTime;
        }

        // Deactivate hitbox once active frames are done
        if (fighter.currentState == FighterState.Attacking && attackTimer <= 0f)
        {
            fighter.SetState(FighterState.Idle);
            hitboxObject.SetActive(false);
        }

        // Only allow attack input if Fighter says we can act
        if (Keyboard.current.jKey.wasPressedThisFrame && fighter.CanAct() && recoveryTimer <= 0f)
        {
            StartAttack();
        }
    }

    void StartAttack()
    {
        fighter.SetState(FighterState.Attacking);
        attackTimer = attackDuration;
        recoveryTimer = attackRecovery;
        hitboxObject.SetActive(true);
    }
}
