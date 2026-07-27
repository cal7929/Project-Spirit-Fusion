using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AttackData;

//Common script controlling light/medium/heavy attacks (and now specials) for
//any fighter. Pure state machine now - it does NOT read Keyboard directly and
//does NOT know how an attack was triggered (button vs motion). It just reacts
//to "this AttackData won this frame," which InputBuffer + CommandParser feed it.
//
//Requires InputBuffer and CommandParser components on the same GameObject.
[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(CommandParser))]
public class AttackController : MonoBehaviour
{
    //One entry per (stance, strength) combo you want available, e.g.
    //Standing+Light, Crouching+Medium, Jumping+Heavy. This replaces the old
    //9 separate named AttackData/Hitbox field pairs - add a stance's worth of
    //moves by adding list entries, not new fields.
    [System.Serializable]
    public struct NormalMoveSlot
    {
        //AttackStance, not FighterState - see AttackStance's comments for why
        //these are tracked as two separate things now.
        public AttackStance stance;
        public AttackStrength strength;
        public AttackData data;
    }

    [Header("Normal Attacks")]
    [Tooltip("One entry per stance+strength combo, e.g. Standing/Light, Crouching/Medium, Jumping/Heavy.")]
    public List<NormalMoveSlot> normalMoves = new List<NormalMoveSlot>();

    private Dictionary<(AttackStance, AttackStrength), AttackData> normalLookup;
    private Dictionary<AttackData, Hitbox> hitboxCache = new Dictionary<AttackData, Hitbox>();

    private Fighter fighter;
    private InputReader inpuinputReader;
    private CommandParser commandParser;

    private AttackData currentAttack;
    private Hitbox currentHitbox;

    //Counts up from 0
    private int attackFrame;

    void Awake()
    {
        fighter = GetComponent<Fighter>();
        inpuinputReader = GetComponent<InputReader>();
        commandParser = GetComponent<CommandParser>();

        normalLookup = new Dictionary<(AttackStance, AttackStrength), AttackData>();
        foreach (NormalMoveSlot slot in normalMoves)
        {
            normalLookup[(slot.stance, slot.strength)] = slot.data;
        }
    }

    //Input is read in Update so wasPressedThisFrame (via InputBuffer) stays consistent.
    void Update()
    {
        HandleInput();
    }

    //Frame counter and phase transitions run in FixedUpdate so they advance
    //exactly once per physics tick, meaning one frame at 60fps.
    void FixedUpdate()
    {
        AdvanceAttack();
    }

    void HandleInput()
    {
        AttackData special = commandParser.TryParseSpecial();
        InputReader.InputFrame last = inpuinputReader.Latest;

        AttackStrength? pressedStrength = null;
        if (last.lightPressed) pressedStrength = AttackStrength.Light;
        else if (last.mediumPressed) pressedStrength = AttackStrength.Medium;
        else if (last.heavyPressed) pressedStrength = AttackStrength.Heavy;

        if (special == null && pressedStrength == null) return;

        //During the cancel window, only whatever is listed in the current
        //attack's cancelOptions is allowed - specials or normals, decided by
        //data instead of hardcoded strength comparisons.
        if (currentAttack != null && InCancelWindow())
        {
            if (special != null && currentAttack.cancelOptions.Contains(special))
            {
                StartAttack(special);
                return;
            }

            if (pressedStrength != null)
            {
                AttackData match = currentAttack.cancelOptions.FirstOrDefault(a =>
                    !a.IsSpecialMove &&
                    a.strength == pressedStrength.Value &&
                    a.requiredStance == fighter.currentStance);

                if (match != null)
                {
                    StartAttack(match);
                    return;
                }
            }
        }

        //If no attack is currently active, start fresh. Specials take
        //priority over normals when both happen to resolve on the same frame.
        if (currentAttack == null && fighter.CanAct())
        {
            if (special != null)
            {
                StartAttack(special);
                return;
            }

            if (pressedStrength != null)
            {
                normalLookup.TryGetValue((fighter.currentStance, pressedStrength.Value), out AttackData data);
                if (data != null) StartAttack(data);
            }
        }
    }

    void StartAttack(AttackData data)
    {
        if (data == null) return;

        //Deactivate the previous hitbox immediately on cancel.
        currentHitbox?.Deactivate();

        currentAttack = data;
        currentHitbox = ResolveHitbox(data);
        attackFrame = 0;

        fighter.SetState(FighterState.Attacking);
    }

    //Resolves and caches the Hitbox component from AttackData.hitboxObject,
    //so each move only needs to be wired up once on its own asset instead of
    //needing a matching field on AttackController.
    Hitbox ResolveHitbox(AttackData data)
    {
        if (data.hitboxObject == null) return null;

        if (!hitboxCache.TryGetValue(data, out Hitbox hitbox))
        {
            hitbox = data.hitboxObject.GetComponent<Hitbox>();
            hitboxCache[data] = hitbox;
        }

        return hitbox;
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
        attackFrame = 0;

        if (fighter.currentState == FighterState.Attacking)
            fighter.SetState(FighterState.Idle);
    }
}