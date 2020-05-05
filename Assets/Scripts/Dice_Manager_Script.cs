using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class Dice_Manager_Script : MonoBehaviour
{

    public List<Dice> diceList;
    public static int[] DiceOutput= new int[2];
    public UnityEvent EndRollEvent;

    public void Update()
    {
        
    }

    public void RollDice()
    {
        foreach (Dice die in diceList)
        {
            if (die.rolling)
                return;
        }

        foreach (Dice die in diceList)
        {
            die.Roll();
        }
    }

    public void GetDiceNumbers()
    {

        //store the output of the dice in the array
        DiceOutput[0] = diceList[0].diceNumber;
        DiceOutput[1] = diceList[1].diceNumber;
        Debug.Log(DiceOutput[0]);
        Debug.Log(DiceOutput[1]);

        EndRollEvent.Invoke();
    }
}
