using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
//using UnityEngine.UIElements;

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
    int Moves, movesTaken, availableMovesCount;
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
        availableMovesCount = 0;
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
                                //checkerselected.setHighlighted();
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

            if (collidersSet && availableMovesCount != 0)
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
                            Column onHit = columns[PlayerB.turn ? 25 : 24].GetComponent<Column>();
                            bool moved = move(from, to, onHit);

                            if (moved) movesTaken++;
                            //check if with the current move taken the player won
                            if (PlayerB.turn && PlayerB.won)
                                Debug.Log("PlayerB won!");
                            else if (PlayerA.turn && PlayerA.won)
                                Debug.Log("PlayerA won!");

                            if (movesTaken == Moves)
                            {
                                checkerselected.setTeamMaterial();
                                state = GameState.Finalizing;
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
            else
                state = GameState.Finalizing;
        }
        else if (state == GameState.Finalizing)
        {
            endButton.interactable = true;
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
        int pick = 0, HittedCheckers = columns[PlayerB.turn ? 24 : 25].transform.childCount;

        //check if all the 15 checkers of the player are in the final 6 columns
        bool readyToPick = pickingCheckers();
        /*
         * if the player which is his turn to play does not have any hitted checkers
         */
        if (HittedCheckers == 0)
        {
            foreach (GameObject col in columns)
            {
                Checker[] checkers = col.transform.GetComponentsInChildren<Checker>();
                foreach (Checker checker in checkers)
                {
                    if (checker.canMove)
                        if ((PlayerB.turn && checker.team == "playerB") || (PlayerA.turn && checker.team == "playerA"))
                            if (availability)
                            {
                                foreach (int die in diceToPlay)
                                {
                                    Column from = col.GetComponent<Column>();
                                    int target = PlayerB.turn ? from.id + die : from.id - die;
                                    if (readyToPick)
                                    {
                                        pick = PlayerB.turn ? 24 - from.id : from.id + 1;
                                        if (!checker.canBePicked)
                                            checker.canBePicked = availableToPick(from.id, pick, die);
                                    }

                                    if ((PlayerB.turn && target < 24 || readyToPick) || (PlayerA.turn && target >= 0 || readyToPick))
                                    {
                                        bool targetChanged = false;
                                        /*
                                         * avoid out of bounds exception in the picking phase
                                         * check if the target has been changed
                                         */
                                        if (PlayerB.turn)
                                        {
                                            if (target > 23)
                                            {
                                                target = 23;
                                                targetChanged = true;
                                            }
                                        }
                                        else
                                            if (target < 0 || from.id == 27)
                                        {
                                            target = 0;
                                            targetChanged = true;
                                        }

                                        Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();

                                        if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                                        {
                                            checker.setHighlighted();
                                            availableMovesCount++;
                                            /*
                                             * in the picking phase highlight the checkers that can be picked
                                             * or can still make a move
                                             */
                                            if (readyToPick && !checker.canBePicked && targetChanged)
                                            {
                                                foreach (int num in diceToPlay)
                                                    if (num < pick)
                                                        if (from.id != 27) //special case for column 27
                                                            checker.canStillMove = true;
                                                /*
                                                 * if checker cannot move or be picked or its out of the board
                                                 * do not highlight it
                                                 */
                                                if (!checker.canStillMove)
                                                    checker.setTeamMaterial();
                                            }
                                        }
                                    }

                                }
                            }
                            else
                                checker.setTeamMaterial();
                }
            }
        }
        /*
         * if the player which is his turn to play has hitted checkers
         */
        else
        {

            Checker[] hitted = (PlayerB.turn ? PlayerB : PlayerA).transform.GetChild(0).GetComponentsInChildren<Checker>();

            foreach (Checker checker in hitted)
                if ((PlayerB.turn && checker.team == "playerB") || (PlayerA.turn && checker.team == "playerA"))
                    if (availability)
                    {
                        foreach (int die in diceToPlay)
                        {
                            //Column from = PlayerB.turn ? PlayerB.transform.GetChild(0).GetComponent<Column>() : PlayerA.transform.GetChild(0).GetComponent<Column>();
                            int target = PlayerB.turn ? die - 1 : 24 - die;
                            if (PlayerB.turn && target < 24 || PlayerA.turn && target >= 0)
                            {
                                Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();
                                if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                                    checker.setHighlighted();
                            }
                        }
                    }
                    else
                        checker.setTeamMaterial();
        }
    }


    //Change the checker's parent(column)
    public bool move(Column from, Column to, Column onHit)
    {
        int checkersInFrom = from.transform.childCount;
        int checkersInTo = to.transform.childCount;
        bool playerAisHit = PlayerA.turn && PlayerA.transform.GetChild(0).childCount > 0;
        bool playerBisHit = PlayerB.turn && PlayerB.transform.GetChild(0).childCount > 0;
        List<Column> playedMove = new List<Column>();

        //check if a player has hitted checkers
        if (!playerBisHit)
            if (PlayerB.turn && from.id >= to.id)
                return false;

        if (!playerAisHit)
            if (PlayerA.turn && from.id <= to.id)
                if (to.id != 27)
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
                    return false;
                //hit a checker scemario
                else
                {
                    toChecker.transform.SetParent(onHit.transform);
                    onHit.adjustCheckers();
                    playedMove.Add(to);
                    playedMove.Add(onHit);
                    moveStack.Push(playedMove);
                }
            }
        }

        playedMove.Add(from);
        playedMove.Add(to);

        moveStack.Push(playedMove);

        // Remove the die that was played
        int diff;


        if (to.id > 25) // checker was picked(out of board)
            diff = PlayerB.turn ? diff = to.id - from.id - 2 : diff = from.id + 1;
        else if (from.id < 24) // normal move
            diff = Math.Abs(from.id - to.id);
        else // hitted checker back in board
            diff = PlayerB.turn ? diff = to.id + 1 : diff = from.id - (to.id + 1);

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
                    /*
                     * when a move is executed and a checker is placed out of the board
                     * check if all the checkers are out of board which mean that a player has won
                     */
                    if (to.id > 25)
                    {
                        to.ajustPickedCheckers();
                        PlayerB.won = PlayerB.turn && to.transform.childCount == 15;
                        PlayerA.won = PlayerA.turn && to.transform.childCount == 15;
                    }

                    else
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
        availableMovesCount = 0;
        diceToPlay.Clear();
    }

    //Undo the last move
    public void Undo()
    {
        if (moveStack.Count == 0)
            return;

        // Undo all the moves made by the player
        while (moveStack.Count != 0)
        {
            int diff = 0;
            List<Column> lastMove = moveStack.Pop();
            Column from = lastMove[1], to = lastMove[0];

            if (from.id < 24)
            {
                diff = Math.Abs(from.id - to.id);
                diceToPlay.Add(diff);
                movesTaken--;
            }
            else if (from.id > 25)
            {
                diff = PlayerB.turn ? from.id - (to.id + 2) : to.id + 1;
                diceToPlay.Add(diff);
                movesTaken--;
            }
            else if (PlayerB.turn && to.id == 24)
            {
                diff = to.id + 1;
                diceToPlay.Add(diff);
            }
            else if(PlayerA.turn && to.id==25)
            {
                diff = 24 - to.id;
                diceToPlay.Add(diff);
            }


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
        setAvailableToMove(true);
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
            Moves = 4;
        }

        // normal throw
        else
        {
            diceToPlay.Add(DiceManager.DiceOutput[0]);
            diceToPlay.Add(DiceManager.DiceOutput[1]);
            Moves = 2;
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
                go.GetComponent<Collider>().enabled = true;

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

            int outCol = PlayerB.turn ? 26 : 27;

            foreach (Checker checker in checkers)
            {
                if (checker.canMove)
                {
                    int target = col.id > 23 ? (PlayerB.turn ? die - 1 : 24 - die) : (PlayerB.turn ? col.id + die : col.id - die);

                    if (PlayerB.turn && target < 24 || PlayerA.turn && target >= 0)
                    {
                        Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();
                        if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                            columns[target].GetComponent<MeshRenderer>().enabled = true;
                    }
                    else
                    {
                        if (checker.canBePicked) // send to winning column
                            columns[outCol].GetComponent<MeshRenderer>().enabled = true;
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
                go.GetComponent<Collider>().enabled = false;

            collidersSet = false;
        }

    }

    /*
     * Disable the column renderers
     */
    void DisableMeshRenderers()
    {
        foreach (GameObject go in columns)
            go.GetComponent<MeshRenderer>().enabled = false;
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

    /*
     * check if all of the player's checkers are in the final columns
     * in order to start taking checkers out of the board or if he has
     * already started taking them out
     */
    bool pickingCheckers()
    {
        int checkersSum = 0;

        for (int i = 0; i < 6; i++)
            checkersSum += columns[(PlayerB.turn ? 18 : 0) + i].transform.childCount;

        return checkersSum == 15 || columns[PlayerB.turn ? 26 : 27].transform.childCount > 0;
    }

    /*
     * in the picking state check if there are checkers in the columns before the selected checker
     * in order to determine if by rolling e.g. a 6 i can pick the checker in the 4th column
     */
    bool availableToPick(int position, int pick, int die)
    {
        int checkersBefore = 0;
        if (pick != 0)
        {
            if (pick == die)
                return true;
            else if (pick < die)
            {
                if (PlayerB.turn)
                    for (int i = position - 1; i >= 18; i--)
                        checkersBefore += columns[i].transform.childCount;
                else
                    for (int i = position + 1; i <= 5; i++)
                        checkersBefore += columns[i].transform.childCount;

                return checkersBefore != 0 ? false : true;
            }
        }
        return false;
    }
}
