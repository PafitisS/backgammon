using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
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

    const string playerMsg = "Player's turn", opponentMsg = "Opponent's turn", winMsg = "You win!!!", loseMsg = "You lose!!!";
    public TMP_Text gameStatusText;
    public Dice_Manager_Script DiceManager;
    public Player PlayerA, PlayerB;
    public Button undoButton, rollButton, endButton;
    public GameState state;
    public Checker checkerselected;
    public GameObject[] columns;
    bool collidersSet, firstTimePlay = true;
    public int Moves, movesTaken, availableMovesCount, set;
    List<int> diceToPlay = new List<int>();
    Stack<List<Column>> moveStack = new Stack<List<Column>>();
    bool AIEnabled = false;


    // Start is called before the first frame update
    void Start()
    {

        initializeColumns();
        initializeListeners();
        collidersSet = false;
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
            if (firstTimePlay)
                decideWhoGoesFirst();

            if (!rollButton.interactable)
                rollButton.interactable = true;
            else if (AIEnabled)
                    rollButton.onClick.Invoke();
        }
        else if (state == GameState.SelectingChecker)
        {
            if (!undoButton.interactable && movesTaken != 0)
                undoButton.interactable = true;

            if (AIEnabled)
                StartCoroutine(selectCheckerAfterSeconds(1f));
            else
                selectChecker();
        }
        else if (state == GameState.SelectingColumn)
        {
            // enable the colliders
            MaterialUtilities.EnableColumnColliders(ref collidersSet, ref columns);
            MaterialUtilities.EnableMeshRenderers(PlayerA, PlayerB, checkerselected, diceToPlay, ref columns);

            if (collidersSet)
            {
                if (AIEnabled)
                    StartCoroutine(selectTargetAfterSeconds(1f));
                else
                    selectTarget();
            }
            else
                state = GameState.Finalizing;
        }
        else if (state == GameState.Finalizing)
        {
            if (!endButton.interactable) 
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
            undoButton.interactable = false;
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
                SetAvailableToMove();
            }
            Undo();
        });
    }

    void SetAvailableToMove()
    {
        int HittedCheckers = columns[PlayerB.turn ? 24 : 25].transform.childCount;
        bool readyToPick = pickingCheckers(HittedCheckers);
        availableMovesCount = 0;
        //sort the dice from bigger to smaller
        if (diceToPlay.Count==2)
        {
            if(diceToPlay[0]<diceToPlay[1])
            {
                int temp = diceToPlay[0];
                diceToPlay[0] = diceToPlay[1];
                diceToPlay[1] = temp;
            }
        }

        foreach (GameObject col in columns)
        {
            Checker[] checkers;
            //check if the player has hitted checkers
            if (HittedCheckers==0)
                checkers = col.transform.GetComponentsInChildren<Checker>();
            else
                checkers= (PlayerB.turn ? PlayerB : PlayerA).transform.GetChild(0).GetComponentsInChildren<Checker>();

            foreach (Checker checker in checkers)
            {
                if (checker.canMove)
                    if ((PlayerB.turn && checker.team == "playerB") || (PlayerA.turn && checker.team == "playerA"))
                        foreach (int die in diceToPlay)
                        {
                            Column from = col.GetComponent<Column>();
                            int target = PlayerB.turn ? from.id + die : from.id - die;
                            //if the player has hitted checkers
                            if (HittedCheckers != 0)
                            {
                                target = PlayerB.turn ? die - 1 : 24 - die;
                            }
                            //check if the player is ready to start picking checkers
                            else if (readyToPick)
                            {
                                int pick = PlayerB.turn ? 24 - from.id : from.id + 1;
                                if (PlayerB.turn && target > 23)
                                {
                                    target = 26;
                                    checker.canBePicked = availableToPick(from.id, pick, die);
                                }
                                else if (PlayerA.turn && target < 0)
                                {
                                    target = 27;
                                    checker.canBePicked = availableToPick(from.id, pick, die);
                                }
                            }

                            if ((PlayerB.turn && target < 24) || (PlayerA.turn && target >= 0) || readyToPick)
                            {
                                Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();

                        //highlight the checkers that are available to move
                                if (((children.Length == 0 && target < 24) || (children.Length == 1 && target < 24) || (children.Length > 1 && children[0].team == checker.team && target < 24) || checker.canBePicked) && from.id < 26)
                                {
                                        checker.setHighlighted();
                                        ++availableMovesCount;
                                }   
                            }
                        }
            }
        }
    }

    void setUnavailableToMove()
    {
        foreach (GameObject col in columns)
        {
            Checker[] checkers= col.transform.GetComponentsInChildren<Checker>();
            foreach (Checker checker in checkers)
                checker.setTeamMaterial();
        }
    }
    
    //Change the checker's parent(column)
    public bool move(Column from, Column to, Column onHit)
    {
        if (from == null || to == null || onHit == null)
            return false;

        int checkersInFrom = from.transform.childCount, checkersInTo = to.transform.childCount;
        bool playerAisHit = PlayerA.turn && PlayerA.transform.GetChild(0).childCount > 0;
        bool playerBisHit = PlayerB.turn && PlayerB.transform.GetChild(0).childCount > 0;
        List<Column> playedMove = new List<Column>(), playedMove1 = new List<Column>();

        //check if a player has hitted checkers
        if (!playerBisHit && PlayerB.turn && from.id >= to.id)
            return false;

        if (!playerAisHit && PlayerA.turn && from.id <= to.id && to.id != 27)
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
                    playedMove1.Add(to);
                    playedMove1.Add(onHit);
                    moveStack.Push(playedMove1);
                }
            }
        }

        if (to.transform.GetComponent<MeshRenderer>().enabled == false)
            return false;

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
                     * check if all the checkers are out of board which means that a player has won
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
        AIEnabled = !AIEnabled;
        gameStatusText.text = PlayerA.turn ? opponentMsg : playerMsg;
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
            Debug.Log("Column from "+from+" Column to "+ to);

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
        SetAvailableToMove();
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
        SetAvailableToMove();
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
    bool pickingCheckers(int hitted)
    {
        int checkersSum = 0;

        if (hitted != 0)
            return false;
        for (int i = 0; i < 6; i++)
        {
            int id = (PlayerB.turn ? 18 : 0) + i;
            int checkerInColumn= columns[id].transform.childCount;
            if (checkerInColumn > 0)
            {

                string team = columns[id].transform.GetChild(0).GetComponent<Checker>().team;
                if ((team == "playerB" && PlayerB.turn) || (team == "playerA" && PlayerA.turn))
                    checkersSum += columns[id].transform.childCount;
            }
        }

        checkersSum += columns[PlayerB.turn ? 26 : 27].transform.childCount;
        return checkersSum == 15;
    }

    /*
     * in the picking state check if there are checkers in the columns before the selected checker
     * in order to determine if by rolling e.g. a 6 i can pick the checker in the 4th column
     */
    bool availableToPick(int position, int pick, int die)
    {
        int checkersBefore = 0;

        if (pick == die)
            return true;
        else if (pick < die)
        {
            if (PlayerB.turn)
                for (int i = position - 1; i >= 18; i--)
                    checkersBefore += columns[i].transform.childCount > 0 && columns[i].transform.GetChild(0).GetComponent<Checker>().team == "playerB" ? columns[i].transform.childCount : 0;
                        
            else
                for (int i = position + 1; i <= 5; i++)
                    checkersBefore += columns[i].transform.childCount > 0 && columns[i].transform.GetChild(0).GetComponent<Checker>().team == "playerA" ? columns[i].transform.childCount : 0;

            return checkersBefore != 0 ? false : true;
        }
        return false;
    }

    void decideWhoGoesFirst()
    {
        firstTimePlay = false;
        int rand = UnityEngine.Random.Range(0, 100);
        if (rand < 50)
        {
            PlayerA.turn = true;
            AIEnabled = true;
            gameStatusText.text = opponentMsg;
            DiceManager.ResetDice(-1f);
            if (AIEnabled)
                rollButton.onClick.Invoke();
        }
        else
        {
            gameStatusText.text = playerMsg;
            PlayerB.turn = true;
        }
    }

    void selectChecker()
    {
        //Select the checker that will move
        if (Input.GetMouseButtonDown(0))
        {
            SetAvailableToMove();
            //if there are no available moves enable end turn button
            if (availableMovesCount == 0)
                state = GameState.Finalizing;

            RaycastHit hitInfo = new RaycastHit();
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo);
            if (hit)
            {
                if (hitInfo.transform.gameObject.tag == "Checker")
                {
                    checkerselected = hitInfo.transform.GetComponent<Checker>();

                    if (checkerselected != null && hitInfo.transform.parent.transform.GetComponent<Column>().id < 26)
                    {
                        // if the checker can move, highlight it
                        if (checkerselected.team == "playerA" && PlayerA.turn || checkerselected.team == "playerB" && PlayerB.turn)
                            state = GameState.SelectingColumn;
                    }
                }
            }
        }
    }

    IEnumerator selectCheckerAfterSeconds(float x)
    {
        yield return new WaitForSeconds(x);

        SetAvailableToMove();
        if (availableMovesCount == 0)
            state = GameState.Finalizing;
        else 
        {
            if (checkerselected == null)
            {
                checkerselected = AI.getSelectedChecker(ref columns);
                state = GameState.SelectingColumn;
            }
        }
    }


    void selectTarget()
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

                    if (moved)
                    {
                        movesTaken++;
                        checkerselected = null;
                    }
                    //check if with the current move taken the player won
                    if (PlayerB.turn && PlayerB.won)
                    {
                        gameStatusText.text = winMsg;
                        Invoke("GoToScene", 5);
                    }
                    else if (PlayerA.turn && PlayerA.won)
                    {
                        gameStatusText.text = loseMsg;
                        Invoke("GoToScene", 5);
                    }

                    if (movesTaken == Moves)
                    {
                        setUnavailableToMove();
                        state = GameState.Finalizing;
                    }
                    else
                    {
                        setUnavailableToMove();
                        SetAvailableToMove();
                        state = GameState.SelectingChecker;
                    }
                }
            }

            MaterialUtilities.DisableColumnColliders(ref collidersSet, ref columns);
            MaterialUtilities.DisableMeshRenderers(ref columns);
        }
    }

    IEnumerator selectTargetAfterSeconds(float x)
    {
        yield return new WaitForSeconds(x);
        if (checkerselected != null)
        {
            Column from = columns[getColumnOfChecker(checkerselected)].GetComponent<Column>();
            Column to = AI.getTargetColumn(ref columns);
            Column onHit = columns[PlayerB.turn ? 25 : 24].GetComponent<Column>();
            bool moved = move(from, to, onHit);

            if (moved)
            {
                checkerselected = null;
                movesTaken++;
            }
            if (PlayerA.won)
            {
                gameStatusText.text = loseMsg;
                Invoke("GoToScene", 5);
            }
            if (movesTaken == Moves)
            {
                setUnavailableToMove();
                state = GameState.Finalizing;
            }
            else
            {
                setUnavailableToMove();
                SetAvailableToMove();
                state = GameState.SelectingChecker;
            }
            MaterialUtilities.DisableColumnColliders(ref collidersSet, ref columns);
            MaterialUtilities.DisableMeshRenderers(ref columns);
        }
    }
    void GoToScene()
    {
        SceneManager.LoadScene("Backgammon");
    }
}
