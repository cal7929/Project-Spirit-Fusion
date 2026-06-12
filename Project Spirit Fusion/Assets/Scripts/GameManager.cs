using UnityEngine;

public enum GameState
{
    GameStart,
    RoundStart,
    Fighting,
    RoundEnd,
    GameEnd
}

public class GameManager : MonoBehaviour
{
    [Header("Players")]
    public Fighter player1;
    public Fighter player2;

    [Header("Match Settings")]
    public int roundsToWin = 2;         

    public GameState currentState = GameState.GameStart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameStart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GameStart()
    {
        currentState = GameState.GameStart;

        player1.SetOpponent(player2.transform);
        player2.SetOpponent(player1.transform);

        Debug.Log("Game Start");

        //RoundStart();
    }

    void RoundStart()
    {
        
    }

    void RoundEnd()
    {

    }
}
