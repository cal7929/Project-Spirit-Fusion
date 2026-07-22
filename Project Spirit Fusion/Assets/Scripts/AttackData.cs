using UnityEngine;

[System.Serializable]
public class AttackData
{
    public enum AttackType
    {
        High,
        Low,
        Overhead
    }

    public string attackName;

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

    [Tooltip("How many frames INTO recovery this attack can still be cancelled into the next in the chain (L>M>H only). 0 = active frames only.")]
    public int cancelWindowFrames;

    //Potentially adding a list of attacks that each attack can cancel into without worrying about frames,
    //this is likely not industry standard but could make our lives a lot easier if it works,
    //allows us to hand pick and customize what moves combo with what, issues would be keeping track of changes and 'magic number'ish scenarios

    //Derived frame thresholds used by AttackController to drive phase transitions.
    public int ActiveStartFrame => startupFrames;
    public int ActiveEndFrame => startupFrames + activeFrames;
    public int CancelEndFrame => startupFrames + activeFrames + cancelWindowFrames;
    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
}
