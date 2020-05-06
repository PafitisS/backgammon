using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.UI;

public class Board_Manager : MonoBehaviour
{
    public enum GameState
    {
        Rolling,
        SelectingChecker,
        SelectingColumn,
        Finalizing
    };

    public Dice_Manager_Script DiceManager;
    GameState state;
    Checker checkerselected;
    Column columnSelected;
    GameObject[] columns;
    public Player PlayerA, PlayerB;
    int availableMoves,maxAMoves, movesTaken;
    public Button[] buttons = new Button[3];

    // Start is called before the first frame update
    void Start()
    {
        initializeColumns();
        //buttons[0].enabled = false;
        //buttons[1].enabled = false;
        //buttons[2].enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == GameState.Rolling)
        {
            buttons[0].enabled = true;  
        }
        else if (state == GameState.SelectingChecker)
        {

            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                if (hit)
                {
                    Debug.Log("Hit " + hitInfo.transform.gameObject.name);
                    if (hitInfo.transform.gameObject.tag == "Checker")
                    {
                        checkerselected = hitInfo.transform.GetComponent<Checker>();

                        // if the checker can move, highlight it
                        if (checkerselected.canMove)
                        {
                            checkerselected.setHighlighted();

                            state = GameState.SelectingColumn;
                        }
                    }
                    else
                    {
                        if (checkerselected != null)
                        {
                            checkerselected.setTeamMaterial();
                            checkerselected = null;
                        }
                    }
                }
            }
        }
        else if (state == GameState.SelectingColumn)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                if (hit)
                {
                    Debug.Log("Hit " + hitInfo.transform.gameObject.name);
                    if (hitInfo.transform.gameObject.tag == "Column")
                    {
                        columnSelected = hitInfo.transform.GetComponent<Column>();

                        if (checkerselected != null)
                        {
                            // move the checker that is selected and adjust the column
                            move(columns[getColumnOfChecker(checkerselected)].GetComponent<Column>(), columnSelected);
                            state = GameState.Finalizing;
                        }
                        else
                        {
                            if (checkerselected != null)
                            {
                                checkerselected.setTeamMaterial();
                                checkerselected = null;
                            }
                            state = GameState.SelectingChecker;
                        }
                    }
                    else
                    {
                        if (checkerselected != null)
                        {
                            checkerselected.setTeamMaterial();
                            checkerselected = null;
                        }
                        state = GameState.SelectingChecker;
                    }

                }
            }
        }
        else if (state == GameState.Finalizing)
        {
            buttons[2].enabled = true;
        }
    }

    void initializeColumns()
    {
        columns = GameObject.FindGameObjectsWithTag("Column");
        for (int i = 0; i < columns.Length - 1; i++)
            for (int j = i + 1; j < columns.Length; j++)
                if (columns[j].GetComponent<Column>().id < columns[i].GetComponent<Column>().id)
                {
                    GameObject temp = columns[j];
                    columns[j] = columns[i];
                    columns[i] = temp;
                }
    }

    //Change the checker's parent(column)
    public void move(Column from, Column to)
    {
        int checkersInFrom = from.transform.childCount;
        int checkersInTo = to.transform.childCount;

        if (checkersInFrom != 0)
        {
            from.transform.SetParent(to.transform);
            from.adjustCheckers();
            to.adjustCheckers();
        }
    }
    int getColumnOfChecker(Checker checker)
    {
        return checker.transform.parent.GetComponent<Column>().id;
    }

    //2 seconds timer
    IEnumerator WaitaBit()
    {
        yield return new WaitForSeconds(2);
    }

    //End current turn and reset the dice accordingly
    public void EndTurn()
    {
        PlayerA.MyTurn = !PlayerA.MyTurn;
        PlayerB.MyTurn = !PlayerB.MyTurn;
        DiceManager.ResetDice(-1f);
    }

    //Undo the last move
    public void Undo ()
    {

    }

    public void EndRollingState()
    {
        buttons[0].enabled = false;
        if (DiceManager.DiceOutput[0] == 0 || DiceManager.DiceOutput[1] == 0)
        {
            DiceManager.ResetDice(1f);
            buttons[0].enabled = true;
        }
            
        else if (DiceManager.DiceOutput[0] == DiceManager.DiceOutput[1])
        {
            maxAMoves = 4;
            availableMoves = 4;
        }
        else
        {
            maxAMoves = 2;
            availableMoves = 2;
        }
        state = GameState.SelectingChecker;
    }
}
