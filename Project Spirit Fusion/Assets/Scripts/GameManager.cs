using UnityEngine;

//For the demo of this game only training mode is taken into account here.
//However VS mode woun't be that much to implement, just trying to stay ins cope and focus on other things.
public class GameManager : MonoBehaviour
{
    [Header("Teams")]
    public TagController player1Team;
    public TagController player2Team;

    //Prevents fighters from colliding with each other in awkward ways, also
    //prevents them from standing on each other.
    [Header("Pushbox Settings")]
    public float pushboxWidth = 1f;
    public float pushboxHeightTolerance = 1.5f;

    void Start()
    {
        if (player1Team == null || player2Team == null)
        {
            Debug.LogError("GameManager is missing a team reference, assign both in the Inspector.");
            enabled = false;
            return;
        }

        foreach (Fighter fighter in player1Team.AllFighters)
            fighter.SetOpponent(player2Team.ActiveFighter.transform);

        foreach (Fighter fighter in player2Team.AllFighters)
            fighter.SetOpponent(player1Team.ActiveFighter.transform);
    }

    void Update()
    {
        // Always track who is the opponent regardless of who's currently tagged in
        player1Team.ActiveFighter.SetOpponent(player2Team.ActiveFighter.transform);
        player2Team.ActiveFighter.SetOpponent(player1Team.ActiveFighter.transform);

        //Unity auto deletes Physics2D.IgnoreCollision rules when a GameObject is deactivated.
        //This continuously applies it so its never lost after a tag. (could also be fixed by adding a tagged out state but we'll see)
        foreach (Fighter a in player1Team.AllFighters)
        {
            foreach (Fighter b in player2Team.AllFighters)
            {
                //Only apply if both are currently active so Unity doesn't give warnings
                if (a.gameObject.activeInHierarchy && b.gameObject.activeInHierarchy)
                {
                    IgnoreCollisionBetween(a, b);
                }
            }
        }
    }

    void FixedUpdate()
    {
        ResolvePushbox();
    }

    //Only pushes apart whichever fighter is currently active on each team
    //the benched fighter is deactivated and isn't part of the scene, so it never needs a pushbox check.
    void ResolvePushbox()
    {
        Fighter fighterA = player1Team.ActiveFighter;
        Fighter fighterB = player2Team.ActiveFighter;

        Rigidbody2D rbA = player1Team.ActiveRigidbody;
        Rigidbody2D rbB = player2Team.ActiveRigidbody;

        if (rbA == null || rbB == null) return;
        if (fighterA.currentState == FighterState.Dead || fighterB.currentState == FighterState.Dead) return;
        if (fighterA.currentState == FighterState.Knockdown || fighterB.currentState == FighterState.Knockdown) return;

        Vector2 posA = rbA.position;
        Vector2 posB = rbB.position;

        //Skip the push if one fighter is jumping.
        float verticalGap = Mathf.Abs(posA.y - posB.y);
        if (verticalGap > pushboxHeightTolerance) return;

        float horizontalGap = posB.x - posA.x;
        float overlap = pushboxWidth - Mathf.Abs(horizontalGap);
        if (overlap <= 0f) return;

        //Push both fighters equally away from each other.
        float pushDir = horizontalGap >= 0f ? 1f : -1f;
        float pushAmount = overlap * 0.5f;

        rbA.position = posA - new Vector2(pushDir * pushAmount, 0f);
        rbB.position = posB + new Vector2(pushDir * pushAmount, 0f);
    }

    void IgnoreCollisionBetween(Fighter a, Fighter b)
    {
        Collider2D colA = a.GetComponent<Collider2D>();
        Collider2D colB = b.GetComponent<Collider2D>();

        if (colA != null && colB != null)
            Physics2D.IgnoreCollision(colA, colB);
    }
}