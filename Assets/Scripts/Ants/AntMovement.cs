using UnityEngine;
using System.Collections;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Extensions;
using Assets.Scripts.Ticking;
using Assets.Scripts.Ants;

public class AntMovement : MonoBehaviour, ITickable
{
    public AntManager ant;
    public SimulationManager simulation;
    CharacterController cont;

    float dir;                              //current direction
    float nextDirChange_dist;               //max distance to be moved before next direction change
    float nextDirChange_time;               //max time before next direction change
    Vector3 lastTurn;                       //position of last direction change 
    Transform pheromoneParent;              //this will be the parent of the pheromones in the gameobject heirachy
    float nextPheromoneCheck;

    //Parameters
    public GameObject pheromonePrefab;      //the pheromone prefab
    public float maxVar = 40f;              //max amount this ant can turn at one time
    public float maxDirChange_time = 3f;//5f;    //maximum time between direction changes

    public float scoutSpeed;                //speed of an active ant while not tandem running or carrying
    public float tandemSpeed;               //speed of an ant while tandem running
    public float carrySpeed;                //speed of an ant while carrying another ant
    public float inactiveSpeed;             //speed of an ant in the inactive state
    public float assessingSpeedFirstVisit;  //speed of an ant in the assessing state (first visit)
    public float assessingSpeedSecondVisit; //speed of an ant in the assessing state (second visit)

    public float doorSenseRange = 50;//5f;       //distance from which an ant can sense the presence of a door

    public bool usePheromones;              //this dictates wether or not the ant uses pheromones
    public float pheromoneRange = 10f;      //maximum distance that pheromones can be sensed from

    // tandem run variables
    public float averageAntennaReach = 2.0f;        // antenna reach value (1mm) => radius of ant is 0.5mm therefore to get 1mm gap between ants need to include 2*0.5mm 
    public float leaderStopDistance = 2.0f;         // leader stops when gap between leader and follower is 2mm. => must be 3.0f as radius of ant is 0.5mm
    public float pheromoneFrequencyFTR = 0.2664f;   // 1.8mm/s => 12.5f ! FTR 3 lay per 10mm => 12.5mm/s (speed) lay pheromone every 0.2664 secs 
    public float pheromoneFrequencyRTR = 0.8f;      // 1.8mm/s => 12.5f ! FTR 1 lay per 10mm => 12.5mm/s (speed) lay pheromone every 0.8 secs


    // buffon needle
    // Eamonn B. Mallon and Nigel R. Franks - Ants estimate area using Buffon’s needle
    // The speeds at intersections were noted when an ant was within one antenna’s length of its first visit path.
    // one antenna lenght = 1mm. As pheromone range a radius around ant assessmentPheromoneRange = 0.5
    public float assessmentPheromoneRange = 0.125f;
    public Vector3 lastPosition;
    public float assessingDistance = 0f;
    public float intersectionNumber = 0f;
    public float pheromoneFrequencyBuffon = 0.0376f;        //the frequency that pheromones are laid for buffon needle

    private int frameNumber = 0;
    private int assessorChangeDirectionPerFrame = 3;

    //greg edit
    public static float gasterHeadDistance = 0f;
    public static float gasterHeadDistanceCount = 0f;
    public float pheromoneFrequencyScouting = 0.125f;

    public bool ShouldBeRemoved { get { return false; } }

    private bool _skipMove;

    private float obstructionCheckRaycastLength = 1;

    // Use this for initialization
    void Start()
    {
        pheromonePrefab = Resources.Load("Pheromone") as GameObject;
        ant = (AntManager)transform.GetComponent(Naming.Ants.Controller);
        cont = (CharacterController)transform.GetComponent("CharacterController");
        lastTurn = transform.position;
        dir = RandomGenerator.Instance.Range(0, 360);
        nextDirChange_time = simulation.TickManager.TotalElapsedSimulatedSeconds + maxDirChange_time;

        pheromoneParent = GameObject.Find(Naming.ObjectGroups.Pheromones).transform;
        nextPheromoneCheck = simulation.TickManager.TotalElapsedSimulatedSeconds;
        //passive ants laying in centre of nests makes ants gravitate towards nest centers to much

        // set the speeds
        scoutSpeed = 50f;                          // 7.2mm/s
        tandemSpeed = 50f;                         // 1.8mm/s (slower due to stop and start)
        carrySpeed = 37.5f;                        // 5.4mm/s
        inactiveSpeed = 5f;
        assessingSpeedFirstVisit = 26.595f;        // 5.107mm/s
        assessingSpeedSecondVisit = 32.175f;       // 6.179mm/

        //greg edit
        gasterHeadDistance = 0f;
        gasterHeadDistanceCount = 0f;

        // all active ants call the LayPheromone function reapeatedly (but only lay is usePheromones true)
        usePheromones = false; // all pheromones are false (turned off if FTR or RTR leader)
                               //greg edit
        if (ant.state == BehaviourState.Scouting)
        {
            usePheromones = false;
        }

        //		if (!this.ant.passive) {
        //			InvokeRepeating ("LayPheromoneScouting", 0, pheromoneFrequencyScouting);
        //InvokeRepeating ("LayPheromoneAssessing", 0, pheromoneFrequencyBuffon);
        //		}
    }

    private float _elapsedFTR = 0, _elapsedRTR = 0;
    public void Tick(float elapsedSimulatedMS)
    {
        // Mimic the InvokeRepeating for the tandem pheromones
        if (Ticker.Should(elapsedSimulatedMS, ref _elapsedFTR, pheromoneFrequencyFTR))
        {
            LayPheromoneFTR();
        }
        if (Ticker.Should(elapsedSimulatedMS, ref _elapsedRTR, pheromoneFrequencyRTR))
        {
            LayPheromoneRTR();
        }


        //if disabled then don't do anything
        if (!IsEnabled())
        {
            return;
        }

        // ensures that an assessor ant always keeps within the nest cavity 
        // if the assessor randomly leaves the nest it will turn back towards the nest centre 
        //This statements makes assessors in the nest change direction more frequently than those outside the nest.


        if (ant.state == BehaviourState.Assessing && !ant.inNest && ant.assessmentStage == NestAssessmentStage.Assessing)
        {
            ChangeDirection();
        }
        else if (ant.state == BehaviourState.Assessing && ant.inNest && ant.assessmentStage == NestAssessmentStage.Assessing)
        {
            frameNumber++;
            if ((frameNumber) % assessorChangeDirectionPerFrame == 0)
            {
                ChangeDirection();
            }
        }

        // if tandem follower is waiting return and do not update movement
        if (ant.leader != null)
        {
            tandemSpeed = 45f;
            if (Vector3.Distance(transform.position, ant.estimateNewLeaderPos) > 9.5f)
            {
                ChangeDirection();
            }
            if (HasFollowerTouchedLeader())
            {
                return;
            }
        }

        // if tandem leader is waiting return and do not update movement
        if (ant.follower != null)
        {
            tandemSpeed = 50f;
            if (ShouldTandemLeaderWait())
            {
                return;
            }
        }

        //move ant forwards
        ProcessMovement(elapsedSimulatedMS);

        //TODO: try pheromone and doorcheck in here
        if (!ant.inNest)
        {
            if (ant.state == BehaviourState.Scouting || ant.state == BehaviourState.Inactive)
            {
                GameObject door = DoorCheck();
                if (door != null)
                    FaceObject(door);
                else
                    Turn(dir);
            }
        }

        //wait for specified time until direction change
        if (simulation.TickManager.TotalElapsedSimulatedSeconds < nextDirChange_time)
            return;

        //change direction calculate when next direction change occurs 
        ChangeDirection();
    }

    //
    private void CheckForIntersection()
    {
        //get pheromones in assessmenPheromoneRange (mm) range (on top of)
        ArrayList pheromones = AssessmentPheromonesInRange();
        if (pheromones.Count != 0)
        {
            Pheromone p, pp;
            for (int i = 0; i < pheromones.Count; i++)
            {
                p = (Pheromone)pheromones[i];
                if (p.owner == this && p.assessingPheromoneCounted == false)
                {
                    // intersection speed
                    // non-intersection 4.06 mm/s, intersection 2.72mm/s => intersection speed reduced to 2.72mm/s
                    assessingSpeedSecondVisit = 16.05f;
                    intersectionNumber += 1.0f;
                    for (int j = 0; j < pheromones.Count; j++)
                    {
                        pp = (Pheromone)pheromones[j];
                        if (pp.owner == this)
                        {
                            pp.assessingPheromoneCounted = true;
                        }
                    }
                    return;
                }
                else
                {
                    assessingSpeedSecondVisit = 32.175f;
                }
            }
        }
    }


    // checks what movement the tandem follower should take
    private bool HasFollowerTouchedLeader()
    {
        // if follower is waiting for leader to move -> return true (follower waits)
        if (ant.followerWait == true)
        {
            // if follower has lost tactile contact with leader -> begin to move (wait == false) 
            if (Vector3.Distance(transform.position, ant.leader.transform.position) > (averageAntennaReach))
            {
                ant.followerWait = false;
            }
            return true;
        }
        // if follower is searching for their leader check if LGUT has expired
        if (ant.HasLGUTExpired())
        {
            // fail tandem run if LGUT has expired
            ant.FailedTandemRun();
            return false;
        }
        // follower has made contact with leader -> reset tandem variables
        if (Vector3.Distance(transform.position, ant.leader.transform.position) < averageAntennaReach &&
            ant.LineOfSight(ant.leader.gameObject))
        {
            TandemRegainedContact();
            return true;
        }
        else
        {
            return false;
        }
    }

    // tandem follower has found their leader
    private void TandemRegainedContact()
    {
        // follower waits for leader to move 
        ant.followerWait = true;
        // leader stop waiting and continues
        ant.leader.leaderWaits = false;
        // re-set LGUT and duration of lost contact variables (for both leader and follower)
        ant.TandemContactRegained();
        ant.leader.TandemContactRegained();
        // estimate where the leader will move to while the follower waits
        EstimateNextLocationOfLeader();

        ant.leader.leaderPositionContact = ant.leader.transform.position;
    }

    // calculates the position of where the follower expects next to find the leader
    private void EstimateNextLocationOfLeader()
    {
        Vector3 leaderPos = ant.leader.transform.position;
        float angleToLeader = GetAngleBetweenPositions(transform.position, leaderPos);
        Vector3 directionToLeader = new Vector3(0, Mathf.Sin(angleToLeader), Mathf.Cos(angleToLeader));
        ant.estimateNewLeaderPos = leaderPos + (directionToLeader.normalized * leaderStopDistance);
    }

    // checks what movement the tandem leader should take
    private bool ShouldTandemLeaderWait()
    {
        // if leader is waiting for follower ensure follower is allowed to move
        if (ant.leaderWaits)
        {
            ant.follower.followerWait = false;
            return true;
        }

        // if leader is > 2mm away from follower she stops and waits
        // Richardson & Franks, Teaching in Tandem Running
        if (Vector3.Distance(ant.follower.transform.position, transform.position) < (2 * leaderStopDistance))
        {
            return false;
        }
        else
        {
            TandemLostContact();
            DistanceBetweenLeaderAndFollower();
            return true;
        }
    }

    private void DistanceBetweenLeaderAndFollower()
    {
        gasterHeadDistance += Vector3.Distance(ant.follower.transform.position, transform.position);
        gasterHeadDistanceCount += 1f;
    }

    // tandem leader has lost tactile contact with the tandem follower 
    private void TandemLostContact()
    {
        // leader waits for follower
        ant.leaderWaits = true;

        // set the tandem lost contact variables (LGUT, time of lost contact)
        ant.TandemContactLost();
        ant.follower.TandemContactLost();
    }


    //turns ant directly to object
    private void FaceObject(GameObject g)
    {
        float a = GetAngleBetweenPositions(transform.position, g.transform.position);
        Turn(a);
    }

    //move ant forwards
    public void ProcessMovement(float elapsed)
    {
        //check for obstructions, turn to avoid if there are
        ObstructionCheck();
        
        // We have just performed an obstruction check but we might have rotated into another obstacle
        // Don't keep rotating, just stop the ant from moving this step
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, obstructionCheckRaycastLength))
        {
            return;
        }

        //move ant at appropriate speed
        if (ant.state == BehaviourState.Inactive)
        {
            MoveAtSpeed(inactiveSpeed, elapsed);
        }
        else if (ant.state == BehaviourState.Reversing)
        {
            MoveAtSpeed(tandemSpeed, elapsed);
        }
        else if (ant.IsTransporting())
        {
            MoveAtSpeed(carrySpeed, elapsed);
        }
        else if (ant.IsTandemRunning())
        {
            MoveAtSpeed(tandemSpeed, elapsed);
        }
        else if (ant.state == BehaviourState.Assessing)
        {

            if (ant.nestAssessmentVisitNumber == 1)
            {
                MoveAtSpeed(assessingSpeedFirstVisit, elapsed);
            }
            else
            {
                // if this.ant.nestAssessmentVisitNumber == 2 -> second visit
                MoveAtSpeed(assessingSpeedSecondVisit, elapsed);
            }

        }
        else
        {
            MoveAtSpeed(scoutSpeed, elapsed);
        }
    }

    private void MoveAtSpeed(float speed, float elapsed)
    {
        // previously this function does it once per update
        // we are now adding a simlated delta (elapsed)
        // correct the original speeds to be in the domain of 1x speed

        speed /= 1000f / 30f;
        speed /= 40;

        //var newPosition = transform.position + ( transform.forward * speed * elapsed);
        //transform.position = new Vector3(Mathf.Round(newPosition.x), Mathf.Round(newPosition.y), Mathf.Round(newPosition.z));
        //transform.position += (transform.forward * speed * elapsed);
        cont.transform.position += (transform.forward * speed * elapsed);
    }

    //change direction based on state
    public void ChangeDirection()
    {
        // the maxVar is 40f unless the ant is following a tandem run where it is 20f 
        // Franks et al. Ant Search Strategy After Interrupted Tandem Runs
        if (ant.state == BehaviourState.Scouting)
        {
            maxVar = 40f;
            ScoutingDirectionChange();
        }
        else if (ant.state == BehaviourState.Following)
        {
            //maxVar = 20f;
            maxVar = 10f;
            FollowingDirectionChange();
        }
        else if (ant.state == BehaviourState.Inactive)
        {
            maxVar = 40f;
            InactiveDirectionChange();
        }
        else if (ant.state == BehaviourState.Recruiting)
        {
            maxVar = 40f;
            RecruitingDirectionChange();
        }
        else if (ant.state == BehaviourState.Reversing)
        {
            maxVar = 40f;
            ReversingDirectionChange();
        }
        else
        {
            //this.maxVar = 20f;
            AssessingDirectionChange();
        }
        Turned();
    }

    private void ScoutingDirectionChange()
    {
        GameObject door = DoorCheck();
        if (door == null)
            RandomWalk();
        else
            WalkToGameObject(door);
    }

    private void FollowingDirectionChange()
    {
        //if not following an ant then walk towards center of nest
        if (ant.leader == null)
        {
            transform.LookAt(ant.myNest.transform);
            // BUGFIX: if ant in the new nest and follower a RTR leader -> always LookAt(leader)
            //		} else if (this.ant.inNest && this.ant.leader.state == AntManager.State.Reversing) {
            //			transform.LookAt(this.ant.leader.transform);
            // if follower can't see the leader -> walk towards where the follower predicts the leader is
        }
        else if (!ant.LineOfSight(ant.leader.gameObject))
        {
            // first time follower moves "estimateNextLocationOfLeader()" not called 
            // therefore move ant to leader on first move
            if (ant.estimateNewLeaderPos == Vector3.zero)
            {
                ant.estimateNewLeaderPos = ant.leader.transform.position;
            }

            var posToUse = Vector3.Lerp(ant.estimateNewLeaderPos, ant.leader.transform.position, (float)RandomGenerator.Instance.NextDouble());
            float predictedLeaderAngle = GetAngleBetweenPositions(transform.position, posToUse);
            float newDir = RandomGenerator.Instance.NormalRandom(predictedLeaderAngle, maxVar);
            Turn(newDir);

            //Debug.DrawLine(ant.transform.position, ant.estimateNewLeaderPos, Color.red, 1);
        }
        else
        {
            // if the follower can see the leader then turn towards them
            transform.LookAt(ant.leader.transform);
        }
    }

    //inactive ants swarn around center of nest
    private void InactiveDirectionChange()
    {
        WalkToGameObject(NextWaypoint());
    }

    //recruiters go backwards and forwards between the nests they are recruiting from and too (with randomness)
    private void RecruitingDirectionChange()
    {
        //if going back to old nest
        if (ant.newToOld && ant.OldNestOccupied())
            WalkToGameObject(NextWaypoint());
        //if no ants in old nest then walk randomly
        else if (ant.newToOld)
            RandomWalk();
        else
            WalkToGameObject(NextWaypoint());
    }

    private void ReversingDirectionChange()
    {
        if (ant.newToOld && ant.OldNestOccupied())
            WalkToGameObject(NextWaypoint());
        //if no ants in old nest then walk randomly
        else if (ant.newToOld)
            RandomWalk();
        else
            WalkToGameObject(NextWaypoint());
    }

    private void AssessingDirectionChange()
    {
        if (ant.assessmentStage == NestAssessmentStage.ReturningToHomeNestDoor)
        {
            WalkToGameObject(ant.oldNest.door);
            if (Vector3.Distance(transform.position, ant.oldNest.door.transform.position) < 10f)
            {
                ant.assessmentStage = NestAssessmentStage.ReturningToHomeNestMiddle;
            }
            return;
        }
        else if (ant.assessmentStage == NestAssessmentStage.ReturningToPotentialNestDoor)
        {
            WalkToGameObject(ant.nestToAssess.door);
            if (Vector3.Distance(transform.position, ant.nestToAssess.door.transform.position) < 10f)
            {
                ant.assessmentStage = NestAssessmentStage.ReturningToPotentialNestMiddle;
            }
            return;
        }
       else  if (ant.assessmentStage == NestAssessmentStage.ReturningToHomeNestMiddle)
        {
            WalkToGameObject(ant.oldNest.gameObject);
            if (Vector3.Distance(transform.position, ant.oldNest.transform.position) < 20f)
            {
                ant.assessmentStage = NestAssessmentStage.ReturningToPotentialNestDoor;
                ant.SetPrimaryColour(AntColours.NestAssessment.ReturningToPotentialNest);
            }
            return;
        }
        else if (ant.assessmentStage == NestAssessmentStage.ReturningToPotentialNestMiddle)
        {
            WalkToGameObject(ant.nestToAssess.gameObject);
            if (Vector3.Distance(transform.position, ant.nestToAssess.transform.position) < 40f)
            {
                if (ant.inNest)
                {
                    ant.NestAssessmentSecondVisit();
                }
            }
            return;
        }

        if (ant.nestAssessmentVisitNumber == 1)
        {
            assessingDistance += Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
        }
        else if (ant.nestAssessmentVisitNumber == 2)
        {
            assessingDistance += Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
        }

        if (ant.assessTime > 0)
        {
            if (ant.inNest)
            {
                RandomWalk();
            }
            else
            {
                WalkToGameObject(ant.nestToAssess.gameObject);
            }
        }
        else
        {
            WalkToGameObject(NextWaypoint());
        }
    }
    //

    //if running along wall this checks that new direction doesn't push ant into it
    float NewDirectionCheck(float newDir)
    {
        RaycastHit hit;
        bool f, r, l, b;
        f = r = l = b = false;

        //0 <= newDir <= 360
        newDir %= 360;
        if (newDir < 0)
            newDir += 360;

        //checks forwards, backwards and both sides to see if there is a wall there
        if (Physics.Raycast(transform.position, Vector3.forward, out hit, 1))
            if (hit.collider.tag != Naming.Ants.Tag)
                f = true;
        if (Physics.Raycast(transform.position, -Vector3.forward, out hit, 1))
            if (hit.collider.tag != Naming.Ants.Tag)
                b = true;
        if (Physics.Raycast(transform.position, Vector3.right, out hit, 1))
            if (hit.collider.tag != Naming.Ants.Tag)
                r = true;
        if (Physics.Raycast(transform.position, -Vector3.right, out hit, 1))
            if (hit.collider.tag != Naming.Ants.Tag)
                l = true;

        //this that new direction doesn't make ant try to walk through a wall and adjusts if neccessary 
        if (r && newDir < 180)
            return dir;
        else if (l && newDir > 180)
            return dir;
        else if (f && (newDir > 270 || newDir < 90))
            return dir;
        else if (b && (newDir < 270 && newDir > 90))
            return dir;
        else
            return newDir;
    }

    //tells ants where to direct themselves towards given where they currently are and what they are doing
    private GameObject NextWaypoint()
    {
        //determine if nearer old or new nest
        bool nearerOld = true;
        if (ant.oldNest == null || Vector3.Distance(transform.position, ant.myNest.transform.position) < Vector3.Distance(transform.position, ant.oldNest.transform.position))
            nearerOld = false;

        //if this is a passive ant then always direct them towards center of their nest (because they are either carried or lead between)
        if (ant.passive || ant.state == BehaviourState.Inactive)
            return ant.myNest.gameObject;

        if (ant.state == BehaviourState.Assessing)
        {
            if (ant.assessTime > 0)
            {
                return ant.nestToAssess.gameObject;
            }
            else
            {
                return ant.nestToAssess.door;
            }
        }

        //if reversing
        if (ant.state == BehaviourState.Reversing)
        {
            //If in new nest
            if (ant.inNest && !nearerOld)
            {
                //If not carrying
                if (!ant.IsTandemRunning())
                {
                    //Find an ant to carry
                    return ant.myNest.gameObject;
                }
                else
                {
                    //Head for exit
                    NestManager my = ant.myNest;
                    if (my.door != null)
                        return my.door;
                    else
                        return ant.myNest.gameObject;
                }
            }
            else
            {
                //Go to old nest
                if (!ant.inNest)
                {
                    NestManager old = ant.oldNest;
                    if (old.door != null)
                        return old.door;
                    else
                        return ant.oldNest.gameObject;
                }
                else
                {
                    return ant.oldNest.gameObject;
                }
            }
        }

        //if this ant is in a nest and is going towards their chosen nest
        if (ant.inNest && !ant.newToOld)
        {
            //if in the nest they are recruiting FROM but want to leave then return the position of the nest's door (if this has been marked)
            if (nearerOld)
            {
                NestManager old = ant.oldNest;
                if (old.door != null)
                    return old.door;
                else
                    return ant.myNest.gameObject;
            }
            //if in the nest that they are recruiting TO and don't want to leave returns the position of it's center 
            else
            {
                return ant.myNest.gameObject;
            }
        }
        //if in nest and going to towards nest that they are recruiting FROM
        else if (ant.inNest)
        {
            //in nest that they recruit TO but trying to leave then return the position of the nest's door (if this has been marked)
            if (!nearerOld)
            {
                NestManager my = ant.myNest;
                if (my.door != null)
                    return my.door;
                else
                    return ant.oldNest.gameObject;
            }
            //if in nest that recruiting FROM and are looking for ant to recruit then head towards center
            else
            {
                return ant.oldNest.gameObject;
            }
        }
        //if not in a nest and heading to nest that they recruit TO then return position of door to that nest (if possible)
        else if (!ant.newToOld)
        {
            NestManager my = ant.myNest;
            if (my.door != null)
                return my.door;
            else
                return ant.myNest.gameObject;
        }
        //if not in a nest and heading towards nest that they recruit FROM then return position of that nest's door (if possible)
        else
        {
            NestManager old = ant.oldNest;
            if (old.door != null)
                return old.door;
            else
                return ant.oldNest.gameObject;
        }
    }

    private GameObject DoorCheck()
    {
        foreach (GameObject door in simulation.doors)
        {
            if (Vector3.Distance(door.transform.position, transform.position) < doorSenseRange)
            {
                if (transform.InverseTransformPoint(door.transform.position).z >= 0)
                {
                    if (!ant.inNest)
                        return door.transform.parent.gameObject;
                    else
                        return door;
                }
            }
        }
        return null;
    }

    //resets next turn time and distance counters
    private void Turned()
    {
        //if stuck then wait less time till next change as it may take a few random rotations to get unstuck
        if (Vector3.Distance(transform.position, lastTurn) > 0)
        {
            // Assessing applies to a wide range of behaviours - doubleing the time taken here just confuses things
            //if (ant.state == BehaviourState.Assessing)
            //    nextDirChange_time = simulation.TickManager.TotalElapsedSimulatedSeconds + RandomGenerator.Instance.Range(0, 1f) * maxDirChange_time * 2f;
            //else
            nextDirChange_time = simulation.TickManager.TotalElapsedSimulatedSeconds + RandomGenerator.Instance.Range(0, 1f) * maxDirChange_time;
        }
        else
            nextDirChange_time = simulation.TickManager.TotalElapsedSimulatedSeconds + (RandomGenerator.Instance.Range(0, 1f) * maxDirChange_time) / 10f;
        lastTurn = transform.position;
    }

    public void Enable()
    {
        cont.enabled = true;
    }

    public void Disable()
    {
        cont.enabled = false;
    }

    public bool IsEnabled()
    {
        return cont.enabled;
    }

    /*this finds mid point (angle wise) between current direction and direction of given object
	then picks direction that is that mid point +/- an angle <= this.maxVar*/
    public void WalkToGameObject(GameObject target)
    {
        float goalAngle;
        float currentAngle = transform.eulerAngles.y;

        //find angle mod 360 of current position to goal
        goalAngle = GetAngleBetweenPositions(transform.position, target.transform.position);

        if (Mathf.Abs(goalAngle - currentAngle) > 180)
            currentAngle -= 360;

        float newDir = RandomGenerator.Instance.NormalRandom(goalAngle, maxVar);
        Turn(newDir);
    }

    private void RandomWalk()
    {
        float maxVar = this.maxVar;
        if (ant.state == BehaviourState.Assessing)
        {
            maxVar = 10f;
        }
        if (Vector3.Distance(transform.position, lastTurn) == 0)
            maxVar = 180;
        float theta = RandomGenerator.Instance.NormalRandom(0, maxVar);
        float newDir = (dir + theta) % 360;
        Turn(newDir);
    }

    //turn ant to this face this direction (around y axis)
    private void Turn(float newDir)
    {
        dir = NewDirectionCheck(PheromoneDirection(newDir));
        transform.rotation = Quaternion.Euler(0, dir, 0);
    }

    //gets angle from v1 to v2
    private float GetAngleBetweenPositions(Vector3 v1, Vector3 v2)
    {
        Vector3 dif = v2 - v1;
        if (v2.x < v1.x)
            return 360 - Vector3.Angle(Vector3.forward, dif);
        else
            return Vector3.Angle(Vector3.forward, dif);
    }

    //this returns direction when pheromones are taken into account (using antbox algorithm)
    private float PheromoneDirection(float direction)
    {
        if (direction < 0)
            direction += 360;

        if (simulation.TickManager.TotalElapsedSimulatedSeconds < nextPheromoneCheck)
            return direction;

        //if not using pheromones then just use given direction
        // only followers that are not in a nest uses phereomones
        if (ant.inNest || (ant.state != BehaviourState.Following && ant.state != BehaviourState.Scouting))
            return direction;

        //get pheromones in range
        ArrayList pheromones = PheromonesInRange();

        //if none then just use direction
        if (pheromones.Count == 0)
            return direction;

        float strength, p_a;
        Vector3 v = transform.forward;
        //Vector3 v = Vector3.zero;
        Vector3 d;
        Pheromone p;
        Transform p_t;

        //total up weighted direction and strength of pheromones
        for (int i = 0; i < pheromones.Count; i++)
        {
            p = (Pheromone)pheromones[i];
            p_t = p.transform;
            p_a = GetAngleBetweenPositions(transform.position, p_t.position);

            //get strength of this pheromone (using equation from antbox)
            strength = p.strength * Mathf.Exp(-0.5f * (Square(2 * Mathf.Abs(p_a - direction) / maxVar)));

            //add this to the total vector
            d = p_t.position - transform.position;
            d.Normalize();
            v += d * strength;
        }

        //this stops long snake-like chains of ants following the same path over and over again
        if (RandomGenerator.Instance.Range(0f, 1f) < 0.02f)
        {
            nextPheromoneCheck = simulation.TickManager.TotalElapsedSimulatedSeconds + RandomGenerator.Instance.Range(0, 1) * maxDirChange_time;
            return RandomGenerator.Instance.NormalRandom(dir, maxVar);
        }

        //get angle and add noise
        return GetAngleBetweenPositions(transform.position, transform.position + v) + Mathf.Exp(-0.5f * (Square(2 * RandomGenerator.Instance.Range(-180, 180) / maxVar)));

    }

    private float Square(float x)
    {
        return x * x;
    }

    //randomly turns ant
    private void RandomRotate()
    {
        float maxVar = this.maxVar;
        if (Vector3.Distance(transform.position, lastTurn) == 0)
            maxVar = 180;
        float newDir = RandomGenerator.Instance.NormalRandom(0, maxVar);
        Turn(newDir);
    }

    //helps and to avoid obstructions and returns true if action was taken and false otherwise
    private bool ObstructionCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, obstructionCheckRaycastLength))
        {
            //if there is an ant directly in front of this ant then randomly turn otherwise must be a wall so follow it
            if (hit.collider.transform.tag == Naming.Ants.Tag)
            {
                // follower ant wants to have tactile contact with leader ant
                if (ant.state != BehaviourState.Following)
                {
                    RandomRotate();
                    Turned();
                }
            }
            else
                FollowWall();
            // Ants were following walls too much. I've made it only call Turned() if they collided with another ant
            // This will make them turn direction even if they have recently hit a wall
            //Turned();
            return true;
        }
        return false;
    }

    //this will only work well on right angle corners, randomness included so ant may not always follow round corner but might turn around
    private void FollowWall()
    {
        //find it if there are obstructions infront, behind and to either side of ant
        bool[] rays = new bool[4];
        rays[0] = Physics.Raycast(transform.position, Vector3.forward, obstructionCheckRaycastLength);
        rays[1] = Physics.Raycast(transform.position, Vector3.right, obstructionCheckRaycastLength);
        rays[2] = Physics.Raycast(transform.position, -Vector3.forward, obstructionCheckRaycastLength);
        rays[3] = Physics.Raycast(transform.position, -Vector3.right, obstructionCheckRaycastLength);
        float a = Mathf.Round(transform.rotation.eulerAngles.y);

        //get direction of ant (0 = forwards, 1 = right, 2 = backwards, 3 = left)
        int d = 0;
        if (a > 45 && a < 135)
            d = 1;
        else if (a > 135 && a < 225)
            d = 2;
        else if (a > 225 && a < 315)
            d = 3;

        //sets weights of directions relative current dirction (weights[0] = direction of travel, [1] = direction ± 90, [2] = direction + 180)
        float[] weights = new float[3];
        weights[0] = 1;
        weights[1] = 0.7f;
        weights[2] = 0.1f;

        //vals[i] = how much direction 'i' (same scale as 'd') contributes towards new direction
        float[] vals = new float[4];
        for (int i = 0; i < vals.Length; i++)
        {
            int dif = Mathf.Abs(i - d);
            if (dif > vals.Length / 2)
                dif = vals.Length - dif;

            //if obstruction detected in this direction then weight == 0 otherwise it equals it's respective weight
            if (rays[i])
                vals[i] = 0;
            else
                vals[i] = weights[dif];
        }

        //roulette wheel selection method for new direction (with directions with larger weight taking up more of the wheel)
        float sum = vals.Sum();
        float rand = RandomGenerator.Instance.Range(0, sum);
        int index = 0;
        float total = 0;
        for (int i = 0; i < 4; i++)
        {
            if (total + vals[i] > rand)
            {
                index = i;
                break;
            }
            else
                total += vals[i];
        }
        dir = 90f * index;
        transform.rotation = Quaternion.Euler(0, dir, 0);
    }


    private void LayPheromoneScouting()
    {
        GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
        pheromone.transform.parent = pheromoneParent;
        if (ant.state == BehaviourState.Scouting)
        {
            ((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayScouting(this);
        }
    }

    private void LayPheromoneFTR()
    {
        if (!(ant.state == BehaviourState.Leading || ant.state == BehaviourState.Recruiting) || usePheromones == false || ant.inNest)
        {
            return;
        }
        GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
        pheromone.transform.parent = pheromoneParent;
        if (ant.state == BehaviourState.Reversing || ant.state == BehaviourState.Leading || ant.state == BehaviourState.Recruiting)
        {
            ((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayTandem(this);
        }
    }

    private void LayPheromoneRTR()
    {
        if (!(ant.state == BehaviourState.Reversing) || usePheromones == false || ant.inNest)
        {
            return;
        }
        GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
        pheromone.transform.parent = pheromoneParent;
        if (ant.state == BehaviourState.Reversing || ant.state == BehaviourState.Leading || ant.state == BehaviourState.Recruiting)
        {
            ((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayTandem(this);
        }
    }

    private void LayPheromoneAssessing()
    {
        if (ant.state != BehaviourState.Assessing || usePheromones == false || !ant.inNest)
        {
            return;
        }
        if (ant.nestToAssess == ant.oldNest)
        {
            return;
        }
        GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
        pheromone.transform.parent = pheromoneParent;
        if (ant.state == BehaviourState.Assessing)
        {
            if (ant.nestToAssess != ant.oldNest)
            {
                ((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayAssessing(this);
            }
        }
    }

    private ArrayList PheromonesInRange()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, pheromoneRange);
        ArrayList pher = new ArrayList();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i].tag == Naming.Ants.Pheromone)
                pher.Add(cols[i].transform.GetComponent(Naming.Ants.Pheromone));
        }
        return pher;
    }

    private ArrayList AssessmentPheromonesInRange()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, assessmentPheromoneRange);
        ArrayList pher = new ArrayList();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i].tag == Naming.Ants.Pheromone)
                pher.Add(cols[i].transform.GetComponent(Naming.Ants.Pheromone));
        }
        return pher;
    }
}
