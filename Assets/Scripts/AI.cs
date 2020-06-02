using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{

    Board_Manager manager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (manager.state == Board_Manager.GameState.Rolling)
        {
            manager.DiceManager.RollDice();
        }
        else if (manager.state == Board_Manager.GameState.SelectingChecker)
        {
            calculateMoves();
        }
        else if (manager.state == Board_Manager.GameState.SelectingColumn)
        {

        }
        else if (manager.state == Board_Manager.GameState.Finalizing)
            manager.EndTurn();
    }

    void calculateMoves()
    {
        foreach (GameObject col in manager.columns)
        {
            Checker[] checkers;
            checkers = col.transform.GetComponentsInChildren<Checker>();
            foreach(Checker checker in checkers)
            {
                if(checker.availableMove)
                {
                    Column from = col.GetComponent<Column>();
                    Column to;
                    if(checker.canBePicked)
                    {

                    }
                }
            }
        }
    }
}
