using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

//Records player input every frame so that special moves and command normals can be performed,
//aslo allows for input buffering in combos. This input is sent to the command parser to actually read
//In the project settings this script has been set to read BEFORE Attack Controller. This is needed
//Because Unity doesn't guarantee the order in which Update()'s resolve.
public class InputReader : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerId;

    public struct InputFrame
    {
        public int frameNumber;
        public int direction;
        public float rawX; 
        public bool isCrouchHeld; 
        public bool isJumpPressed; 
        public bool isJumpHeld; 

        public bool lightPressed;
        public bool mediumPressed;
        public bool heavyPressed;
        public bool tagPressed;
    }

    [Tooltip("How many frames of history to keep. At 60fps, 30 = half a second.")]
    public int bufferSize = 30;

    private Queue<InputFrame> buffer = new Queue<InputFrame>();
    private int frameCounter;
    private InputFrame lastFrame;

    private Fighter fighter;

    //Numpad-notation grid property. Standard for fighting games:
    //5 is neutral, 4 is back, 6 is forward, 8 is up, and 2 is down (array is flipped). The others are diagonals
    private static readonly int[,] NumpadGrid =
    {
        { 1, 2, 3 },
        { 4, 5, 6 },
        { 7, 8, 9 }
    };

    void Awake()
    {
        fighter = GetComponent<Fighter>();
    }

    void Update()
    {
        Record();
    }

    void Record()
    {
        frameCounter++;

        Vector2Int raw = ReadRawDirection();
        int direction = ToNumpadNotation(raw, fighter.facingDir);

        // Map inputs based on Player ID (P1 = WASD, P2 = Arrows)
        if (playerId == 1)
        {
            lastFrame = new InputFrame
            {
                frameNumber = frameCounter,
                direction = direction,
                rawX = raw.x,
                isCrouchHeld = Keyboard.current.sKey.isPressed,
                isJumpPressed = Keyboard.current.wKey.wasPressedThisFrame,
                isJumpHeld = Keyboard.current.wKey.isPressed,
                lightPressed = Keyboard.current.jKey.wasPressedThisFrame,
                mediumPressed = Keyboard.current.kKey.wasPressedThisFrame,
                heavyPressed = Keyboard.current.lKey.wasPressedThisFrame,
                tagPressed = Keyboard.current.uKey.wasPressedThisFrame
            };
        }
        else if (playerId == 2)
        {
            lastFrame = new InputFrame
            {
                frameNumber = frameCounter,
                direction = direction,
                rawX = raw.x,
                isCrouchHeld = Keyboard.current.downArrowKey.isPressed,
                isJumpPressed = Keyboard.current.upArrowKey.wasPressedThisFrame,
                isJumpHeld = Keyboard.current.upArrowKey.isPressed,
                lightPressed = Keyboard.current.numpad1Key.wasPressedThisFrame,
                mediumPressed = Keyboard.current.numpad2Key.wasPressedThisFrame,
                heavyPressed = Keyboard.current.numpad3Key.wasPressedThisFrame,
                tagPressed = Keyboard.current.numpad0Key.wasPressedThisFrame
            };
        }

        buffer.Enqueue(lastFrame);
        while (buffer.Count > bufferSize)
        {
            buffer.Dequeue();
        }
    }

    Vector2Int ReadRawDirection()
    {
        int x = 0, y = 0;
        if (playerId == 1)
        {
            if (Keyboard.current.aKey.isPressed) x -= 1;
            if (Keyboard.current.dKey.isPressed) x += 1;
            if (Keyboard.current.sKey.isPressed) y -= 1;
            if (Keyboard.current.wKey.isPressed) y += 1;
        }
        else if (playerId == 2)
        {
            if (Keyboard.current.leftArrowKey.isPressed) x -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) x += 1;
            if (Keyboard.current.downArrowKey.isPressed) y -= 1;
            if (Keyboard.current.upArrowKey.isPressed) y += 1;
        }
        return new Vector2Int(x, y);
    }

    //Converts x/y (-1, 0, 1) input into the numpad notation, mirroring x when facing
    //left so that forward is always the same digit regardless of side.
    int ToNumpadNotation(Vector2Int raw, float facingDir)
    {
        int x = raw.x;
        if (facingDir < 0) x = -x;

        int col = x + 1;      //-1,0,1 = 0,1,2
        int row = raw.y + 1;  //-1,0,1 = 0,1,2
        return NumpadGrid[row, col];
    }

    //Most recent recorded frame. 
    public InputFrame Latest => lastFrame;

    //Returns up to the count of the most recent frames, oldest first. Used by
    //CommandParser to scan for motion patterns.
    public InputFrame[] GetRecentFrames(int count)
    {
        InputFrame[] all = buffer.ToArray();
        int start = Mathf.Max(0, all.Length - count);
        int length = all.Length - start;
        InputFrame[] result = new InputFrame[length];
        System.Array.Copy(all, start, result, 0, length);
        return result;
    }
}