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
        public string name;
        public AttackStance stance;
        public AttackStrength strength;
        public AttackData data;
    }

    //Maps a hitboxId to the fighter instance's own local Hitbox child. Set in the Inspector, so each
    //fighter resolves to its own hitbox even if multiple fighters share the same AttackData assets.(when we add those)
    [System.Serializable]
    public struct HitboxSlot
    {
        public string id;
        public Hitbox hitbox;
    }

    [Header("Normal Attacks")]
    public List<NormalMoveSlot> normalMoves = new List<NormalMoveSlot>();

    //This fighter's own hitbox children, tagged with an id that matches AttackData.hitboxId
    [Header("Hitboxes")]
    public List<HitboxSlot> hitboxSlots = new List<HitboxSlot>();

    //Where projectile-type attacks spawn from. Defaults to this fighter's own position if left empty
    [Header("Projectiles")]
    public Transform projectileSpawnPoint;

    //Dictionary to look for valid moves within the list
    private Dictionary<(AttackStance, AttackStrength), AttackData> normalLookup;
    private Dictionary<string, Hitbox> hitboxLookup;

    //Tracks each projectile-type move's currently in-flight instance (if
    //any), keyed by AttackData, so this fighter can't throw a second one
    //until the first is destroyed. Lives on THIS AttackController instance,
    //so two fighters sharing the same AttackData asset don't block each
    //other - same per-instance reasoning as hitboxLookup above.
    private Dictionary<AttackData, GameObject> activeProjectiles = new Dictionary<AttackData, GameObject>();

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
            if (string.IsNullOrEmpty(slot.id))
            {
                Debug.LogWarning("A Hitbox Slot has a blank id and will be skipped.", this);
                continue;
            }
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

        //A projectile can't be thrown again until its previous instance is
        //gone. Cleared here (not by an event from Hitbox/Projectile) since
        //Unity's Object equality treats a destroyed GameObject as == null,
        //so this check stays correct automatically once it hits or expires.
        if (special != null && IsProjectileOnCooldown(special))
            special = null;

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

        //Defensive stance check. normalLookup/cancelOptions are already
        //keyed by stance, and Movement now keeps currentStance fresh every
        //Update (see Movement.cs) specifically to prevent stale-stance false
        //triggers - but this is a hard backstop against ANY path (including
        //specials, which don't go through normalLookup) starting a move
        //whose required stance doesn't match what the fighter is actually
        //doing right now.
        if (data.requiredStance != fighter.currentStance) return;

        //Deactivate the previous hitbox immediately on a cancel.
        currentHitbox?.Deactivate();

        currentAttack = data;
        currentHitbox = ResolveHitbox(data);
        attackFrame = 0;

        fighter.SetState(FighterState.Attacking);
    }

    //Returns true if this move is a projectile-type special and its previous
    //instance is still alive. Unity's Object equality treats a destroyed
    //UnityEngine.Object as == null, so once the tracked instance hits or
    //expires, this correctly reports "not on cooldown" with no extra cleanup.
    bool IsProjectileOnCooldown(AttackData data)
    {
        if (!data.IsProjectile) return false;
        return activeProjectiles.TryGetValue(data, out GameObject existing) && existing != null;
    }

    Hitbox ResolveHitbox(AttackData data)
    {
        //Projectiles manage their own Hitbox
        if (data.IsProjectile) return null;

        if (string.IsNullOrEmpty(data.hitboxId))
        {
            Debug.LogWarning($"{data.attackName}: no hitboxId set and this isn't a projectile move - it will do nothing on hit.", this);
            return null;
        }

        if (!hitboxLookup.TryGetValue(data.hitboxId, out Hitbox hitbox) || hitbox == null)
        {
            Debug.LogWarning($"{data.attackName}: hitboxId \"{data.hitboxId}\" has no matching entry in this fighter's Hitbox Slots.", this);
        }

        return hitbox;
    }

    void AdvanceAttack()
    {
        if (currentAttack == null) return;

        //Jumping attacks must end the instant the fighter lands - a jump-in
        //normal shouldn't keep playing out (and hitting people) once
        //grounded, since its hitbox/animation is only meant for the air.
        if (currentAttack.requiredStance == AttackStance.Jumping && fighter.currentStance != AttackStance.Jumping)
        {
            EndAttack();
            return;
        }

        //Startup to Active
        if (attackFrame == currentAttack.ActiveStartFrame)
        {
            if (currentAttack.IsProjectile)
                SpawnProjectile(currentAttack);
            else
                currentHitbox?.Activate(currentAttack, fighter);
        }

        //Active to Recovery. 
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

    //If the fighter has a projectile attack, which many will.
    //Instanstantiates the projectile at the spawn point and then launches it.
    void SpawnProjectile(AttackData data)
    {
        if (data.projectilePrefab == null)
        {
            Debug.LogWarning($"{data.attackName}: IsProjectile is true but no projectilePrefab is assigned. Nothing will spawn.", this);
            return;
        }

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject instance = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

        //Flip the projectile's visuals to match facing direction at launch.
        Vector3 scale = instance.transform.localScale;
        instance.transform.localScale = new Vector3(Mathf.Abs(scale.x) * fighter.facingDir, scale.y, scale.z);

        Projectile projectile = instance.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogWarning($"{data.attackName}: projectilePrefab has no Projectile component - it will spawn but never move or activate its Hitbox.", instance);
            return;
        }

        activeProjectiles[data] = instance;
        projectile.Launch(data, fighter, fighter.facingDir);
    }

    //True during the active frames or within the cancel window of recovery frames
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