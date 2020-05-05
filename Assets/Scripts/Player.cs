using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool MyTurn;
    public bool IWin;
    public int cheksersCount;

    // Start is called before the first frame update
    void Start()
    {
        MyTurn = false;
        IWin = false;
        cheksersCount = 15;
    }

    // Update is called once per frame
    void Update()
    {
        if(cheksersCount==0)
        {
            IWin = true;
        }
    }

}
