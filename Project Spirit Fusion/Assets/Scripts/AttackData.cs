using System.Collections.Generic;
using UnityEngine;

//Script that contains all the relevant info for a move in the game. 
[CreateAssetMenu(fileName = "New Attack", menuName = "Project_Spirit_Fusion/Attack Data")]
public class AttackData : ScriptableObject
{
    public enum AttackType
    {
        High,
        Low,
        Overhead
    }

    [Header("Identity")]
    public string attackName;

    public AttackStance requiredStance = AttackStance.Standing;

    public AttackStrength strength;

    [Tooltip("Leave empty for normals (button press only). Fill in with numpad notation for a special, ex: 236 for a quarter circle forward")]
    public string motionInput = "";

    public bool IsSpecialMove => !string.IsNullOrEmpty(motionInput);

    [Tooltip("Matches a HitboxSlot.id on this fighter's AttackController - resolved per-instance instead of pointing at a specific GameObject, since this asset is shared across every fighter of this archetype.")]
    public string hitboxId;

    [Header("Stats")]
    public int damage;

    public int hitstunFrames;

    public float hitKnockback;

    public AttackType type;

    [Header("Frame Data (60fps)")]
    [Tooltip("Frames before the hitbox becomes active. Lower = faster attack.")]
    public int startupFrames;

    [Tooltip("Frames the hitbox is active and can deal damage.")]
    public int activeFrames;

    [Tooltip("Frames of recovery after active.")]
    public int recoveryFrames;

    [Tooltip("How many frames during recovery this attack can still be cancelled into something in its cancelOptions. 0 = active frames only.")]
    public int cancelWindowFrames;

    [Header("Cancels")]
    [Tooltip("Any move that this move can cancel into during its cancel window.")]
    public List<AttackData> cancelOptions = new List<AttackData>();

    [Header("Projectile (optional)")]
    [Tooltip("If set, this move spawns this prefab instead of activating a fixed hitboxId - used for fireball-style specials. Leave empty for normal melee moves.")]
    public GameObject projectilePrefab;

    [Tooltip("Units per second the spawned projectile travels forward.")]
    public float projectileSpeed = 10f;

    [Tooltip("Seconds before the projectile despawns itself if it hasn't hit anything.")]
    public float projectileLifetime = 3f;

    public bool IsProjectile => projectilePrefab != null;

    //Derived frame thresholds used by AttackController to drive phase transitions.
    public int ActiveStartFrame => startupFrames;
    public int ActiveEndFrame => startupFrames + activeFrames;
    public int CancelEndFrame => startupFrames + activeFrames + cancelWindowFrames;
    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
}