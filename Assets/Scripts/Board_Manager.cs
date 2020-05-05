using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

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
    // Start is called before the first frame update
    void Start()
    {
        initializeColumns();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == GameState.Rolling)
        {
            if (Input.GetKeyDown("space"))
            {
                DiceManager.RollDice();
                state = GameState.SelectingChecker;
            }
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
            DiceManager.ResetDice(-1f);
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

    IEnumerator WaitaBit()
    {
        yield return new WaitForSeconds(2);
    }

    //End current turn and reset the dice accordingly
    public void EndTurn()
    {
        PlayerA.MyTurn = !PlayerA.MyTurn;
        PlayerB.MyTurn = !PlayerB.MyTurn;
        EndTurnResetDice(); 
    }

    //Reset the position of the dice based on player's turn
    void EndTurnResetDice()
    {
        if (PlayerA.MyTurn)
        {
            Debug.Log("A");
            foreach (Dice die in DiceManager.diceList)
            {
                die.transform.SetPositionAndRotation(new Vector3(die.startPosition.x * -1f,
                   die.startPosition.y, die.startPosition.z * -1), die.startRotation);
                die.multiplier = 1;
                die.startPosition = transform.position;

            }
        }
        else if(PlayerB.MyTurn)
        {
            Debug.Log("B");
            foreach (Dice die in DiceManager.diceList)
            {
                die.transform.SetPositionAndRotation(new Vector3(die.startPosition.x * -1f,
                   die.startPosition.y, die.startPosition.z * -1), die.startRotation);
                die.multiplier = -1;
                die.startPosition = transform.position;

            }
        }
    }

    public void Undo ()
    {

    }
}
