using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

public class Board_Manager : MonoBehaviour
{
    public Dice_Manager_Script DiceManager;
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
