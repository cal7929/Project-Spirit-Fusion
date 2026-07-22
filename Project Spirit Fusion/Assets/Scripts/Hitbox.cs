
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private AttackData attackData;
    private Fighter owner;

    //Called by AttackController when this attack's active frames begin.
    public void Activate(AttackData data, Fighter ownerFighter)
    {
        attackData = data;
        owner = ownerFighter;
        gameObject.SetActive(true);
    }

    //Called by AttackController when active frames end or on cancel.
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Fighter targetFighter = other.GetComponent<Fighter>();

        //Attacks don't hit your own hitbox
        if (targetFighter == null) return;
        if (targetFighter == owner) return;

        Vector2 knockbackDir = new Vector2(owner.facingDir, 0f).normalized;
        Vector2 knockback = knockbackDir * attackData.hitKnockback;

        targetFighter.TakeDamage(attackData.damage, attackData.hitstunFrames, knockback);
    }
}