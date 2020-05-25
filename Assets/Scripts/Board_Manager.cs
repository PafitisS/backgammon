using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using UnityEditor.Build;
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
    private bool collidersSet;
    int availableMoves, maxAMoves, movesTaken;
    List<int> diceToPlay = new List<int>();
    public Button[] buttons = new Button[3];

    // Start is called before the first frame update
    void Start()
    {
        initializeColumns();
        collidersSet = false;
        PlayerB.turn = true;

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
            DiceManager.EndRollEvent.AddListener(EndRollingState);
        }
        else if (state == GameState.SelectingChecker)
        {

            //Highlight the checkers that are available to move
            

            //Select the checker that will move
            if (Input.GetMouseButtonDown(0))
            {
                setAvailableToMove(false);
                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                if (hit)
                {
                    Debug.Log(hitInfo.transform.gameObject.tag);
                    if (hitInfo.transform.gameObject.tag == "Checker")
                    {
                        checkerselected = hitInfo.transform.GetComponent<Checker>();

                        if (checkerselected != null)
                        { 
                            Debug.Log(getColumnOfChecker(checkerselected));
                            // if the checker can move, highlight it
                            if (checkerselected.canMove)
                            {
                                checkerselected.setHighlighted();
                                state = GameState.SelectingColumn;
                            }
                        }
                    }
                    else
                    {
                        if (checkerselected != null)
                        {
                            checkerselected.setTeamMaterial();
                        }
                    }
                }
            }
        }
        else if (state == GameState.SelectingColumn)
        {
            // enable the colliders
            EnableColumnColliders();
            EnableMeshRenderers();

            if (collidersSet)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit hitInfo = new RaycastHit();
                    bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                    if (availableMoves != 0) { 
                        if (hit)
                        {
                            if (hitInfo.transform.gameObject.tag == "Column")
                            {

                                columnSelected = hitInfo.transform.GetComponent<Column>();

                                // move the checker that is selected and adjust the column
                                Column from = columns[getColumnOfChecker(checkerselected)].GetComponent<Column>();
                                Column to = columnSelected;
                                if (move(from, to))
                                    state = GameState.Finalizing;
                                else
                                {
                                    checkerselected.setTeamMaterial();
                                    state = GameState.SelectingChecker;
                                }
                            }

                            else
                            {
                                //
                                checkerselected.setTeamMaterial();
                                state = GameState.SelectingChecker;
                            }

                        
                        }
                    }
                    DisableMeshRenderers();
                }
            }

        }
        else if (state == GameState.Finalizing)
        {
            buttons[2].enabled = true;
            state = GameState.Rolling;
        }
    }

    void setAvailableToMove(bool availability)
    {
        foreach (GameObject col in columns)
        {
            Checker[] checkers = col.transform.GetComponentsInChildren<Checker>();
            foreach (Checker checker in checkers)
            {
                if (checker.canMove)
                    if ((PlayerB.turn && checker.team == "playerB") || (PlayerA.turn && checker.team == "playerA"))
                        if (availability)
                            checker.setHighlighted();
                        else
                            checker.setTeamMaterial();
            }
        }
    }


    void initializeColumns()
    {
        columns = GameObject.FindGameObjectsWithTag("Column");
        for (int i = 0; i < columns.Length - 1; i++)
        {
            for (int j = i + 1; j < columns.Length; j++)
            {
                if (columns[j].GetComponent<Column>().id < columns[i].GetComponent<Column>().id)
                {
                    GameObject temp = columns[j];
                    columns[j] = columns[i];
                    columns[i] = temp;
                }
            }
        }
    }

    //Change the checker's parent(column)
    public bool move(Column from, Column to)
    {
        Debug.Log("Moving from " + from.transform.name + " to " + to.transform.name);
        int checkersInFrom = from.transform.childCount;
        int checkersInTo = to.transform.childCount;

        if ( PlayerB.turn && from.id >= to.id)
            return false;
        if (PlayerA.turn && from.id <= to.id)
            return false;


            if (checkersInFrom != 0)
        {
            Checker[] children = from.transform.GetComponentsInChildren<Checker>();
            
            foreach (Checker child in children)
            {
                if (child.canMove == true)
                {
                    child.transform.SetParent(to.transform);
                    from.adjustCheckers();
                    to.adjustCheckers();
                    break;
                }
            }
        }
        return true;
    }
    int getColumnOfChecker(Checker checker)
    {
        return checker.transform.parent.GetComponent<Column>().id;
    }

    //End current turn and reset the dice accordingly
    public void EndTurn()
    {
        PlayerA.turn = !PlayerA.turn;
        PlayerB.turn = !PlayerB.turn;
        DiceManager.ResetDice(-1f);
        DisableColumnColliders();
    }

    //Undo the last move
    public void Undo ()
    {

    }

    public void EndRollingState()
    {
        buttons[0].enabled = false;
        // not thrown dice
        if (DiceManager.DiceOutput[0] == 0 || DiceManager.DiceOutput[1] == 0)
        {
            DiceManager.ResetDice(1f);
            buttons[0].enabled = true;
        }
        // thrown and got doubles
        else if (DiceManager.DiceOutput[0] == DiceManager.DiceOutput[1] && DiceManager.DiceOutput[0] != 0)
        {
            for (int i = 0; i < 4; i++)
            {
                diceToPlay.Add(DiceManager.DiceOutput[0]);
            }
            maxAMoves = 4;
            availableMoves = 4;
            state = GameState.SelectingChecker;
            setAvailableToMove(true);
        }
        // thrown
        else
        {
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            diceToPlay.Add(DiceManager.DiceOutput[1]);
            maxAMoves = 2;
            availableMoves = 2;
            state = GameState.SelectingChecker;
            setAvailableToMove(true);
        }
    }

    void EnableColumnColliders()
    {
        if (!collidersSet)
        {
            foreach (GameObject go in columns)
            {
                go.GetComponent<Collider>().enabled = true;
            }
            collidersSet = true;
        }
    }

    void EnableMeshRenderers()
    {
        foreach (int die in diceToPlay)
        {
            if (PlayerB.turn)
            {
                Column col = checkerselected.transform.parent.GetComponent<Column>();
                Checker[] checkers = col.transform.GetComponentsInChildren<Checker>();
                foreach (Checker checker in checkers)
                {
                    if (checker.canMove && checker.team == "playerB")
                    {
                        int target = col.id + die;
                        if (target < 25)
                        {
                            Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();

                            if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                                columns[target].GetComponent<MeshRenderer>().enabled = true;
                        }
                        else
                        {
                            ;// send to winning column
                        }

                    }
                }
            }
            else
            {

            }
        }
    }

    void DisableColumnColliders()
    {
        if (collidersSet)
        {
            foreach (GameObject go in columns)
            {
                go.GetComponent<Collider>().enabled = false;
            }
            collidersSet = false;
        }

    }

    void DisableMeshRenderers()
    {
        foreach (GameObject go in columns)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
