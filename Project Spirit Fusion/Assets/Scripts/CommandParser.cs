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
    //motion was completed with a button press on this frame.
    public AttackData TryParseSpecial()
    {
        InputReader.InputFrame[] recent = inputBuffer.GetRecentFrames(searchWindow);
        if (recent.Length == 0) return null;

        InputReader.InputFrame last = recent[recent.Length - 1];
        bool buttonThisFrame = last.lightPressed || last.mediumPressed || last.heavyPressed;
        if (!buttonThisFrame) return null;

        foreach (AttackData move in specialMoves)
        {
            if (!move.IsSpecialMove) continue;
            if (MotionMatches(move.motionInput, recent))
                return move;
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

    //Checks the frames for any matching motion inputs. A long run of frames
    //holding the same direction only counts once, so holding down for
    //several frames doesn't require several separate matching frames.
    bool MotionMatches(string motion, InputReader.InputFrame[] frames)
    {
        int motionIndex = 0;
        int lastDirection = -1;

        foreach (InputReader.InputFrame frame in frames)
        {
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
    //
    // 3. Buffering during hitstop/animation lock: if the parser only checks
    //    "button pressed AND motion matched on the same exact frame," players
    //    who finish the motion slightly before their recovery ends will drop
    //    inputs. Consider allowing the motion-complete flag to persist a few
    //    frames waiting for the button, not just the reverse.
}