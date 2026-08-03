using UnityEngine;

//Training-mode-only GameManager. Match-flow features (rounds, KO wins, round
//timer, match end) are vs-mode scope and deliberately NOT implemented here -
//see the note at the bottom for where that slots back in later.
//
//Holds one TagController per side - Player 1's team, and Player 2's team
//(currently the training dummy team). Doesn't need to know or care whether a
//team's active fighter is player-controlled or a dummy; TagController
//already abstracts "whichever fighter is currently in play" identically for
//both, so this script only ever deals in that abstraction.
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
        // Opponent tracking has to stay live
        player1Team.ActiveFighter.SetOpponent(player2Team.ActiveFighter.transform);
        player2Team.ActiveFighter.SetOpponent(player1Team.ActiveFighter.transform);

        // Unity deletes Physics2D.IgnoreCollision rules when a GameObject is deactivated.
        // Continuously applying it here ensures fighters immediately regain their 
        // pushbox logic the instant they are tagged back in.
        foreach (Fighter a in player1Team.AllFighters)
        {
            foreach (Fighter b in player2Team.AllFighters)
            {
                // Only apply if both are currently active so Unity doesn't throw warnings
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

    //Only pushes apart whichever fighter is currently active on each team -
    //the benched fighter is deactivated and isn't part of the physical scene,
    //so it never needs a pushbox check.
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

        //Skip the push if one fighter is mid-jump.
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

    //---- Deliberately not implemented yet (vs-mode scope) ----
    //
    // Round start/end states, round timer, KO-based round wins, match-end
    // win screens, and fighter health/position reset between rounds all
    // belong here once vs mode is back in scope. The previous single-Fighter
    // version of this script had a working version of all of that (MatchState,
    // roundsWon tracking, StartRound/EndRound/ResetFighter) - worth adapting
    // from directly once teams need round-based resets instead of a single
    // fighter each (e.g. ResetFighter would need to reset BOTH team members
    // and re-bench whichever wasn't active, not just one Fighter).
}