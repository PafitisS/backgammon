using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checker_on_Board : MonoBehaviour
{
    bool OutOfBoard = false;
    
    //Check if the Checker is out of the game
    void OnTriggerStay(Collider Col)
    {
        if (Col.tag == "Checker")
        {
            OutOfBoard = true;
        }
    }

    public bool Out()
    {
        return OutOfBoard;
    }
}
