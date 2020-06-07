using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public static Checker getSelectedChecker(ref GameObject[] columns)
    {
        List<Checker> checkers=new List<Checker>();
        foreach (GameObject col in columns)
            foreach(Checker checker in col.transform.GetComponentsInChildren<Checker>())
                if(checker.isHighlighted)
                    checkers.Add(checker);

        return checkers.Count > 0 ? checkers[UnityEngine.Random.Range(0, checkers.Count)] : null; ;
    }

    public static Column getTargetColumn(ref GameObject[] columns)
    {
        List<Column> cols = new List<Column>();
        foreach (GameObject col in columns)
            if (col.transform.GetComponent<MeshRenderer>().enabled)
                cols.Add(col.GetComponent<Column>());

        return cols.Count > 0 ? cols[UnityEngine.Random.Range(0, cols.Count)] : null;
    }
}
