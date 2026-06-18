using UnityEngine;
using UnityEngine.InputSystem;

// Replaces the old Attack.cs. Delete Attack.cs from the project and
// put this on any character that should be able to attack
// (the training dummy doesn't need this component).
public class AttackController : MonoBehaviour
{
    private enum Phase 
    { 
        None, 
        Startup, 
        Active, 
        Recovery 
    }

    [Header("Attacks")]
    public AttackData lightAttack;
    public AttackData mediumAttack;
    public AttackData heavyAttack;

    private Fighter fighter;

    private Hitbox lightHitbox;
    private Hitbox mediumHitbox;
    private Hitbox heavyHitbox;

    private AttackData currentAttack;
    private Hitbox currentHitbox;
    private Phase phase = Phase.None;
    private float phaseTimer;

    void Start()
    {
        fighter = GetComponent<Fighter>();

        lightHitbox = CacheHitbox(lightAttack);
        mediumHitbox = CacheHitbox(mediumAttack);
        heavyHitbox = CacheHitbox(heavyAttack);
    }

    Hitbox CacheHitbox(AttackData data)
    {
        if (data == null || data.hitboxObject == null)
        {
            return null;
        }

        data.hitboxObject.SetActive(false);
        return data.hitboxObject.GetComponent<Hitbox>();
    }

    void Update()
    {
        AdvancePhase();
        HandleInput();
    }

    void HandleInput()
    {
        //Mid-attack: ignore new attack input for now (no combo/canceling/chaining yet).
        if (phase != Phase.None)
        {
            return;
        }
        if (!fighter.CanAct())
        {
            return;
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            StartAttack(lightAttack, lightHitbox);
        }
        else if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            StartAttack(mediumAttack, mediumHitbox);
        }
        else if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            StartAttack(heavyAttack, heavyHitbox);
        }
            
    }

    void StartAttack(AttackData data, Hitbox hitbox)
    {
        if (data == null)
        {
            return;
        }

        currentAttack = data;
        currentHitbox = hitbox;
        phase = Phase.Startup;
        phaseTimer = data.startupTime;
        fighter.SetState(FighterState.Attacking);
    }

    void AdvancePhase()
    {
        if (phase == Phase.None)
        {
            return;
        }

        phaseTimer -= Time.deltaTime;

        if (phaseTimer > 0f)
        {
            return;
        }

        switch (phase)
        {
            case Phase.Startup:
                phase = Phase.Active;
                phaseTimer = currentAttack.activeTime;
                currentHitbox?.Activate(currentAttack, fighter);
                break;

            case Phase.Active:
                phase = Phase.Recovery;
                phaseTimer = currentAttack.recoveryTime;
                currentHitbox?.Deactivate();
                break;

            case Phase.Recovery:
                phase = Phase.None;
                currentAttack = null;
                currentHitbox = null;
                if (fighter.currentState == FighterState.Attacking)
                {
                    fighter.SetState(FighterState.Idle);
                }
                break;
        }
    }
}