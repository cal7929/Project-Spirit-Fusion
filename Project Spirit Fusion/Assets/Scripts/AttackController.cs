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

    //Maps a hitboxId (matching AttackData.hitboxId) to THIS fighter instance's
    //own local Hitbox child. Wired up per-prefab in the Inspector, so each
    //fighter resolves to its own hitbox even when multiple fighters share the
    //same AttackData assets.
    [System.Serializable]
    public struct HitboxSlot
    {
        public string id;
        public Hitbox hitbox;
    }

    [Header("Normal Attacks")]
    public List<NormalMoveSlot> normalMoves = new List<NormalMoveSlot>();

    [Header("Hitboxes")]
    [Tooltip("This fighter's own hitbox children, tagged with an id that matches AttackData.hitboxId.")]
    public List<HitboxSlot> hitboxSlots = new List<HitboxSlot>();

    //Dictionary to look for valid moves within the list
    private Dictionary<(AttackStance, AttackStrength), AttackData> normalLookup;
    private Dictionary<string, Hitbox> hitboxLookup;

    private Fighter fighter;
    private InputReader inputReader;
    private CommandParser commandParser;

    private AttackData currentAttack;
    private Hitbox currentHitbox;

    //Counts up from 0
    private int attackFrame;

    void Awake()
    {
        fighter = GetComponent<Fighter>();
        inputReader = GetComponent<InputReader>();
        commandParser = GetComponent<CommandParser>();

        normalLookup = new Dictionary<(AttackStance, AttackStrength), AttackData>();
        foreach (NormalMoveSlot slot in normalMoves)
        {
            normalLookup[(slot.stance, slot.strength)] = slot.data;
        }

        hitboxLookup = new Dictionary<string, Hitbox>();
        foreach (HitboxSlot slot in hitboxSlots)
        {
            hitboxLookup[slot.id] = slot.hitbox;
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
        InputReader.InputFrame last = inputReader.Latest;

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

    //Resolves the Hitbox for this move from THIS fighter's own hitboxLookup,
    //keyed by AttackData.hitboxId. Every fighter instance resolves to its own
    //hitbox child even when sharing the same AttackData asset.
    Hitbox ResolveHitbox(AttackData data)
    {
        if (string.IsNullOrEmpty(data.hitboxId)) return null;

        hitboxLookup.TryGetValue(data.hitboxId, out Hitbox hitbox);
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