using UnityEngine;
using System.Collections.Generic;

//Watches InputBuffer for known special-move motion notation and returns the matching AttackData when
//one is completed. AttackController calls TryParseSpecial() once per frame alongside its normal-attack
//handling so that specials take priority when both trigger on the same frame.
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
    //motion had already completed by the time of the correct button
    //strength press within the buffer window.
    public AttackData TryParseSpecial()
    {
        InputReader.InputFrame[] recent = inputBuffer.GetRecentFrames(searchWindow);
        if (recent.Length == 0) return null;

        //Scan from the most recent frame backward so the newest valid
        //button press wins if more than one still qualifies.
        for (int i = recent.Length - 1; i >= 0; i--)
        {
            InputReader.InputFrame frame = recent[i];

            //Determine which strength button if any was pressed on this exact frame
            AttackStrength? pressedStrength = null;
            if (frame.lightPressed) pressedStrength = AttackStrength.Light;
            else if (frame.mediumPressed) pressedStrength = AttackStrength.Medium;
            else if (frame.heavyPressed) pressedStrength = AttackStrength.Heavy;

            if (pressedStrength == null) continue;

            foreach (AttackData move in specialMoves)
            {
                if (!move.IsSpecialMove) continue;

                //The special move must match the exact strength of the button pressed
                if (move.strength != pressedStrength.Value) continue;

                //Only frames up to and including this button press count
                //the motion must have completed BEFORE or AT the button, not
                //using frames that come after it.
                if (MotionMatches(move.motionInput, recent, i + 1))
                {
                    return move;
                }
            }
        }

        return null;
    }

    //Allows leniant motion inputs, standard in all fighting games past street fighter 1. Meaning 
    //diagonals can be skipped when doing a quarter circle for example.
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

    //Checks all the frames for a string of inputs that match a known move
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
            //Diagonal leniency
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
}