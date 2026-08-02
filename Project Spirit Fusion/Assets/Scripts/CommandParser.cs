using UnityEngine;
using System.Collections.Generic;

//Watches InputBuffer for known special-move motion notation and returns the matching AttackData when
//one is completed. AttackController calls TryParseSpecial() once per frame
//alongside its normal-attack handling so that specials take priority when both trigger on the same frame.
public class CommandParser : MonoBehaviour
{
    [Tooltip("All special moves this character can perform.")]
    public List<AttackData> specialMoves = new List<AttackData>();

    [Tooltip("How many frames back to search for a motion. Larger number = more leniant timing, but more likely to perform unintentional moves.")]
    public int searchWindow = 20;

    private InputReader inputBuffer;

    void Awake()
    {
        inputBuffer = GetComponent<InputReader>();
    }

    //Call once per frame. Returns the AttackData for the first special whose
    //motion had already completed by the time of the most recent button
    //press within the buffer window - not just on this exact literal frame.
    //This is what lets a player finish 236+P a few frames before their
    //recovery/hitstun ends without the input being dropped: instead of
    //requiring "motion complete AND button pressed on this same instant,"
    //the completed motion+button combo stays valid for the rest of
    //searchWindow's frames, exactly like real fighting-game input buffering.
    public AttackData TryParseSpecial()
    {
        InputReader.InputFrame[] recent = inputBuffer.GetRecentFrames(searchWindow);
        if (recent.Length == 0) return null;

        //Scan from the most recent frame backward so the freshest valid
        //button press wins if more than one still qualifies.
        for (int i = recent.Length - 1; i >= 0; i--)
        {
            InputReader.InputFrame frame = recent[i];
            bool buttonPressed = frame.lightPressed || frame.mediumPressed || frame.heavyPressed;
            if (!buttonPressed) continue;

            foreach (AttackData move in specialMoves)
            {
                if (!move.IsSpecialMove) continue;

                //Only frames up to and including this button press count -
                //the motion must have completed BEFORE or AT the button, not
                //using frames that come after it.
                if (MotionMatches(move.motionInput, recent, i + 1))
                    return move;
            }
        }

        return null;
    }

    //Which numpad directions are "one step" from each other on the grid,
    //used to allow a skipped diagonal frame (e.g. rolling straight from 2 to
    //6 without a frame ever reading 3) to still count as having passed
    //through it. Only adjacency, not arbitrary jumps - 2 straight to 8 still
    //fails, since that's not a legitimate roll-through.
    private static readonly Dictionary<int, int[]> Adjacent = new Dictionary<int, int[]>
    {
        { 1, new[] { 2, 4 } },
        { 2, new[] { 1, 3 } },
        { 3, new[] { 2, 6 } },
        { 4, new[] { 1, 7 } },
        { 6, new[] { 3, 9 } },
        { 7, new[] { 4, 8 } },
        { 8, new[] { 7, 9 } },
        { 9, new[] { 6, 8 } },
    };

    //Checks frames[0..frameCount) for a matching motion input. A long run of
    //frames holding the same direction only counts once, so holding down for
    //several frames doesn't require several separate matching frames.
    //frameCount lets TryParseSpecial check "did the motion complete by THIS
    //button press" without allocating a new sub-array per attempt.
    bool MotionMatches(string motion, InputReader.InputFrame[] frames, int frameCount)
    {
        int motionIndex = 0;
        int lastDirection = -1;

        for (int i = 0; i < frameCount; i++)
        {
            InputReader.InputFrame frame = frames[i];
            if (motionIndex >= motion.Length) break;

            int wantDigit = motion[motionIndex] - '0';

            if (frame.direction == wantDigit && frame.direction != lastDirection)
            {
                motionIndex++;
            }
            //Diagonal leniency: if this frame skipped straight to the NEXT
            //required digit, and that digit is adjacent to the one we
            //skipped, count both steps at once - the player rolled through
            //the diagonal too fast for it to land on its own frame.
            else if (motionIndex + 1 < motion.Length)
            {
                int nextWantDigit = motion[motionIndex + 1] - '0';
                bool skippedStepIsAdjacent = Adjacent.TryGetValue(wantDigit, out int[] neighbors)
                    && System.Array.IndexOf(neighbors, frame.direction) != -1;

                if (frame.direction == nextWantDigit && skippedStepIsAdjacent && frame.direction != lastDirection)
                {
                    motionIndex += 2;
                }
            }

            lastDirection = frame.direction;
        }

        return motionIndex >= motion.Length;
    }

    //---- Where to take this next (not implemented here on purpose) ----
    //
    // 1. Charge motions (e.g. "[4]6" = hold back 40+ frames, then forward):
    //    needs tracking HOW LONG a direction was held, not just that it
    //    appeared - a different check than simple subsequence matching.
    //
    // 2. Priority between multiple matching specials: if two moves' motions
    //    both matched this frame, decide a tie-break (e.g. prefer the one
    //    needing the longer/more specific motion).
}