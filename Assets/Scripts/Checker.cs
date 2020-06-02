using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checker : MonoBehaviour
{
    public string team;
    public bool canMove;
    public bool availableMove;
    public bool canBePicked;
    public Material teamAMaterial;
    public Material teamBMaterial;
    public Material highlightedMaterial;

    // Start is called before the first frame update
    void Awake()
    {
        team = Int32.Parse(transform.name.Substring(7)) < 15 ? "playerA" : "playerB";
        canMove = false;
        canBePicked = false;
        availableMove = false;
        setTeamMaterial();
    }

    //Set the highlighted material
    public void setHighlighted()
    {
        gameObject.GetComponent<Renderer>().material = highlightedMaterial;
    }

    //Set team material
    public void setTeamMaterial()
    {
        gameObject.GetComponent<Renderer>().material = team == "playerA" ? teamAMaterial : teamBMaterial;
    }
}
