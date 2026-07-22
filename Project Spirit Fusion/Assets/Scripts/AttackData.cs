using UnityEngine;

//All timing values are in frames at 60fps.
[System.Serializable]
public class AttackData
{
    public string attackName;

    public GameObject hitboxObject;

    public int damage;

    public int hitstunFrames;

    public float hitKnockback;

    [Header("Frame Data (60fps)")]
    [Tooltip("Frames before the hitbox becomes active. Lower = faster attack.")]
    public int startupFrames;

    [Tooltip("Frames the hitbox is active and can deal damage.")]
    public int activeFrames;

    [Tooltip("Frames of recovery after active. The fighter is vulnerable here.")]
    public int recoveryFrames;

    [Tooltip("How many frames INTO recovery this attack can still be cancelled into the next in the chain (L>M>H only). 0 = active frames only.")]
    public int cancelWindowFrames;

    //Derived frame thresholds used by AttackController to drive phase transitions.
    public int ActiveStartFrame => startupFrames;
    public int ActiveEndFrame => startupFrames + activeFrames;
    public int CancelEndFrame => startupFrames + activeFrames + cancelWindowFrames;
    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
}
