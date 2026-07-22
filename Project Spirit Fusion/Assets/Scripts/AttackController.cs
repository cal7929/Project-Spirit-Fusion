using UnityEngine;
using UnityEngine.InputSystem;

//This script will be a common script that should control the light medium and hevay attacks for each fighter,
//since all fighters share those 3 attacks plus any jumping and crouching attacks.
public class AttackController : MonoBehaviour
{
    //Which button strength is currently active, null when idle.
    private enum AttackStrength { Light, Medium, Heavy }

    [Header("Standing Attacks")]
    public AttackData standLight;
    public AttackData standMedium;
    public AttackData standHeavy;

    [Header("Crouching Attacks")]
    public AttackData crouchLight;
    public AttackData crouchMedium;
    public AttackData crouchHeavy;

    [Header("Jumping Attacks")]
    public AttackData jumpLight;
    public AttackData jumpMedium;
    public AttackData jumpHeavy;

    private Fighter fighter;

    public Hitbox standLHitbox;
    public Hitbox standMHitbox;
    public Hitbox standHHitbox;

    public Hitbox crouchLHitbox;
    public Hitbox crouchMHitbox;
    public Hitbox crouchHHitbox;

    public Hitbox jumpLHitbox;
    public Hitbox jumpMHitbox;
    public Hitbox jumpHHitbox;

    private AttackData currentAttack;
    private Hitbox currentHitbox;
    private AttackStrength? currentStrength; 

    //Counts up from 0
    private int attackFrame; 

    void Start()
    {
        fighter = GetComponent<Fighter>();
        standLHitbox = CacheHitbox(standLight);
        standMHitbox = CacheHitbox(standMedium);
        standHHitbox = CacheHitbox(standHeavy);
    }

    Hitbox CacheHitbox(AttackData data)
    {
        if (data == null || data.hitboxObject == null) 
            return null;

        data.hitboxObject.SetActive(false);
        return data.hitboxObject.GetComponent<Hitbox>();
    }

    //Input is read in Update so wasPressedThisFrame actually works consistently.
    void Update()
    {
        HandleInput();
    }

    //Frame counter and phase transitions run in FixedUpdate so they advance exactly once per physics tick, meaning one frame at 60fps.
    void FixedUpdate()
    {
        AdvanceAttack();
    }

    void HandleInput()
    {
        bool lightPressed = Keyboard.current.jKey.wasPressedThisFrame;
        bool mediumPressed = Keyboard.current.kKey.wasPressedThisFrame;
        bool heavyPressed = Keyboard.current.lKey.wasPressedThisFrame;

        if (!lightPressed && !mediumPressed && !heavyPressed) return;

        //During the cancel window, only the specific next attack in the chain is allowed.
        //Will hopefull change this to be more dynamic later on to include command normals and specials
        //L can cancel into M. M can cancel into H. H cannot cancel into anything.
        if (currentAttack != null && InCancelWindow())
        {
            if (currentStrength == AttackStrength.Light && mediumPressed)
            {
                StartAttack(standMedium, standMHitbox, AttackStrength.Medium);
                return;
            }
            if (currentStrength == AttackStrength.Medium && heavyPressed)
            {
                StartAttack(standHeavy, standHHitbox, AttackStrength.Heavy);
                return;
            }
        }

        //If no attack is currently active
        if (currentAttack == null && fighter.CanAct())
        {
            if (lightPressed)
                StartAttack(standLight, standLHitbox, AttackStrength.Light);
            else if (mediumPressed)
                StartAttack(standMedium, standMHitbox, AttackStrength.Medium);
            else if (heavyPressed)
                StartAttack(standHeavy, standHHitbox, AttackStrength.Heavy);
        }
    }

    void StartAttack(AttackData data, Hitbox hitbox, AttackStrength strength)
    {
        if (data == null) return;

        //Deactivate the previous hitbox immediately on cancel.
        currentHitbox?.Deactivate();

        currentAttack = data;
        currentHitbox = hitbox;
        currentStrength = strength;
        attackFrame = 0;

        fighter.SetState(FighterState.Attacking);
    }

    void AdvanceAttack()
    {
        if (currentAttack == null) return;

        //Startup to Active
        if (attackFrame == currentAttack.ActiveStartFrame)
            currentHitbox?.Activate(currentAttack, fighter);

        //Active to Recovery
        if (attackFrame == currentAttack.ActiveEndFrame)
            currentHitbox?.Deactivate();

        //Recovery to end
        if (attackFrame >= currentAttack.TotalFrames)
        {
            EndAttack();
            return;
        }

        attackFrame++;
    }

    //True while we're in the active frames or within the cancel window of recovery.
    bool InCancelWindow()
    {
        if (currentAttack == null) return false;
        return attackFrame >= currentAttack.ActiveStartFrame
            && attackFrame <= currentAttack.CancelEndFrame;
    }

    void EndAttack()
    {
        currentHitbox?.Deactivate();
        currentAttack = null;
        currentHitbox = null;
        currentStrength = null;
        attackFrame = 0;

        if (fighter.currentState == FighterState.Attacking)
            fighter.SetState(FighterState.Idle);
    }
}