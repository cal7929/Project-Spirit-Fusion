using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Project_Spirit_Fusion/Attack Data")]
public class AttackData : ScriptableObject
{
    public enum AttackType
    {
        High,
        Low,
        Overhead
    }

    public enum AttackStance
    {
        Standing,
        Crouching,
        Jumping
    }

    public enum AttackStrength
    {
        Light,
        Medium,
        Heavy
    }

    [Header("Identity")]
    public string attackName;

    public AttackStance requiredStance = AttackStance.Standing;

    public AttackStrength strength;

    [Tooltip("Leave empty for a normal (button press only). Fill in with numpad notation for a special, e.g. \"236\" = quarter-circle-forward.")]
    public string motionInput = "";

    public bool IsSpecialMove => !string.IsNullOrEmpty(motionInput);

    public GameObject hitboxObject;

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

    [Tooltip("Frames of recovery after active. The fighter is vulnerable here.")]
    public int recoveryFrames;

    [Tooltip("How many frames INTO recovery this attack can still be cancelled into something in cancelOptions. 0 = active frames only.")]
    public int cancelWindowFrames;

    [Header("Cancels")]
    [Tooltip("Attacks (normals or specials) this move can cancel into during its active/cancel window. Replaces the old hardcoded L>M>H chain - add/remove entries here instead of editing code.")]
    public List<AttackData> cancelOptions = new List<AttackData>();

    //Derived frame thresholds used by AttackController to drive phase transitions.
    public int ActiveStartFrame => startupFrames;
    public int ActiveEndFrame => startupFrames + activeFrames;
    public int CancelEndFrame => startupFrames + activeFrames + cancelWindowFrames;
    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
}
