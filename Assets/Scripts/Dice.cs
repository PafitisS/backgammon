using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class Dice : MonoBehaviour
{
    Rigidbody rb;
    //Check if the die is rolling
    public bool rolling;
    //outcome of the die
    public int diceNumber;
    //dorces required to toss the die
    public float speed = 8f;
    
    //array of die sides
    public Check_Dice_Side[] diceSides;

    //Keep the dice intitial position and rotation
    public Vector3 startPosition;
    public Quaternion startRotation;

    //Values 1 or -1 according to which player is playing to 
    //change the position and the diraction of the force accordingly
    public float multiplier;

    //This event is called when the dice stops moving.
    public UnityEvent RollEvent;

    

    void Start()
    {
        rb=this.GetComponent<Rigidbody>();
        rb.isKinematic = true; // disable physics

        //Get initial position and initliaze multiplier
        this.startPosition=transform.position;
        this.startRotation=transform.rotation;
        multiplier = 1;

    }

    // Update is called once per frame
    void Update()
    {
        if (rolling)
        {
            if (rb.IsSleeping())
            {
                rb.isKinematic = true;
                //multiplier *= -1;

                rolling = false;
                SideCheck();
                RollEvent.Invoke();
            }
        }
    }


    //If the die is not rolling set rolling to true and add force to the die to roll it.
        public void Roll()
        {   
            if (!rolling)
            {
            rolling = true;
            rb.isKinematic = false;
            rb.AddForce(transform.forward * Random.Range(60, 175) * speed * multiplier);
            rb.AddForce(transform.up * Random.Range(0, 18) * speed);
            rb.AddTorque(Random.Range(50, 500), Random.Range(50, 500), Random.Range(50, 500));
            }
        }

        void SideCheck()
        {
            //initilaze the output of the die to zero
            diceNumber = 0;
            foreach(Check_Dice_Side side in diceSides)
            {
                if(side.OnGround())
                diceNumber = side.sideValue;
            }
        }

    //reset dice position
    public void ResetPosition(float mul)
    {
        transform.SetPositionAndRotation(new Vector3(startPosition.x * mul, startPosition.y, startPosition.z * mul), startRotation);
        multiplier *= mul;
        startPosition = transform.position;
    }
}
