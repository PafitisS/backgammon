using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Events;
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
    public Player PlayerA, PlayerB;
    public Button undoButton, rollButton, endButton;

    GameState state;
    Checker checkerselected;
    GameObject[] columns;
    bool collidersSet;
    int availableMoves, movesTaken;
    List<int> diceToPlay = new List<int>();
    Stack<List<Column>> moveStack = new Stack<List<Column>>();


    // Start is called before the first frame update
    void Start()
    {

        initializeColumns();
        initializeListeners();
        collidersSet = false;
        PlayerB.turn = true;
        state = GameState.Rolling;

        rollButton.interactable = false;
        undoButton.interactable = false;
        endButton.interactable = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == GameState.Rolling)
        {
            if (!rollButton.interactable)
                rollButton.interactable = true;

        }
        else if (state == GameState.SelectingChecker)
        {

            if (!undoButton.interactable && movesTaken != 0)
                undoButton.interactable = true;

            //Select the checker that will move
            if (Input.GetMouseButtonDown(0))
            {
                setAvailableToMove(false);
                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
                if (hit)
                {
                    if (hitInfo.transform.gameObject.tag == "Checker")
                    {
                        checkerselected = hitInfo.transform.GetComponent<Checker>();

                        if (checkerselected != null)
                        {
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
                    if (hit)
                    {
                        if (hitInfo.transform.gameObject.tag == "Column")
                        {
                            Column from = columns[getColumnOfChecker(checkerselected)].GetComponent<Column>();
                            Column to = hitInfo.transform.GetComponent<Column>();
                            bool moved = move(from, to);

                            if (moved)
                                movesTaken++;

                            if (movesTaken == availableMoves)
                            {
                                checkerselected.setTeamMaterial();
                                state = GameState.Finalizing;
                                endButton.interactable = true;
                            }
                            else
                            {
                                checkerselected.setTeamMaterial();
                                setAvailableToMove(true);
                                state = GameState.SelectingChecker;

                            }
                        }
                    }
                    DisableColumnColliders();
                    DisableMeshRenderers();
                }
            }
        }
        else if (state == GameState.Finalizing)
        {
            ; // Doing nothing for now
        }
    }

    void initializeListeners()
    {
        // Listener to the end roll event
        DiceManager.EndRollEvent.AddListener(
            EndRollingState
        );

        // Listeners to the button clicks
        rollButton.onClick.AddListener(() => {
            state = GameState.SelectingChecker;
        });

        endButton.onClick.AddListener(() => {
            endButton.interactable = false;
            rollButton.interactable = true;
            state = GameState.Rolling;
            while (moveStack.Count > 0)
                moveStack.Pop();
        });

        undoButton.onClick.AddListener(() => {
            if (endButton.interactable)
            {
                endButton.interactable = false;
                state = GameState.SelectingChecker;
                setAvailableToMove(true);
            }
            Undo();
        });
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

    //Change the checker's parent(column)
    public bool move(Column from, Column to)
    {
        int checkersInFrom = from.transform.childCount;
        int checkersInTo = to.transform.childCount;

        if (PlayerB.turn && from.id >= to.id)
            return false;
        if (PlayerA.turn && from.id <= to.id)
            return false;
        if (to.transform.GetComponent<MeshRenderer>().enabled == false)
            return false;

        if (checkersInTo > 0)
        {
            Checker fromChecker = from.transform.GetChild(0).GetComponent<Checker>();
            Checker toChecker = to.transform.GetChild(0).GetComponent<Checker>();
            if (fromChecker.team != toChecker.team)
            {
                if (checkersInTo > 1)
                {
                    return false;
                }
                // attacking scenario
                else
                {
                    // move the child in the out column
                }
            }
        }

        List<Column> playedMove = new List<Column>();
        playedMove.Add(from);
        playedMove.Add(to);

        moveStack.Push(playedMove);

        // Remove the die that was played
        int diff = Math.Abs(from.id - to.id);
        diceToPlay.Remove(diff);


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
    }

    //Undo the last move
    public void Undo()
    {
        if (moveStack.Count == 0)
            return;

        List<Column> lastMove = moveStack.Pop();
        Column to = lastMove[0];
        Column from = lastMove[1];

        // Add the die that was played
        int diff = Math.Abs(from.id - to.id);
        diceToPlay.Add(diff);

        movesTaken--;

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

    public void EndRollingState()
    {
        // Make sure the function is called once on callback invoked
        if (diceToPlay.Count > 0)
            return;

        rollButton.interactable = false;

        // not thrown dice
        if (DiceManager.DiceOutput[0] == 0 || DiceManager.DiceOutput[1] == 0)
        {
            DiceManager.ResetDice(1f);
            rollButton.interactable = true;
            return;
        }

        movesTaken = 0;

        // thrown and got doubles
        if (DiceManager.DiceOutput[0] == DiceManager.DiceOutput[1])
        {

            // Add the dice number 4 times
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            availableMoves = 4;
        }

        // normal throw
        else
        {
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            diceToPlay.Add(DiceManager.DiceOutput[1]);
            availableMoves = 2;
        }

        state = GameState.SelectingChecker;
        setAvailableToMove(true);
    }

    /*
     * Enable the column colliders when not in a rolling state
     */
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

    /*
     * Enable the column renderers to display which columns are playable
     */
    void EnableMeshRenderers()
    {
        foreach (int die in diceToPlay)
        {
            Column col = checkerselected.transform.parent.GetComponent<Column>();
            Checker[] checkers = col.transform.GetComponentsInChildren<Checker>();
            foreach (Checker checker in checkers)
            {
                if (checker.canMove)
                {

                    int target = PlayerB.turn ? col.id + die : col.id - die;

                    if (PlayerB.turn && target < 25 || PlayerA.turn && target >= 0)
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
    }

    /*
     * Disable the column colliders when rolling
     */
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

    /*
     * Disable the column renderers
     */
    void DisableMeshRenderers()
    {
        foreach (GameObject go in columns)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
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
}
