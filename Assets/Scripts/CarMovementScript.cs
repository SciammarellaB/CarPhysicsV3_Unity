using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarMovementScript : MonoBehaviour
{
    public float brakeTorque; // USED TO BRAKE THE CAR WITH ABS
    public float brakeMaxTorque; // USED TO BRAKE THE CAR WITHOUT ABS
    public float steeringAngle; //USED TO MAKE THE CAR TURN DURING MOVEMENT
    public float steeringMaxAngle; //USED TO MAKE THE WHEEL TURN STOPPED
    public float engineMaxTorque; //USED TO NOTHING
    public float engineTorque; //USED TO MAKE TORQUE TO THE WHEELS
    public float engineRPM; // USED TO INTERACT WITH GEARS,TURBO AND GAUGE
    public float maxSpeed; //USED TO LIMIT THE CAR TOP SPEED BASED ON THE GEAR
    public float currentSpeed; //USED TO MAKE THE CALCULATIONS AND KNOW THE CAR SPEED
    public float[] wheelSpeed; //USED TO ABS SYSTEM
    public float gearPos; //USED TO THE SWITCH OF THE GEAR    
    public float horizontal, vertical; // USED TO GET INPUT VALUES OF THE AXIS
    public float accelerator; //USED TO MAKE THE CAR RUN

    public WheelCollider[] wheelPhysics; // USED TO INTERACT WITH WHEELCOLLIDER COMPONENT

    public Transform[] wheelBody;  // USED TO MAKE THE VISUAL OF THE WHEELS SPIN AND TURN

    public AudioSource engineSound; //USED TO MAKE THE ENGINE SOUND OF THE CAR

    public Rigidbody rb; //USED TO CALCULATE PHYSICS PROPERTIES

    public bool automatic; //CHANGE FROM AUTO TO MANUAL
    public bool ABS; //USED TO MAKE THE CAR BRAKE BETTER

    public Text[] carTexts;    

    void Start()
    {
        brakeMaxTorque = 3000;
        steeringMaxAngle = 35;
        engineMaxTorque = 154.85f;
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        accelerator = Input.GetAxis("Accelerator") * -1;
              
        Engine();
        GearShift();
    }

    private void FixedUpdate()
    {
        WheelSpeeds();
        BrakeForce();
        Gears();
        Steering();
        WheelMeshUpdate();
    }

    void Engine()
    {
        //ACORDING TO CALCULATORS > A engine with 6900 RPM and 150 Hp should give 154,84 Torque force
                
        engineSound.pitch = Mathf.Lerp(0.5f,3.0f,(currentSpeed/maxSpeed));

        engineRPM = (Mathf.Lerp(800, 6900, (currentSpeed / maxSpeed)));

        carTexts[0].text = Mathf.Round(engineRPM).ToString() + "RPM";
        carTexts[1].text = currentSpeed.ToString() + "Km/h";
        
        if (currentSpeed < maxSpeed)
        {
            engineTorque = ((150 * 5252) / engineRPM); // engineTorque = Engine HP * 5252 / RPM THAT WILL HAVE THE MOST TORQUE
        }

        else
        {
            engineTorque = 0;
        }

    }

    void Steering()
    {
        wheelPhysics[0].steerAngle = steeringMaxAngle * horizontal;
        wheelPhysics[1].steerAngle = steeringMaxAngle * horizontal;
    }

    void WheelMeshUpdate()
    {
        for (int i = 0; i < 4; i++)
        {
            Quaternion quat;
            Vector3 pos;
            wheelPhysics[i].GetWorldPose(out pos, out quat);

            wheelBody[i].position = pos;
            wheelBody[i].rotation = quat;
        }
    }

    void GearShift()
    {
        if(!automatic)
        {
            if (Input.GetButtonDown("ShiftDown") && gearPos > -1)
            {
                gearPos--;
            }

            if (Input.GetButtonDown("ShiftUp") && gearPos > -1)
            {
                gearPos++;
            }
        }

        else
        {
            //GEAR MUST BE DONE DO WORK PROPERLY
        }      
    }

    void Gears()
    {
        //https://itstillruns.com/calculate-gear-ratios-torque-8140164.html
        //Gear Calculator
        
        switch(gearPos)
        {
            case -1: //REVERSE
                 {
                    wheelPhysics[2].motorTorque = -engineTorque * 15 * accelerator;
                    wheelPhysics[3].motorTorque = -engineTorque * 15 * accelerator;
                    maxSpeed = 30;
                }
                 break;

            case 0: //NEUTRAL
                {
                    wheelPhysics[2].motorTorque = 0;
                    wheelPhysics[3].motorTorque = 0;
                }
                break;

            case 1: //FIRST
                {
                    // Engine axis +- 2.5cm radius > 0.025M
                    // Engine torque = 154,84 > 154,85
                    // Gear torque = 154,85 * 26 = 4026
                    // 20 is the gear ratio
                    
                    wheelPhysics[2].motorTorque = engineTorque * 20 * accelerator;
                    wheelPhysics[3].motorTorque = engineTorque * 20 * accelerator;
                    maxSpeed = 30;
                }
                break;
            case 2:
                //Gear ratio = 15
                wheelPhysics[2].motorTorque = engineTorque * 15 * accelerator;
                wheelPhysics[3].motorTorque = engineTorque * 15 * accelerator;
                maxSpeed = 60;
                break;
            case 3:
                //Gear ratio = 10
                wheelPhysics[2].motorTorque = engineTorque * 10 * accelerator;
                wheelPhysics[3].motorTorque = engineTorque * 10 * accelerator;
                maxSpeed = 90;
                break;
            case 4:
                //Gear ratio = 5
                wheelPhysics[2].motorTorque = engineTorque * 5 * accelerator;
                wheelPhysics[3].motorTorque = engineTorque * 5 * accelerator;
                maxSpeed = 120;
                break;
            case 5:
                //Gear ratio = 2,5
                wheelPhysics[2].motorTorque = engineTorque * 2.5f * accelerator;
                wheelPhysics[3].motorTorque = engineTorque * 2.5f * accelerator;
                maxSpeed = 150;
                break;
            case 6:
                //Gear ratio = 1
                wheelPhysics[2].motorTorque = engineTorque * 1 * accelerator;
                wheelPhysics[3].motorTorque = engineTorque * 1 * accelerator;
                maxSpeed = 180;
                break;
        }
    }

    void WheelSpeeds()
    {
        //https://sciencing.com/calculate-wheel-speed-7448165.html

        //I'm going to do this for every wheel because of the ABS System

        //Speed based on the wheel RPM
        //First > Circuimference of the wheel > WheelRadius * 2 * 3,14 (Pi)
        //Second > Speed (meter/minute) = RPM * WheelRadius
        //Whit this I can see each wheel RPM for the abs system work and the car speed I will make an average of each wheel speed

        wheelSpeed[0] = wheelPhysics[0].rpm;
        wheelSpeed[1] = wheelPhysics[1].rpm;
        wheelSpeed[2] = wheelPhysics[2].rpm;
        wheelSpeed[3] = wheelPhysics[3].rpm;

        currentSpeed = rb.velocity.magnitude * 3.6f;
        currentSpeed = Mathf.Round(currentSpeed);
    }

    void BrakeForce()
    {
        brakeTorque = brakeMaxTorque;

        if(!ABS)
        {
            if (vertical < 0)
            {
                wheelPhysics[0].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                wheelPhysics[1].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                wheelPhysics[2].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                wheelPhysics[3].brakeTorque = Mathf.Abs(brakeTorque * vertical);
            }

            else
            {
                wheelPhysics[0].brakeTorque = 0;
                wheelPhysics[1].brakeTorque = 0;
                wheelPhysics[2].brakeTorque = 0;
                wheelPhysics[3].brakeTorque = 0;
            }
        }

        else
        {
            if(vertical< 0 && currentSpeed > 0)
            {
                //FRONT LEFT
                if (wheelSpeed[0] != 0)
                {
                    wheelPhysics[0].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                }

                else
                {
                    wheelPhysics[0].brakeTorque = 0;
                }

                //FRONT RIGHT
                if (wheelSpeed[1] != 0)
                {
                    wheelPhysics[1].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                }

                else
                {
                    wheelPhysics[1].brakeTorque = 0;
                }

                //REAR LEFT
                if (wheelSpeed[2] != 0)
                {
                    wheelPhysics[2].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                }

                else
                {
                    wheelPhysics[2].brakeTorque = 0;
                }

                //REAR RIGHT
                if (wheelSpeed[3] != 0)
                {
                    wheelPhysics[3].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                }

                else
                {
                    wheelPhysics[3].brakeTorque = 0;
                }
            }

            else if(vertical < 0 && currentSpeed == 0)
            {
                wheelPhysics[0].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                wheelPhysics[1].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                wheelPhysics[2].brakeTorque = Mathf.Abs(brakeTorque * vertical);
                wheelPhysics[3].brakeTorque = Mathf.Abs(brakeTorque * vertical);
            }

            else
            {
                wheelPhysics[0].brakeTorque = 0;
                wheelPhysics[1].brakeTorque = 0;
                wheelPhysics[2].brakeTorque = 0;
                wheelPhysics[3].brakeTorque = 0;
            }

            
        }

        
    }
}