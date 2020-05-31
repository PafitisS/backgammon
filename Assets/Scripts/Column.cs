using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Column : MonoBehaviour
{
    //column name set manually
    public int id;
    //side of the board where the column is placed up or down
    public string side;
    public float xStart, offset;
    // Start is called before the first frame update
    void Start()
    {
        offset = 0.17f;

        if(id>11)
        {
            side = "down";
            xStart=0.42f;
            offset *= -1;
        }
        else
        {
            side = "up";
            xStart = -0.42f;
        }

        adjustCheckers();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //adjust the checkers in the current column
    public void adjustCheckers()
    {
        int count = 0;
        Checker[] checkers = transform.GetComponentsInChildren<Checker>();

        float height = -1f;
        foreach (Checker checker in checkers)
        {
            float x, y, z;

            /*
             * if there are already 6 checkers in the current column
             *place the next checkers above the existing checkers forming
             *another layer on the column
             */
            if(count%6==0)
            {
                height += 0.9f;
            }

            x = 0f;
            y = height;
            z = xStart + offset * (count%6);

            checker.transform.localPosition = new Vector3(x, y, z);
            checker.canMove = false;
            checker.setTeamMaterial();
            count++;
        }
        if (count != 0)
            checkers[count - 1].canMove = true;
    }


    //Out of board checkers adjustment
    public void ajustPickedCheckers()
    {
        int count = 0;
        Checker[] checkers = transform.GetComponentsInChildren<Checker>();

        float height = -1f;
        foreach (Checker checker in checkers)
        {
            float x, y, z;

            /*
             * 5 checkers per column layer
             */
            if (count % 5 == 0)
            {
                height += 0.9f;
            }

            x = 0f;
            y = height;
            z = xStart + offset * (count % 5);

            checker.transform.localPosition = new Vector3(x, y, z);
            checker.canMove = false;
            checker.canBePicked = false;
            checker.setTeamMaterial();
            count++;  
        }
        if (count != 0)
                checkers[count - 1].canMove = true;
    }
}
