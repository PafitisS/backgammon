using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checker : MonoBehaviour
{
    public string team;
    public bool canMove;
    public Material teamAMaterial;
    public Material teamBMaterial;
    public Material highlightedMaterial;

    // Start is called before the first frame update
    void Awake()
    {
        team = Int32.Parse(transform.name.Substring(7)) < 15 ? "playerA" : "playerB";
        canMove = false;
    }

    public void setMaterial(int materialIndex)
    {
        gameObject.GetComponent<Renderer>().material = materialIndex == 0 ? teamAMaterial : materialIndex == 1 ? teamBMaterial : highlightedMaterial;
    }

    public void CheckerOnBoard()
    {

    }
}
