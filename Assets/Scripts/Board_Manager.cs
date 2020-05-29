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
    int Moves, movesTaken,availableMovesCount = 0;
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

            if (collidersSet&&availableMovesCount!=0)
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
                            Column onHit;
                            if (PlayerB.turn)
                                onHit = columns[25].GetComponent<Column>();
                            else
                                onHit = columns[24].GetComponent<Column>();

                            bool moved = move(from, to, onHit);

                            if (moved)
                            {
                                movesTaken++;
                            }
                                

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
        int HittedCheckers;
        int pick=0;
        if (PlayerB.turn)
            HittedCheckers = columns[24].transform.childCount;
        else
            HittedCheckers = columns[25].transform.childCount;

        //check if all the 15 checkers of the player are in the final 6 columns
        bool readyToPick = pickingCheckers();
        /*
         * if the player which is his turn to play does not have any hitted checkers
         */
        if (HittedCheckers==0)
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
                                    if(readyToPick)
                                    {
                                        pick = PlayerB.turn ? 24-from.id : from.id +1;
                                        if(!checker.canBePicked)
                                        checker.canBePicked = availableToPick(from.id, pick, die);
                                    }
                                        
                                    if ((PlayerB.turn && target < 24 || readyToPick) || (PlayerA.turn && target >= 0 || readyToPick))
                                    {
                                        //avoid out of bounds exception in the picking phase
                                        if (PlayerB.turn)
                                        {
                                            if (target > 23)
                                                target = 23;
                                        }
                                        else if (PlayerA.turn)
                                            if (target < 0)
                                                target = 0;

                                            Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();

                                        if (checker.canBePicked || children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                                        {
                                            checker.setHighlighted();
                                            availableMovesCount++; 
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

            Checker[] hitted;
            if(PlayerB.turn)
                hitted = PlayerB.transform.GetChild(0).GetComponentsInChildren<Checker>();
            else
                hitted = PlayerA.transform.GetChild(0).GetComponentsInChildren<Checker>();

            foreach(Checker checker in hitted)
                if ((PlayerB.turn && checker.team == "playerB") || (PlayerA.turn && checker.team == "playerA"))
                    if (availability)
                    {
                        foreach (int die in diceToPlay)
                        {
                            //Column from = PlayerB.turn ? PlayerB.transform.GetChild(0).GetComponent<Column>() : PlayerA.transform.GetChild(0).GetComponent<Column>();
                            int target = PlayerB.turn ? die-1 : 24-die;
                            if (PlayerB.turn && target < 24 || PlayerA.turn && target >= 0)
                            {
                                Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();

                                if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                                {
                                    checker.setHighlighted();
                                }
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

        if(!playerAisHit)
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
        
        //if the checker was picked(out of board)
        if(to.id>25)
        {
            if (PlayerB.turn)
                diff = to.id - from.id - 2;
            else
                diff = from.id + 1;
        }
        //normal move
        else if (from.id < 24)
            diff = Math.Abs(from.id - to.id);
        //hiited checker back in board
        else
        {
            if (PlayerB.turn)
                diff = to.id + 1;
            else
                diff = from.id - (to.id + 1);
        }
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
                    if (to.id > 25)
                        to.ajustPickedCheckers();
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
    }

    //Undo the last move
    public void Undo()
    {
        if (moveStack.Count == 0)
            return;

        List<Column> lastMove = moveStack.Pop();
        Column to = lastMove[0];
        Column from = lastMove[1];

            if (from.id<24)
            {
            int diff = Math.Abs(from.id - to.id);
            diceToPlay.Add(diff);

            movesTaken--; 

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

            int outCol = PlayerB.turn ? 26 : 27;

            foreach (Checker checker in checkers)
            {
                if (checker.canMove)
                {
                    int target;
                    if (col.id>23)
                        target = PlayerB.turn ? die - 1 : 24 - die;
                    else
                        target = PlayerB.turn ? col.id + die : col.id - die;

                    if (PlayerB.turn && target < 24 || PlayerA.turn && target >= 0)
                    {
                        Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();

                        if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                        {
                            columns[target].GetComponent<MeshRenderer>().enabled = true;
                        }   
                    }
                    else
                    {
                        // send to winning column
                        if (checker.canBePicked)
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

    /*
     * check if all of the player's checkers are in the final columns
     * in order to start taking checkers out of the board or if he has
     * already started taking them out
     */
    bool pickingCheckers ()
    {
        bool picking= false;
        int checkersSum = 0;
        if (PlayerB.turn)
        {
            for (int i = 0; i < 6; i++)
            {
                checkersSum += columns[18 + i].transform.childCount;
            }
            if (checkersSum == 15 || columns[26].transform.childCount > 0)
                    picking = true;
        }

        else
        {
            for (int i = 0; i < 6; i++)
            {
                checkersSum += columns[i].transform.childCount;
            }
            if (checkersSum == 15 || columns[27].transform.childCount > 0)
                picking = true;
        } 
        return picking;
    }

    /*
     * in the picking state check if there are checkers in the columns before the selected checker
     * in order to determine if by rolling e.g. a 6 i can pick the checker in the 4th column
     */
    bool availableToPick(int position,int pick, int die)
    {
        int checkersBefore = 0;
        if (pick != 0)
        {
            if (pick == die)
                return true;
            else if(pick<die)
            {
                if (PlayerB.turn)
                    for (int i = position - 1; i >= 18; i--)
                        checkersBefore += columns[i].transform.childCount;
                else
                    for (int i = position+1; i<=5;i++)
                        checkersBefore += columns[i].transform.childCount;
                Debug.Log("checkers before"+checkersBefore);
                // if (checkersBefore != 0)
                //   return false;
                //else
                //   return true;
                return checkersBefore != 0 ? false : true;
            }
        }
        return false;
    }
}
