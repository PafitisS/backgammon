using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class Dice_Manager_Script : MonoBehaviour
{

    public List<Dice> diceList;
    public int[] DiceOutput= new int[2];
    public UnityEvent EndRollEvent;

    public void RollDice()
    {
        foreach (Dice die in diceList)
            if (die.rolling)
                return;

        foreach (Dice die in diceList)
            die.Roll();
    }

    public void GetDiceNumbers()
    {
        foreach (Dice die in diceList)
            if (die.rolling)
                return;

         DiceOutput[0] = diceList[0].diceNumber;
         DiceOutput[1] = diceList[1].diceNumber;
         EndRollEvent.Invoke();
    }

    //reset the postion of the dice
    public void ResetDice(float mul)
    {
        foreach (Dice die in diceList)
        {
            die.ResetPosition(mul);
        }
    }
}
