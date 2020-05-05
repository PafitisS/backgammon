using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Check_Dice_Side : MonoBehaviour
{
    bool onGround;
    public int sideValue;
    

    //check if the die has stopped moving
    void OnTriggerStay(Collider Col)
    {
       if(Col.tag=="BottomSurface")
        {
            onGround = true;
        }            
    }
    private void OnTriggerExit(Collider Col)
    {
        if (Col.tag == "BottomSurface")
        {
            onGround = false;
        }
    }

    public bool OnGround()
    {
        return onGround;
    }
}
