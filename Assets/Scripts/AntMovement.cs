using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;

public class AntMovement : MonoBehaviour
{
	public AntManager ant;
	SimulationManager simManager;
	CharacterController cont;
	RandomGenerator rg;
	float dir;                              //current direction
	float nextDirChange_dist;               //max distance to be moved before next direction change
	float nextDirChange_time;               //max time before next direction change
	Vector3 lastTurn;                       //position of last direction change 
	Transform pheromoneParent;              //this will be the parent of the pheromones in the gameobject heirachy
	float nextPheromoneCheck;

	//Parameters
	public GameObject pheromonePrefab;      //the pheromone prefab
	public float maxVar = 40f;              //max amount this ant can turn at one time
	public float maxDirChange_time = 5f;    //maximum time between direction changes

	public float scoutSpeed;                //speed of an active ant while not tandem running or carrying
	public float tandemSpeed;               //speed of an ant while tandem running
	public float carrySpeed;                //speed of an ant while carrying another ant
	public float inactiveSpeed;             //speed of an ant in the inactive state
	public float assessingSpeedFirstVisit;  //speed of an ant in the assessing state (first visit)
	public float assessingSpeedSecondVisit; //speed of an ant in the assessing state (second visit)

	public float doorSenseRange = 5f;       //distance from which an ant can sense the presence of a door

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



	// Use this for initialization
	void Start()
	{
		this.simManager = (SimulationManager)GameObject.Find(Naming.World.InitialNest).GetComponent(Naming.Simulation.Manager);

		this.ant = (AntManager)transform.GetComponent(Naming.Ants.Controller);
		this.cont = (CharacterController)transform.GetComponent("CharacterController");
		this.lastTurn = transform.position;
		this.dir = Random.Range(0, 360);
		this.nextDirChange_time = Time.timeSinceLevelLoad + maxDirChange_time;
		this.rg = new RandomGenerator();
		this.pheromoneParent = GameObject.Find(Naming.ObjectGroups.Pheromones).transform;
		this.nextPheromoneCheck = Time.timeSinceLevelLoad;
		//passive ants laying in centre of nests makes ants gravitate towards nest centers to much

		// set the speeds
		this.scoutSpeed = 50f;                          // 7.2mm/s
		this.tandemSpeed = 50f;                         // 1.8mm/s (slower due to stop and start)
		this.carrySpeed = 37.5f;                        // 5.4mm/s
		this.inactiveSpeed = 5f;
		this.assessingSpeedFirstVisit = 26.595f;        // 5.107mm/s
		this.assessingSpeedSecondVisit = 32.175f;       // 6.179mm/

		//greg edit
		gasterHeadDistance = 0f;
		gasterHeadDistanceCount = 0f;
		
		//BatchRunner batchObj = (BatchRunner)arena.GetComponent("BatchRunner");
		//if (batchObj != null)
		//{
		//    //			this.leaderStopDistance = batchObj.antennaReach;
		//    //			this.pheromoneFrequencyBuffon = batchObj.buffonFrequency;

		//}

		// all active ants call the LayPheromone function reapeatedly (but only lay is usePheromones true)
		usePheromones = false; // all pheromones are false (turned off if FTR or RTR leader)
							   //greg edit
		if (this.ant.state == AntManager.State.Scouting)
		{
			usePheromones = false;
		}

		//		if (!this.ant.passive) {
		//			InvokeRepeating ("LayPheromoneScouting", 0, pheromoneFrequencyScouting);
		InvokeRepeating("LayPheromoneFTR", 0, pheromoneFrequencyFTR);
		InvokeRepeating("LayPheromoneRTR", 0, pheromoneFrequencyRTR);
		//InvokeRepeating ("LayPheromoneAssessing", 0, pheromoneFrequencyBuffon);
		//		}
	}

	//called every frame
	void Update()
	{
		//if disabled then don't do anything
		if (!isEnabled())
		{
			return;
		}

		// ensures that an assessor ant always keeps within the nest cavity 
		// if the assessor randomly leaves the nest it will turn back towards the nest centre 
		//This statements makes assessors in the nest change direction more frequently than those outside the nest.


		if (this.ant.state == AntManager.State.Assessing && !this.ant.inNest && this.ant.assessmentStage == 0)
		{
			ChangeDirection();
		}
		else if (this.ant.state == AntManager.State.Assessing && this.ant.inNest && this.ant.assessmentStage == 0)
		{
			this.frameNumber++;
			if ((this.frameNumber) % assessorChangeDirectionPerFrame == 0)
			{
				ChangeDirection();
			}
		}

		/*
		if (this.ant.state == AntManager.State.Assessing && this.ant.nestAssessmentVisitNumber == 2) {
			checkForIntersection();
		}		
		*/

		// if tandem follower is waiting return and do not update movement
		if (this.ant.leader != null)
		{
			this.tandemSpeed = 45f;
			if (Vector3.Distance(transform.position, this.ant.estimateNewLeaderPos) > 9.5f)
			{
				ChangeDirection();
			}
			if (hasFollowerTouchedLeader())
			{
				return;
			}
		}

		// if tandem leader is waiting return and do not update movement
		if (this.ant.follower != null)
		{
			this.tandemSpeed = 50f;
			if (shouldTandemLeaderWait())
			{
				return;
			}
		}

		//move ant forwards
		Move();

		//TODO: try pheromone and doorcheck in here
		if (!this.ant.inNest)
		{
			if (this.ant.state == AntManager.State.Scouting || this.ant.state == AntManager.State.Inactive)
			{
				GameObject door = DoorCheck();
				if (door != null)
					FaceObject(door);
				else
					Turn(this.dir);
			}
		}

		//wait for specified time until direction change
		if (Time.timeSinceLevelLoad < this.nextDirChange_time)
			return;

		//change direction calculate when next direction change occurs 
		ChangeDirection();
	}

	//
	private void checkForIntersection()
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
					this.assessingSpeedSecondVisit = 16.05f;
					this.intersectionNumber += 1.0f;
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
				else {
					this.assessingSpeedSecondVisit = 32.175f;
				}
			}
		}
	}


	// checks what movement the tandem follower should take
	private bool hasFollowerTouchedLeader()
	{
		// if follower is waiting for leader to move -> return true (follower waits)
		if (this.ant.followerWait == true)
		{
			// if follower has lost tactile contact with leader -> begin to move (wait == false) 
			if (Vector3.Distance(transform.position, this.ant.leader.transform.position) > (averageAntennaReach))
			{
				this.ant.followerWait = false;
			}
			return true;
		}
		// if follower is searching for their leader check if LGUT has expired
		if (this.ant.hasLGUTExpired())
		{
			// fail tandem run if LGUT has expired
			this.ant.failedTandemRun();
			return false;
		}
		// follower has made contact with leader -> reset tandem variables
		if (Vector3.Distance(transform.position, this.ant.leader.transform.position) < averageAntennaReach &&
			this.ant.LineOfSight(this.ant.leader.gameObject))
		{
			tandemRegainedContact();
			return true;
		}
		else {
			return false;
		}
	}

	// tandem follower has found their leader
	private void tandemRegainedContact()
	{
		// follower waits for leader to move 
		this.ant.followerWait = true;
		// leader stop waiting and continues
		this.ant.leader.leaderWaits = false;
		// re-set LGUT and duration of lost contact variables (for both leader and follower)
		this.ant.tandemContactRegained();
		this.ant.leader.tandemContactRegained();
		// estimate where the leader will move to while the follower waits
		estimateNextLocationOfLeader();

		this.ant.leader.leaderPositionContact = this.ant.leader.transform.position;
	}

	// calculates the position of where the follower expects next to find the leader
	private void estimateNextLocationOfLeader()
	{
		Vector3 leaderPos = this.ant.leader.transform.position;
		float angleToLeader = GetAngleBetweenPositions(transform.position, leaderPos);
		Vector3 directionToLeader = new Vector3(0, Mathf.Sin(angleToLeader), Mathf.Cos(angleToLeader));
		this.ant.estimateNewLeaderPos = leaderPos + (directionToLeader.normalized * this.leaderStopDistance);
	}

	// checks what movement the tandem leader should take
	private bool shouldTandemLeaderWait()
	{
		// if leader is waiting for follower ensure follower is allowed to move
		if (this.ant.leaderWaits == true)
		{
			this.ant.follower.followerWait = false;
			return true;
		}

		// if leader is > 2mm away from follower she stops and waits
		// Richardson & Franks, Teaching in Tandem Running
		if (Vector3.Distance(this.ant.follower.transform.position, transform.position) < (2 * this.leaderStopDistance))
		{
			return false;
		}
		else {
			tandemLostContact();
			distanceBetweenLeaderAndFollower();
			return true;
		}
	}

	private void distanceBetweenLeaderAndFollower()
	{
		gasterHeadDistance += Vector3.Distance(this.ant.follower.transform.position, transform.position);
		gasterHeadDistanceCount += 1f;
	}

	// tandem leader has lost tactile contact with the tandem follower 
	private void tandemLostContact()
	{
		// leader waits for follower
		this.ant.leaderWaits = true;

		// set the tandem lost contact variables (LGUT, time of lost contact)
		this.ant.tandemContactLost();
		this.ant.follower.tandemContactLost();
	}


	//turns ant directly to object
	private void FaceObject(GameObject g)
	{
		float a = GetAngleBetweenPositions(transform.position, g.transform.position);
		Turn(a);
	}

	//move ant forwards
	public void Move()
	{
		//check for obstructions, turn to avoid if there are
		ObstructionCheck();

		//move ant at appropriate speed
		if (this.ant.state == AntManager.State.Inactive)
		{
			this.cont.SimpleMove(this.inactiveSpeed * transform.forward);
		}
		else if (this.ant.state == AntManager.State.Reversing)
		{
			this.cont.SimpleMove(this.tandemSpeed * transform.forward);
		}
		else if (this.ant.isTransporting())
		{
			this.cont.SimpleMove(this.carrySpeed * transform.forward);
		}
		else if (this.ant.isTandemRunning())
		{
			this.cont.SimpleMove(this.tandemSpeed * transform.forward);
		}
		else if (this.ant.state == AntManager.State.Assessing)
		{

			if (this.ant.nestAssessmentVisitNumber == 1)
			{
				this.cont.SimpleMove(this.assessingSpeedFirstVisit * transform.forward);
			}
			else {
				// if this.ant.nestAssessmentVisitNumber == 2 -> second visit
				this.cont.SimpleMove(this.assessingSpeedSecondVisit * transform.forward);
			}

		}
		else {
			this.cont.SimpleMove(this.scoutSpeed * transform.forward);
		}
	}

	//change direction based on state
	public void ChangeDirection()
	{
		// the maxVar is 40f unless the ant is following a tandem run where it is 20f 
		// Franks et al. Ant Search Strategy After Interrupted Tandem Runs
		if (this.ant.state == AntManager.State.Scouting)
		{
			this.maxVar = 40f;
			ScoutingDirectionChange();
		}
		else if (this.ant.state == AntManager.State.Following)
		{
			this.maxVar = 20f;
			FollowingDirectionChange();
		}
		else if (this.ant.state == AntManager.State.Inactive)
		{
			this.maxVar = 40f;
			InactiveDirectionChange();
		}
		else if (this.ant.state == AntManager.State.Recruiting)
		{
			this.maxVar = 40f;
			RecruitingDirectionChange();
		}
		else if (this.ant.state == AntManager.State.Reversing)
		{
			this.maxVar = 40f;
			ReversingDirectionChange();
		}
		else {
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
		if (this.ant.leader == null)
		{
			transform.LookAt(this.ant.myNest.transform);
			// BUGFIX: if ant in the new nest and follower a RTR leader -> always LookAt(leader)
			//		} else if (this.ant.inNest && this.ant.leader.state == AntManager.State.Reversing) {
			//			transform.LookAt(this.ant.leader.transform);
			// if follower can't see the leader -> walk towards where the follower predicts the leader is
		}
		else if (!this.ant.LineOfSight(this.ant.leader.gameObject))
		{
			// first time follower moves "estimateNextLocationOfLeader()" not called 
			// therefore move ant to leader on first move
			Debug.Log("Got here");
			if (this.ant.estimateNewLeaderPos == new Vector3(0, 0, 0))
			{
				this.ant.estimateNewLeaderPos = this.ant.leader.transform.position;
			}
			float predictedLeaderAngle = GetAngleBetweenPositions(transform.position, this.ant.estimateNewLeaderPos);
			float newDir = normalRandom(predictedLeaderAngle, this.maxVar);
			Turn(newDir);
		}
		else {
			// if the follower can see the leader then turn towards them
			transform.LookAt(this.ant.leader.transform);
		}

		/* OLD SPACE MODLE (Martin) FollowingDirectionChange() function
		//if not following an ant then walk towards center of nest
		if(this.ant.leader == null)
			transform.LookAt(this.ant.myNest.transform);
		//if following and can't see leader then randomly walk towards them
		else if(!this.ant.LineOfSight(this.ant.leader.gameObject)) 
			WalkToGameObject(this.ant.leader.gameObject);
		//if following then walk at leader
		else
			transform.LookAt(this.ant.leader.transform);
		*/
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
		if (this.ant.newToOld && this.ant.OldNestOccupied())
			WalkToGameObject(NextWaypoint());
		//if no ants in old nest then walk randomly
		else if (this.ant.newToOld)
			RandomWalk();
		else
			WalkToGameObject(NextWaypoint());
	}

	private void ReversingDirectionChange()
	{
		if (this.ant.newToOld && this.ant.OldNestOccupied())
			WalkToGameObject(NextWaypoint());
		//if no ants in old nest then walk randomly
		else if (this.ant.newToOld)
			RandomWalk();
		else
			WalkToGameObject(NextWaypoint());
	}

	//greg edit
	//
	private void AssessingDirectionChange()
	{
		if (this.ant.assessmentStage == 1)
		{
			WalkToGameObject(this.ant.oldNest);
			if (Vector3.Distance(transform.position, this.ant.oldNest.transform.position) < 20f)
			{
				this.ant.assessmentStage = 2;
			}
			return;
		}
		else if (this.ant.assessmentStage == 2)
		{
			WalkToGameObject(this.ant.nestToAssess);
			if (Vector3.Distance(transform.position, this.ant.nestToAssess.transform.position) < 40f)
			{
				if (this.ant.inNest)
				{
					this.ant.nestAssessmentSeconfVisit();
				}
			}
			return;
		}

		if (this.ant.nestAssessmentVisitNumber == 1)
		{
			this.assessingDistance += Vector3.Distance(transform.position, this.lastPosition);
			this.lastPosition = transform.position;
		}
		else if (this.ant.nestAssessmentVisitNumber == 2)
		{
			this.assessingDistance += Vector3.Distance(transform.position, this.lastPosition);
			this.lastPosition = transform.position;
		}

		if (this.ant.assessTime > 0)
		{
			if (this.ant.inNest)
			{
				RandomWalk();
			}
			else {
				//WalkToGameObject(NextWaypoint());
				WalkToGameObject(this.ant.nestToAssess);
			}
		}
		else {
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
			return this.dir;
		else if (l && newDir > 180)
			return this.dir;
		else if (f && (newDir > 270 || newDir < 90))
			return this.dir;
		else if (b && (newDir < 270 && newDir > 90))
			return this.dir;
		else
			return newDir;
	}

	//tells ants where to direct themselves towards given where they currently are and what they are doing
	private GameObject NextWaypoint()
	{
		//determine if nearer old or new nest
		bool nearerOld = true;
		if (this.ant.oldNest == null || Vector3.Distance(transform.position, this.ant.myNest.transform.position) < Vector3.Distance(transform.position, this.ant.oldNest.transform.position))
			nearerOld = false;

		//if this is a passive ant then always direct them towards center of their nest (because they are either carried or lead between)
		if (this.ant.passive || this.ant.state == AntManager.State.Inactive)
			return this.ant.myNest;

		if (this.ant.state == AntManager.State.Assessing)
		{
			if (this.ant.assessTime > 0)
			{
				return this.ant.nestToAssess;
			}
			else
			{
				NestManager nest = (NestManager)this.ant.nestToAssess.GetComponent(Naming.World.Nest);
				return nest.door;
			}
		}

		//if reversing
		if (this.ant.state == AntManager.State.Reversing)
		{
			//If in new nest
			if (this.ant.inNest && !nearerOld)
			{
				//If not carrying
				if (!this.ant.isTandemRunning())
				{
					//Find an ant to carry
					return this.ant.myNest;
				}
				else
				{
					//Head for exit
					NestManager my = (NestManager)this.ant.myNest.GetComponent(Naming.World.Nest);
					if (my.door != null)
						return my.door;
					else
						return this.ant.myNest;
				}
			}
			else
			{
				//Go to old nest
				if (!this.ant.inNest)
				{
					NestManager old = (NestManager)this.ant.oldNest.GetComponent(Naming.World.Nest);
					if (old.door != null)
						return old.door;
					else
						return this.ant.oldNest;
				}
				else
				{
					return this.ant.oldNest;
				}
			}
		}

		//if this ant is in a nest and is going towards their chosen nest
		if (this.ant.inNest && !this.ant.newToOld)
		{
			//if in the nest they are recruiting FROM but want to leave then return the position of the nest's door (if this has been marked)
			if (nearerOld)
			{
				NestManager old = (NestManager)this.ant.oldNest.GetComponent(Naming.World.Nest);
				if (old.door != null)
					return old.door;
				else
					return this.ant.myNest;
			}
			//if in the nest that they are recruiting TO and don't want to leave returns the position of it's center 
			else
				return this.ant.myNest;
		}
		//if in nest and going to towards nest that they are recruiting FROM
		else if (this.ant.inNest)
		{
			//in nest that they recruit TO but trying to leave then return the position of the nest's door (if this has been marked)
			if (!nearerOld)
			{
				NestManager my = (NestManager)this.ant.myNest.GetComponent(Naming.World.Nest);
				if (my.door != null)
					return my.door;
				else
					return this.ant.oldNest;
			}
			//if in nest that recruiting FROM and are looking for ant to recruit then head towards center
			else
				return this.ant.oldNest;
		}
		//if not in a nest and heading to nest that they recruit TO then return position of door to that nest (if possible)
		else if (!this.ant.newToOld)
		{
			NestManager my = (NestManager)this.ant.myNest.GetComponent(Naming.World.Nest);
			if (my.door != null)
				return my.door;
			else
				return this.ant.myNest;
		}
		//if not in a nest and heading towards nest that they recruit FROM then return position of that nest's door (if possible)
		else
		{
			NestManager old = (NestManager)this.ant.oldNest.GetComponent(Naming.World.Nest);
			if (old.door != null)
				return old.door;
			else
				return this.ant.oldNest;
		}
	}

	private GameObject DoorCheck()
	{
		foreach (GameObject door in this.simManager.doors)
		{
			if (Vector3.Distance(door.transform.position, transform.position) < this.doorSenseRange && transform.InverseTransformPoint(door.transform.position).z >= 0)
			{
				if (!this.ant.inNest)
					return door.transform.parent.gameObject;
				else
					return door;
			}
		}
		return null;
	}

	//resets next turn time and distance counters
	private void Turned()
	{
		//if stuck then wait less time till next change as it may take a few random rotations to get unstuck
		if (Vector3.Distance(transform.position, this.lastTurn) > 0)
		{
			if (this.ant.state == AntManager.State.Assessing)
				this.nextDirChange_time = Time.timeSinceLevelLoad + Random.Range(0, 1f) * this.maxDirChange_time * 2f;
			else
				this.nextDirChange_time = Time.timeSinceLevelLoad + Random.Range(0, 1f) * this.maxDirChange_time;
		}
		else
			this.nextDirChange_time = Time.timeSinceLevelLoad + (Random.Range(0, 1f) * this.maxDirChange_time) / 10f;
		this.lastTurn = transform.position;
	}

	public void Enable()
	{
		this.cont.enabled = true;
	}

	public void Disable()
	{
		this.cont.enabled = false;
	}

	public bool isEnabled()
	{
		return this.cont.enabled;
	}

	private float normalRandom(float mean, float std)
	{
		try
		{
			return (float)this.rg.NormalDeviate() * std + mean;
		}
		catch
		{
			this.rg = new RandomGenerator();
			return (float)this.rg.NormalDeviate() * std + mean;
		}
	}

	/*this finds mid point (angle wise) between current direction and direction of given object
	then picks direction that is that mid point +/- an angle <= this.maxVar*/
	public void WalkToGameObject(GameObject nest)
	{
		float goalAngle;
		float currentAngle = transform.eulerAngles.y;

		//find angle mod 360 of current position to goal
		goalAngle = GetAngleBetweenPositions(transform.position, nest.transform.position);

		if (Mathf.Abs(goalAngle - currentAngle) > 180)
			currentAngle -= 360;

		float newDir = normalRandom((((goalAngle + currentAngle) / 2f) % 360f), this.maxVar);
		Turn(newDir);
	}

	private void RandomWalk()
	{
		float maxVar = this.maxVar;
		if (this.ant.state == AntManager.State.Assessing)
		{
			maxVar = 10f;
		}
		if (Vector3.Distance(transform.position, this.lastTurn) == 0)
			maxVar = 180;
		float theta = normalRandom(0, maxVar);
		float newDir = (this.dir + theta) % 360;
		Turn(newDir);
	}

	//turn ant to this face this direction (around y axis)
	private void Turn(float newDir)
	{
		this.dir = NewDirectionCheck(PheromoneDirection(newDir));
		transform.rotation = Quaternion.Euler(0, this.dir, 0);
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

		if (Time.timeSinceLevelLoad < this.nextPheromoneCheck)
			return direction;

		//if not using pheromones then just use given direction
		// only followers that are not in a nest uses phereomones
		if (this.ant.inNest || (this.ant.state != AntManager.State.Following && this.ant.state != AntManager.State.Scouting))
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
			strength = p.strength * Mathf.Exp(-0.5f * (square(2 * Mathf.Abs(p_a - direction) / this.maxVar)));

			//add this to the total vector
			d = p_t.position - transform.position;
			d.Normalize();
			v += d * strength;
		}

		//this stops long snake-like chains of ants following the same path over and over again
		if (Random.Range(0f, 1f) < 0.02f)
		{
			this.nextPheromoneCheck = Time.timeSinceLevelLoad + Random.Range(0, 1) * this.maxDirChange_time;
			return normalRandom(this.dir, maxVar);
		}

		//get angle and add noise
		return GetAngleBetweenPositions(transform.position, transform.position + v) + Mathf.Exp(-0.5f * (square(2 * Random.Range(-180, 180) / this.maxVar)));

	}

	private float square(float x)
	{
		return x * x;
	}

	//randomly turns ant
	private void RandomRotate()
	{
		float maxVar = this.maxVar;
		if (Vector3.Distance(transform.position, this.lastTurn) == 0)
			maxVar = 180;
		float newDir = normalRandom(0, maxVar);
		Turn(newDir);
	}

	//helps and to avoid obstructions and returns true if action was taken and false otherwise
	private bool ObstructionCheck()
	{
		RaycastHit hit = new RaycastHit();
		if (Physics.Raycast(transform.position, transform.forward, out hit, 1))
		{
			//if there is an ant directly in front of this ant then randomly turn otherwise must be a wall so follow it
			if (hit.collider.transform.tag == Naming.Ants.Tag)
			{
				// follower ant wants to have tactile contact with leader ant
				if (this.ant.state != AntManager.State.Following)
				{
					RandomRotate();
				}
			}
			else
				FollowWall();
			Turned();
			return true;
		}
		return false;
	}

	//this will only work well on right angle corners, randomness included so ant may not always follow round corner but might turn around
	private void FollowWall()
	{
		//find it if there are obstructions infront, behind and to either side of ant
		bool[] rays = new bool[4];
		rays[0] = Physics.Raycast(transform.position, Vector3.forward, 1);
		rays[1] = Physics.Raycast(transform.position, Vector3.right, 1);
		rays[2] = Physics.Raycast(transform.position, -Vector3.forward, 1);
		rays[3] = Physics.Raycast(transform.position, -Vector3.right, 1);
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
		float rand = Random.Range(0, sum);
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
		this.dir = 90f * (float)index;
		transform.rotation = Quaternion.Euler(0, this.dir, 0);
	}



	//
	//greg edit
	private void LayPheromoneScouting()
	{
		//		if( !(this.ant.state == AntManager.State.Scouting) || this.ant.inNest) {
		//			return;
		//		}
		GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
		pheromone.transform.parent = this.pheromoneParent;
		if (this.ant.state == AntManager.State.Scouting)
		{
			((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayScouting(this);
		}
	}

	private void LayPheromoneFTR()
	{
		if (!(this.ant.state == AntManager.State.Leading || this.ant.state == AntManager.State.Recruiting) || this.usePheromones == false || this.ant.inNest)
		{
			return;
		}
		GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
		pheromone.transform.parent = this.pheromoneParent;
		if (this.ant.state == AntManager.State.Reversing || this.ant.state == AntManager.State.Leading || this.ant.state == AntManager.State.Recruiting)
		{
			((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayTandem(this);
		}
	}

	private void LayPheromoneRTR()
	{
		if (!(this.ant.state == AntManager.State.Reversing) || this.usePheromones == false || this.ant.inNest)
		{
			return;
		}
		GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
		pheromone.transform.parent = this.pheromoneParent;
		if (this.ant.state == AntManager.State.Reversing || this.ant.state == AntManager.State.Leading || this.ant.state == AntManager.State.Recruiting)
		{
			((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayTandem(this);
		}
	}

	private void LayPheromoneAssessing()
	{
		if (this.ant.state != AntManager.State.Assessing || this.usePheromones == false || !this.ant.inNest)
		{
			return;
		}
		if (this.ant.nestToAssess == this.ant.oldNest)
		{
			return;
		}
		GameObject pheromone = (GameObject)Instantiate(pheromonePrefab, transform.position, Quaternion.identity);
		pheromone.transform.parent = this.pheromoneParent;
		if (this.ant.state == AntManager.State.Assessing)
		{
			if (this.ant.nestToAssess != this.ant.oldNest)
			{
				((Pheromone)pheromone.transform.GetComponent(Naming.Ants.Pheromone)).LayAssessing(this);
			}
		}
	}

	//


	private ArrayList PheromonesInRange()
	{
		Collider[] cols = Physics.OverlapSphere(transform.position, this.pheromoneRange);
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
		Collider[] cols = Physics.OverlapSphere(transform.position, this.assessmentPheromoneRange);
		ArrayList pher = new ArrayList();
		for (int i = 0; i < cols.Length; i++)
		{
			if (cols[i].tag == Naming.Ants.Pheromone)
				pher.Add(cols[i].transform.GetComponent(Naming.Ants.Pheromone));
		}
		return pher;
	}
}
