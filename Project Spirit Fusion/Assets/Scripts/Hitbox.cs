using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private AttackData attackData;
    private Fighter owner;
    private readonly HashSet<Fighter> hitTargets = new HashSet<Fighter>();

    //Called by AttackController when this attack's active frames begin.
    public void Activate(AttackData data, Fighter ownerFighter)
    {
        attackData = data;
        owner = ownerFighter;
        hitTargets.Clear();
        gameObject.SetActive(true);
    }

    //Called by AttackController when active frames end.
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Fighter targetFighter = other.GetComponent<Fighter>();

        //Ignore non-fighters, yourself, and anything already hit this activation,
        //(prevents it from hitting multiple times which may be something we want for certain attacks).
        if (targetFighter == null) return;
        if (targetFighter == owner) return;
        if (hitTargets.Contains(targetFighter)) return;

        hitTargets.Add(targetFighter);

        Vector2 knockbackDir = new Vector2(owner.facingDir, 0f).normalized;
        Vector2 knockback = knockbackDir * attackData.hitKnockback;

        targetFighter.TakeDamage(attackData.damage, attackData.hitstunDuration, knockback);
    }
}
