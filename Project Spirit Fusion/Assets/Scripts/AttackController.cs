using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//The state machine that takes info given to it by CommandParser
//and executes the actual attacks and specials based on the given data.
[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(CommandParser))]
public class AttackController : MonoBehaviour
{
    //Struct containing the fighter's normal moves, set in the inspector
    [System.Serializable]
    public struct NormalMoveSlot
    {
        public AttackStance stance;
        public AttackStrength strength;
        public AttackData data;
    }

    [Header("Normal Attacks")]
    public List<NormalMoveSlot> normalMoves = new List<NormalMoveSlot>();

    //Dictionary to look for valid moves within the list
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

    //Input is read in Update to stay consistent with wasPressedThisFrame from InputReader
    void Update()
    {
        HandleInput();
    }

    //Frame counter and phase transitions run in FixedUpdate so
    //they advance exactly once per frame
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
        //attack's cancelOptions is allowed
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

        //Deactivate the previous hitbox immediately on a cancel.
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

    //True during the active frames or within the cancel window of recovery.
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