using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool turn;
    public bool won;
    public int cheksersCount;

    // Start is called before the first frame update
    void Start()
    {
        turn = false;
        won = false;
        cheksersCount = 15;
    }
}
