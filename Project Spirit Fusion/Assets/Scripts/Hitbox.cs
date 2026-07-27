using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private AttackData attackData;
    private Fighter owner;

    //Tracks who's already been hit during the current activation, so a target
    //with multiple colliders (or one that stays overlapped across several
    //active frames) doesn't get hit - and damaged - more than once per swing.
    private HashSet<Fighter> hitThisActivation = new HashSet<Fighter>();

    //Called by AttackController when this attack's active frames begin.
    public void Activate(AttackData data, Fighter ownerFighter)
    {
        attackData = data;
        owner = ownerFighter;
        hitThisActivation.Clear();
        gameObject.SetActive(true);
    }

    //Called by AttackController when active frames end or on cancel.
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    //Actually deals the damage based on attack data
    void OnTriggerEnter2D(Collider2D other)
    {
        Fighter targetFighter = other.GetComponent<Fighter>();

        //Attacks don't hit your own hitbox
        if (targetFighter == null) return;
        if (targetFighter == owner) return;

        //Already hit this target during this activation - ignore further overlaps.
        if (!hitThisActivation.Add(targetFighter)) return;

        Vector2 knockbackDir = new Vector2(owner.facingDir, 0f).normalized;
        Vector2 knockback = knockbackDir * attackData.hitKnockback;

        targetFighter.TakeDamage(attackData.damage, attackData.hitstunFrames, knockback);
    }
}