using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialUtilities : MonoBehaviour
{
    /*
     * Disable the column renderers
     */
    public static void DisableMeshRenderers(ref GameObject[] columns)
    {
        foreach (GameObject go in columns)
            go.GetComponent<MeshRenderer>().enabled = false;
    }

    /*
    * Disable the column colliders when rolling
    */
    public static void DisableColumnColliders(ref bool collidersSet, ref GameObject[] columns)
    {
        if (collidersSet)
        {
            foreach (GameObject go in columns)
                go.GetComponent<Collider>().enabled = false;

            collidersSet = false;
        }
    }

    /*
    * Enable the column renderers to display which columns are playable
    */
    public static void EnableMeshRenderers(Player PlayerA, Player PlayerB, Checker checkerselected, List<int> diceToPlay, ref GameObject[] columns)
    {
        foreach (int die in diceToPlay)
        {
            Column col = checkerselected.transform.parent.GetComponent<Column>();
            Checker[] checkers = col.transform.GetComponentsInChildren<Checker>();

            int outCol = PlayerB.turn ? 26 : 27;

            foreach (Checker checker in checkers)
            {
                if (checker.canMove)
                {
                    int target = col.id > 23 ? (PlayerB.turn ? die - 1 : 24 - die) : (PlayerB.turn ? col.id + die : col.id - die);

                    if (PlayerB.turn && target < 24 || PlayerA.turn && target >= 0)
                    {
                        Checker[] children = columns[target].GetComponent<Column>().transform.GetComponentsInChildren<Checker>();
                        if (children.Length == 0 || children.Length == 1 || (children.Length > 1 && children[0].team == checker.team))
                            columns[target].GetComponent<MeshRenderer>().enabled = true;

                    }
                    else
                    {
                        if (checker.canBePicked) // send to winning column
                            columns[outCol].GetComponent<MeshRenderer>().enabled = true;
                    }
                }
            }
        }
    }

    /*
    * Enable the column colliders when not in a rolling state
    */
    public static void EnableColumnColliders(ref bool collidersSet, ref GameObject[] columns)
    {
        if (!collidersSet)
        {
            foreach (GameObject go in columns)
                go.GetComponent<Collider>().enabled = true;

            collidersSet = true;
        }
    }
}
