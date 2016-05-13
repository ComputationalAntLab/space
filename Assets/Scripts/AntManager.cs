using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;

public class AntManager : MonoBehaviour 
{
	//Individuals properties
	AntMovement move;						//controls movement
	Collider sensesCol;						//the collider used to sense ants and doors
	Transform carryPosition;				//where to carry ant 
	public State state;						//which state the ant is currently in
	State previousState;					//the state which the ant was in prior to assessing
	public GameObject myNest;  				//recruit to this nest
	public GameObject oldNest; 				//recruit from this nest
	public GameObject nestToAssess;			//nest that the ant is currently assessing
	public AntManager leader, follower;		//the ants that are leading or following this ant
	public bool inNest;   					//true when this ant is in a nest (needed for direction
	public bool newToOld; 					//true when this ant is heading towards the nest they are recruiting TO from the nest they're recruiting to FROM
	bool finishedRecruiting;				//true when this ant has finished recruiting and is returning to its nest
	public float nextAssesment;				//when the next assessment of the nest that this ant is will be carried out
	public float percievedQuality;			//the quality that this ant percieves this.myNest to be
	public float percievedQourum;			//the qourum that this ant percieves this.myNest to have
	public float nestThreshold;				//this individual's threshold for nest quality
	public int quorumThreshold;				//qourum threshold where recruiting ant carries rather than tandem runs
	public SimData History;
	public int revTime;
	public int recTime;
	public int assessTime;
	public RandomGenerator rg;
	
	// tandem run variables
	public int tandemTimeSteps;
	public bool forwardTandemRun;
	public bool reverseTandemRun;
	public Vector3 startPos;
	public Vector3 endPos;
	public int carryingTimeSteps;
	public bool socialCarrying;
	public bool followerWait = true;
	public bool leaderWaits = false;
	private int startTandemRunSeconds = 0;
	private int timeWhenTandemLostContact = 0;
	public float LGUT = 0.0f;
	public Vector3 estimateNewLeaderPos;
	public bool failedTandemLeader = false;
	public Vector3 leaderPositionContact;
	
	// buffon needle
	public int nestAssessmentVisitNumber = 0;
	private float assessmentFirstLengthHistory = 0;
	private float assessmentSecondLengthHistory = 0;
	private int assessmentFirstTimeHistory = 0;
	private int assessmentSecondTimeHistory = 0;
	public int assessmentStage = 0;
	private float currentNestArea = 0;

	
	//Parameters
	public bool passive = true;				//is this a passive ant or not
	public bool comparisonAssess = false;	//when true this allows the ant to compare new nests it encounters to the nest it currently has allegiance to
	public float qourumAssessNoise = 2f;	//std of normal distrubtion with mean equal to the nests actual qourum from which percieved qourum is drawn
	public float assessmentNoise = 0.1f;	//std of normal distrubtion with mean equal to the nests actual quality from which percieved quality is drawn
	public float maxAssessmentWait = 10f;	//maximum wait between asseesments of a nest when a !passive ant is in the Inactive state in a nest
	public float qualityThreshNoise = 0.1f;	//the std of the normal distibution from which this ants quality threshold for nests is picked
	public float qualityThreshMean = 0.5f;  //the mean of the normal distibution from which this ants quality threshold for nests is picked
	public float tandRecSwitchProb = 0.3f;	//the probability that an ant that is recruiting via tandem (though not leading at this time) can be recruited by another ant
	public float carryRecSwitchProb = 0.1f;	//the probability that an ant that is recruiting via transports (though not at this time) can be recruited by another ant
	public float pRecAssessOld = 0.1f;		//the probability that a recruiter assesses its old nest when it enters it
	public float pRecAssessNew = 0.2f;		//the probability that a recruiter assesses its new nest when it enters it
	public int timeStep = 0; 				//Stores time through emigration
	public int revTryTime = 2;				//No. of seconds an ant spends trying to RTR.
	public int droppedRecently = 0;			//Flag to show if an ant has been recently dropped or not.
	public int droppedWait = 5;
	public int recTryTime = 20;				//No. of collisions with passive ants before a recruiter gives up.
	
	//Other	
	
	public enum State 
	{
		Inactive,
		Scouting,
		Assessing,
		Recruiting,
		Following,
		Reversing,
		ReversingLeading,
		Leading,
		Carrying,
	};
	
	// Use this for initialization
	void Start () 
	{
		this.oldNest = GameObject.Find(Naming.World.InitialNest);
		this.carryPosition = transform.Find(Naming.Ants.CarryPosition);
		this.sensesCol = (Collider) transform.Find(Naming.Ants.SensesArea).GetComponent("Collider");
		this.move = (AntMovement) transform.GetComponent(Naming.Ants.Movement);
		this.nestThreshold = normalRandom(this.qualityThreshMean, this.qualityThreshNoise);
		this.percievedQuality = float.MinValue;
		this.finishedRecruiting = false;
		this.History = (SimData) transform.GetComponent(Naming.Simulation.SimData);
		//make sure the value is within contraints
		if(this.nestThreshold > 1)
			this.nestThreshold = 1;
		else if(this.nestThreshold < 0)
			this.nestThreshold = 0;
		
		
		//!passive ants assess nest as a soon as simulation begins
		/*
		NestManager nestM = (NestManager) this.oldNest.GetComponent("NestManager");
		if(nestM.quality < 0)
			this.nextAssesment = Time.timeSinceLevelLoad;
		else 
			this.nextAssesment = Time.timeSinceLevelLoad + Random.Range(0.5f, 1f) * this.maxAssessmentWait;
		*/
		InvokeRepeating("WriteHistory", 0f, 1.0f);
		InvokeRepeating("DecrementCounters", 0f, 1.0f);
	}
	
	//called every frame
	void Update()
	{	
		
		//BUGFIX: sometimes assessors leave nest without triggering OnExit in NestManager
		if(this.state == State.Assessing && Vector3.Distance(this.nestToAssess.transform.position, transform.position) > 
		   Mathf.Sqrt(Mathf.Pow(this.nestToAssess.transform.localScale.x, 2) + Mathf.Pow(this.nestToAssess.transform.localScale.z, 2)))
			LeftNest();
		
		//BUGFIX: occasionly when followers enter a nest there enterednest function doesn't get called, this forces that
		if(this.state == State.Following && Vector3.Distance(leadersNest().transform.position, transform.position) < leadersNest().transform.localScale.x/2f)
			EnteredNest(leadersNest());
		
		//makes Inactive and !passive ants assess nest that they are in every so often
		if(!this.passive && this.state == State.Inactive && this.nextAssesment > 0 && Time.timeSinceLevelLoad >= this.nextAssesment)
		{
			AssessNest(this.myNest);
			this.nextAssesment = Time.timeSinceLevelLoad + Random.Range(0.5f, 1f) * this.maxAssessmentWait;
		}
		
		//if an ant is carrying another and is within x distance of their nest's centre then drop the ant
		if(this.carryPosition.childCount > 0 && Vector3.Distance(this.myNest.transform.position, transform.position) < this.myNest.transform.localScale.x/4f)
		{
			((AntManager) this.carryPosition.Find(Naming.Ants.CarryAnt).GetComponent(Naming.Ants.Behaviour)).Dropped(this.myNest);
			
			// drop social carry "follower" calculate total timesteps for social carry
			if (socialCarrying == true) {
				carryingTimeSteps = -1 * (carryingTimeSteps - this.timeStep);
			}
			// get end position of social carry
			endPos = transform.position;
			// calculate total distance and speed of social carry
			float TRDistance = Vector3.Distance (endPos, startPos);
			float TRSpeed = TRDistance / (float)carryingTimeSteps;
			// update history with social carry and social carry speed
			if (socialCarrying == true) {
				this.History.carryingTimeSteps.Add(TRSpeed);
				socialCarrying = false;
			}
			Reverse(this.myNest);
		}
		
		//BUGFIX: Sometimes new to old is incorrectly set for recruiters - unclear why as of yet.
		if(this.state == State.Recruiting && this.follower != null && this.inNest && this.NearerOld())
		{
			this.newToOld = false;
		}
		
	}
	
	private void DecrementCounters()
	{
		//Only try reverse tandem runs for a certain amount of time
		if(this.state == State.Reversing && this.inNest && !NearerOld() && this.follower == null)
		{
			if(this.revTime <1)
			{
				RecruitToNest(this.myNest);
			}
			else
			{
				this.revTime -=1;
			}
		}
		
		if(this.droppedRecently > 0)
		{
			this.droppedRecently -= 1;
		}
		
		if(this.state == State.Assessing && this.assessTime > 0)
		{
			this.assessTime -= 1;
		}
		else if(this.state == State.Assessing)
		{
			if (this.assessmentStage == 0) {
				nestAssessmentVisit(); 
			}
		}
		
	}
	
	private void WriteHistory()
	{
		//Update timestep
		this.timeStep++;
		if(this.state == State.Recruiting)
		{
			if(this.isTransporting())
			{
				this.History.StateHistory.Add(AntManager.State.Carrying);
			}
			else if(this.isTandemRunning())
			{
				this.History.StateHistory.Add(AntManager.State.Leading);
			}
			else
			{
				this.History.StateHistory.Add(AntManager.State.Recruiting);
			}
		}
		else if(this.state==State.Reversing)
		{
			if(this.isTandemRunning())
			{
				this.History.StateHistory.Add(AntManager.State.ReversingLeading);
//				this.History.StateHistory.Add(AntManager.State.Reversing);
			}
			else
			{
				this.History.StateHistory.Add(AntManager.State.Reversing);
			}
		}
		else
		{
			this.History.StateHistory.Add(this.state);
		}
		
	}
	
	//makes sure that ants are always under correct parent object
	private void AssignParent()
	{
		if(transform.parent.tag == Naming.Ants.CarryPosition)
			return;
		else if(this.state == State.Recruiting)
		{
			int id = GetNestID(this.myNest);
			if(transform.parent.name != "R" + id)
			{
				transform.parent = GameObject.Find("R" + id).transform;
				GetComponent<Renderer>().material.color = Color.blue;
			}
		}
		else if(this.state == State.Inactive)
		{
			int id = GetNestID(this.myNest);
			if(transform.parent.name != "P" + id)
			{
				transform.parent = GameObject.Find("P" + id).transform;
				GetComponent<Renderer>().material.color = Color.black;
			}
		}
		else if(this.state == State.Scouting && transform.parent.name != "S")
		{
			transform.parent = GameObject.Find("S").transform;
			GetComponent<Renderer>().material.color = Color.white;
		}
		else if(this.state == State.Assessing)
		{
			int id = GetNestID(this.nestToAssess);
			if(transform.parent.name != "A" + id)
			{
				transform.parent = GameObject.Find("A" + id).transform;
				GetComponent<Renderer>().material.color = Color.red;
			}
		}
		else if(this.state == State.Reversing)
		{
			int id = GetNestID(this.myNest);
			if(transform.parent.name != "RT" + id)
			{
				transform.parent = GameObject.Find("RT" + id).transform;
				GetComponent<Renderer>().material.color = Color.yellow;
			}
		}
		/*else 
		{
        	transform.parent = GameObject.Find("F").transform;
		}*/
	}
	
	//returns true if this ant is carrying another
	public bool isTransporting()
	{
		if(transform.parent.tag == Naming.Ants.CarryPosition || this.carryPosition.childCount > 0) 
			return true;
		else 
			return false;
	}
	
	//returns true if this ant is leading or being led
	public bool isTandemRunning()
	{
		if(this.follower != null || this.leader != null) 
			return true;
		else 
			return false;
	}
	
	private GameObject leadersNest()
	{
		return ((AntManager) this.leader.GetComponent(Naming.Ants.Behaviour)).myNest;
	}
	
	//tell this ant to lead 'follower' to preffered nest
	public void Lead(AntManager follower)
	{
		
		if (this.failedTandemLeader == true && this.state == State.Recruiting) {
			this.failedTandemLeader = false;
			this.History.failedLeaderFoundFollowerAdd();
		}
		
		System.DateTime CurrentDate = new System.DateTime();
		CurrentDate = System.DateTime.Now;
		startTandemRunSeconds = (CurrentDate.Hour * 3600) + (CurrentDate.Minute * 60) + (CurrentDate.Second);
		
		// set start of forward tandem run (log start position and timestep)
		forwardTandemRun = true;
		startPos = transform.position;
		tandemTimeSteps = this.timeStep;
		
		this.leaderPositionContact = transform.position;
		
		// allow FTR leader to lay pheromones at rate 1.85 per sec (Basari, Trail Laying During Tandem Running)
		move.usePheromones = true;
		
		//let following ant know that you're leading it
		this.follower = follower;
		this.follower.Follow(this);
		this.newToOld = false;
		
		//turn this ant around to face towards chosen nest
		transform.LookAt(this.myNest.transform);
		
		//Update History
		if(this.History.firstTandem == 0)
		{
			this.History.firstTandem = this.timeStep;
		}
		this.History.numTandem++;   
	}
	
	public void ReverseLead(AntManager follower)
	{
		System.DateTime CurrentDate = new System.DateTime();
		CurrentDate = System.DateTime.Now;
		startTandemRunSeconds = (CurrentDate.Hour * 3600) + (CurrentDate.Minute * 60) + (CurrentDate.Second);
		
		// set start of reverse tandem run (log start position and timestep)
		reverseTandemRun = true;
		startPos = transform.position;
		tandemTimeSteps = this.timeStep;
		
		move.usePheromones = true;
		this.leaderPositionContact = transform.position;
		
		//let following ant know that you're leading it
		this.follower = follower;
		this.follower.Follow(this);
		this.newToOld = true;
		
		//turn this ant around to face towards chosen nest
		transform.LookAt(this.oldNest.transform);
		
		//Update History
		if(this.History.firstRev == 0)
		{
			this.History.firstRev = this.timeStep;
		}
		this.History.numRev++;  
		
	}
	
	public void StopLeading()
	{
		this.startTandemRunSeconds = 0;
		this.followerWait = true;
		this.leaderWaits = false;
		
		// get total time steps taken for tandem run
		this.tandemTimeSteps = -1 * (this.tandemTimeSteps - this.timeStep);
		
		// get end poistion of tandem run
		this.endPos = transform.position;
		// calculate distance covered for tandem run
		float TRDistance = Vector3.Distance (this.endPos, this.startPos);
		// calculate the speed of tandem run (Unity Distance / Unity timesteps) 
		float TRSpeed = TRDistance / (float)tandemTimeSteps;  
		
		// update forward / reverse tandem run speed and successful tandem run
		if (forwardTandemRun == true) {
			this.History.forwardTandemTimeSteps.Add(TRSpeed);
			this.History.completeFTR();
			forwardTandemRun = false;
		} else if (reverseTandemRun == true) {
			this.History.reverseTandemTimeSteps.Add(TRSpeed);
			this.History.completeRTR();
			reverseTandemRun = false;
		}
		
		// leader has stopped leader => does not lay pheromones [as frequently]
		// (Basari, Trail Laying During Tandem Running)
		move.usePheromones = false;
		
		this.follower = null;
		RecruitToNest(this.myNest);
	}
	
	//returns true if there is a line of sight between this ant and the given object
	public bool LineOfSight(GameObject obj)
	{
		float distance = 20f;
		if (this.leader != null) { distance = 4.5f; }
		RaycastHit hit;
		if(Physics.Raycast(transform.position, obj.transform.position - transform.position, out hit, distance))
		{
			if(hit.collider.transform == obj.transform) 
				return true;	
		}
		return false;
	}
	
	//follow the leader ant 
	public void Follow(AntManager leader)
	{
		System.DateTime CurrentDate = new System.DateTime();
		CurrentDate = System.DateTime.Now;
		startTandemRunSeconds = (CurrentDate.Hour * 3600) + (CurrentDate.Minute * 60) + (CurrentDate.Second);
		
		//start following leader towards nest
		ChangeState(State.Following);
		this.newToOld = false;
		this.leader = leader;
		
		//we want to turn to follow the leader now
		move.ChangeDirection();
		
		this.followerWait = true;
	}
	
	public void StopFollowing()
	{
		this.followerWait = true;
		this.leaderWaits = false;
		this.startTandemRunSeconds = 0;
		this.estimateNewLeaderPos = new Vector3 (0, 0, 0);
		this.leader = null;
	}
	
	//makes this ant pick up 'otherAnt' and carry them back to preffered nest
	public void PickUp(AntManager otherAnt)
	{
		socialCarrying = true;
		startPos = transform.position;
		carryingTimeSteps = this.timeStep;
		
		otherAnt.PickedUp(transform);
		this.newToOld = false;
		transform.LookAt(this.myNest.transform);
		
		if(this.History.firstCarry == 0)
		{
			this.History.firstCarry = this.timeStep;
		}
		this.History.numCarry++;
	}
	
	//lets this ant know that it has been picked up by carrier
	public void PickedUp(Transform carrier)
	{
		//stop moving
		this.move.Disable();
		
		//get into position
		Transform carryPosition = carrier.Find(Naming.Ants.CarryPosition);
		transform.parent = carryPosition;
		transform.position = carryPosition.position;
		transform.rotation = Quaternion.Euler(0, 0, 90);
		
		//turn off senses
		this.sensesCol.enabled  = false;
	}
	
	//lets this ant know that it has been put down, sets it upright and turns senses back on 
	public void Dropped(GameObject nest)
	{
		//turn the right way up 
		transform.rotation = Quaternion.identity;
		transform.position = new Vector3(transform.position.x + 1, 1.08f, transform.position.z);
		this.move.Enable();
		
		if(transform.parent.tag == Naming.Ants.CarryPosition) 
		{
			int id = GetNestID(nest);
			transform.parent = GameObject.Find("P" + id).transform;
		}
		
		//make ant inactive in this nest
		this.oldNest = nest;
		this.myNest = nest;
		this.droppedRecently = this.droppedWait;
		//this.nextAssesment = Time.timeSinceLevelLoad + Random.Range(0.5f, 1f) * this.maxAssessmentWait;
		ChangeState(State.Inactive);
		
		//turns senses on if non passive ant
		if(!this.passive) 
			this.sensesCol.enabled = true;
		
		//Store history
		int nestID = GetNestID(nest)-1;
		if(nestID >= 0)
		{
			this.History.NestDiscoveryType[nestID] = SimData.DiscoveryType.Lead;
			this.History.NestDiscoveryTime[nestID] = this.timeStep;
		}
	}
	
	//returns true if ant is within certain range of nest centre and there are no more passive ants ro recruit there
	public bool OldNestOccupied()
	{
		if(oldNest == null) 
			return false;
		int id = GetNestID(oldNest);
		if(GameObject.Find("P" + id).transform.childCount == 0 && Vector3.Distance(oldNest.transform.position, transform.position) < 10) 
		{
			//oldNest = null;
			newToOld = true;
			return false;
		}
		else 
			return true;
	}
	
	//this is called whenever an ant enters a nest
	public void EnteredNest(GameObject nest)
	{
		
		if (this.failedTandemLeader == true && this.state == State.Recruiting && nest != this.oldNest) {
			this.failedTandemLeader = false;
			this.History.failedLeaderNewNestAdd();
		}
		
		this.inNest = true; 
		
		//Deal with History
		int nestID = GetNestID(nest)-1;
		if(nestID >= 0)
		{
			if(this.History.NestDiscoveryTime[nestID] == 0)
			{
				this.History.NestDiscoveryTime[nestID] = this.timeStep;	
				if(this.state == State.Following)
				{
					this.History.NestDiscoveryType[nestID] = SimData.DiscoveryType.Lead;
				}
				else
				{
					this.History.NestDiscoveryType[nestID] = SimData.DiscoveryType.Found;
				}
			}
		}
		
		//ignore ants that have just been dropped here
		if(nest == this.myNest && this.state == State.Inactive)
			return;
		
		//ignore ants that are carrying or are being carried
		if(this.carryPosition.childCount > 0 || transform.parent.tag == Naming.Ants.CarryPosition) 
			return;
		
		//if this ant has been lead to this nest then tell leader that it's done its job
		if(this.state == State.Following && this.leader != null) 
		{
			if(this.leader.state == State.Recruiting && nest != this.leader.oldNest)
			{
				((AntManager) leader.transform.GetComponent(Naming.Ants.Behaviour)).StopLeading();
				StopFollowing();
				if(this.passive)
				{
					this.myNest = nest;
					ChangeState(State.Inactive);
					return;
				}
				else
				{
					this.nestToAssess = nest;
					ChangeState(State.Assessing);
				}
			}
			else if(this.leader.state == State.Reversing && nest == this.leader.oldNest)
			{
				AntManager leader = (AntManager) this.leader.transform.GetComponent(Naming.Ants.Behaviour);
				this.myNest = leader.myNest;
				this.oldNest = leader.oldNest;
				leader.StopLeading();
				StopFollowing();
				RecruitToNest(this.myNest);
			}
		}
		
		//if entering own nest and finished recruiting then become inactive
		if(this.state == State.Recruiting && nest == this.myNest)
		{
			
			if(this.finishedRecruiting == true)
			{
				ChangeState(State.Inactive);
				this.finishedRecruiting = false;
				return;
			}
			else
			{
				if(this.follower == null && Random.Range(0f, 1f) < this.pRecAssessNew && !this.IsQuorumReached())
				{
					this.nestToAssess = nest;
					ChangeState(State.Assessing);
				}
				else
				{
					RecruitToNest(nest);
				}
			}
		}
		
		if(this.state == State.Recruiting && nest == this.oldNest)
		{
			NestManager nestM = (NestManager) nest.transform.GetComponent(Naming.World.Nest);
			
			//if no passive ants left in old nest then turn around and return home
			if(this.finishedRecruiting || nestM.GetPassive() == 0)
			{
				this.newToOld = false;
				this.finishedRecruiting = true;
				return;
			}
			//if recruiting and this is old nest then assess with probability pRecAssessOld
			else if(Random.Range(0f, 1f) < this.pRecAssessOld)
			{
				this.nestToAssess = nest;
				ChangeState(State.Assessing);
				return;
			}
		}
		
		if(this.state == State.Reversing && nest == this.oldNest)
		{
			NestManager nestM = (NestManager) nest.transform.GetComponent(Naming.World.Nest);
			
			if(nestM.GetPassive() == 0)
			{
				this.newToOld = false;
				ChangeState(State.Recruiting);
				this.finishedRecruiting = true;
				return;
			}
		}
		
		
		//if either ant is following or this isn't one of the ants known nests then assess it
		if(((this.state == State.Scouting || this.state == State.Recruiting) && nest != oldNest && nest != myNest) || this.state == State.Following && this.leader.state != State.Reversing && nest != this.leader.oldNest) 
		{		
			this.nestToAssess = nest;
			ChangeState(State.Assessing);
		}
		else 
		{
			int id = GetNestID(nest);
			//if this nest is my old nest and there's nothing to recruit from it then stop coming here
			if(nest == oldNest && GameObject.Find("P" + id).transform.childCount == 0)
				//oldNest = null;    
			
			//if recruiting and this is your nest then go back to looking around for ants to recruit 
			if(this.state == State.Recruiting && nest == this.myNest && this.follower == null)
				RecruitToNest(this.myNest);
		}
	} 
	
	
	//greg edit
	//
	private void nestAssessmentVisit() {
		
		if (this.nestAssessmentVisitNumber == 1) {
			// store lenght of first visit and reset length to zero
			this.assessmentFirstLengthHistory = this.move.assessingDistance; 
			this.move.assessingDistance = 0f;
			this.assessmentStage = 1;
			print("finished assessment");
			return;
		}
		this.assessmentSecondLengthHistory = this.move.assessingDistance;
		this.move.assessingDistance = 0f;
		
		// store buffon needle assessment values and reset (once assessment has finishe)
		storeAssessmentHistory();
		
		AssessNest(this.nestToAssess);
	}
	
	public void nestAssessmentSeconfVisit() {
		GetComponent<Renderer>().material.color = Color.grey;
		this.assessmentStage = 0;
		this.nestAssessmentVisitNumber = 2;
		this.assessTime = getAssessTime();
		move.usePheromones = false;
	}
	
	
	
	private void storeAssessmentHistory() {
		if (this.nestAssessmentVisitNumber != 2) {
			return;
		}
		
		// store history once assessment finished
		this.History.assessmentFirstLength.Add(this.assessmentFirstLengthHistory);
		this.History.assessmentSecondLength.Add(this.assessmentSecondLengthHistory);
		this.History.assessmentFirstTime.Add(this.assessmentFirstTimeHistory);
		this.History.assessmentSecondTime.Add(this.assessmentSecondTimeHistory);
		
		if (this.move.intersectionNumber != 0f) {
			
			float area = (2.0f * this.assessmentFirstLengthHistory * this.assessmentSecondLengthHistory) / (3.14159265359f * this.move.intersectionNumber);
			this.currentNestArea = area;

			SimulationManager simManager = (SimulationManager) GameObject.Find(Naming.World.InitialNest).transform.GetComponent(Naming.Simulation.Manager);
			int ID = simManager.nests.IndexOf(this.nestToAssess.transform);
			this.History.assessmentAreaResult.Add("nest_" + ID + "\":'" + area);
			
		}
		
		// reset values
		this.assessmentFirstLengthHistory = 0;
		this.assessmentSecondLengthHistory = 0;
		this.assessmentFirstTimeHistory = 0;
		this.assessmentSecondTimeHistory = 0;
		this.nestAssessmentVisitNumber = 0;
		this.move.intersectionNumber = 0f;
	}
	//
	
	//assesses nest and takes appropriate action
	private void AssessNest(GameObject nest)
	{   
		move.usePheromones = false;

		//make assessment of this nest's quality
		int nestID = GetNestID(nest)-1;
		if (nestID>=0 && nest != this.myNest)
		{
			this.History.numAssessments[nestID]++;
		}

//greg edit 
/*
		// nest quality based on size of nest
		float q = 0f;
		if (this.currentNestArea <= 2000f) {
			q = 0.0005f * this.currentNestArea;
		} else if (this.currentNestArea > 2000f) {
			q = 1 - (0.0005f * (this.currentNestArea - 2000f));
		}
*/

		//reset current nest area
		this.currentNestArea = 0f;

		/*
		 * Old nest quality measurement
		 *
		 */
		NestManager nestM = (NestManager) nest.transform.GetComponent(Naming.World.Nest);
		float q = normalRandom(nestM.quality, this.assessmentNoise);
		if(q < 0f) 
			q = 0f;
		else if(q > 1f)
			q = 1f;


		//if an !passive ant decides that his current isn't good enough then go look for another
		if(this.state == State.Inactive && nest == this.myNest)
		{         
			this.percievedQuality = q;  
			if(q < this.nestThreshold) 
			{
				this.oldNest = this.myNest;
				ChangeState(State.Scouting);
			}
		}
		else
		{
			//if not using comparison then check if this nest is as good or better than threshold
			if(!this.comparisonAssess && q >= this.nestThreshold) 
			{
				if (nest != this.myNest)
				{
					this.oldNest = this.myNest;
				}
				if(this.follower != null)
				{
					this.follower.myNest = nest;
					StopLeading();
				}
				this.percievedQuality = q;
				RecruitToNest(nest);
				if (nestID>=0)
				{
					this.History.numAcceptance[nestID]++;
				}
				if (this.myNest != null)
				{
					this.History.numSwitch++;
				}
			}
			//if not using comparison then check if this reaches threshold and is better than previous nest
			else if(this.comparisonAssess && q >= this.nestThreshold && (this.myNest == null || q > this.percievedQuality))
			{
				if (nest != this.myNest)
				{
					this.oldNest = this.myNest;
					if (nestID >= 0)
					{
						this.History.numAcceptance[nestID]++;
					}
					
					if (this.myNest != null)
					{
						this.History.numSwitch++;
					}
				}
				
				if(this.follower != null)
				{
					this.follower.myNest = nest;
					StopLeading();
				}
				this.percievedQuality = q;
				RecruitToNest(nest);
				
			}
			else
			{
				if(this.previousState == State.Scouting)
					ChangeState(State.Scouting);
				else if(this.previousState == State.Recruiting)
					RecruitToNest(this.myNest);	
			}
		}
	}
	
	//returns true if quorum has been reached in this.myNest 
	public bool IsQuorumReached()
	{
		return this.percievedQourum >= this.quorumThreshold;
	}
	
	//returns the ID of the nest that is passed in
	public int GetNestID(GameObject nest)
	{
		SimulationManager simManager = (SimulationManager) GameObject.Find(Naming.World.InitialNest).transform.GetComponent(Naming.Simulation.Manager);
		return simManager.nests.IndexOf(nest.transform);
	}
	
	//called whenever an ant leaves a nest
	public void LeftNest()
	{
		this.inNest = false;
		
		//when an assessor leaves the nest then make decision about wether to recruit TO that nest
		if(this.state == State.Assessing && this.assessTime == 0) {
			if (this.assessmentStage == 0) {
				nestAssessmentVisit();	
			}	
			
		}
		
		//Update Emigration History Data
		if(this.History.LeftOld == 0)
		{
			this.History.LeftOld = this.timeStep;
		}
	}	
	
	//changes state of ant and assigns the correct parent in gameobject heirachy
	public void ChangeState(State state)
	{
		if(state == State.Assessing) {
			ChangeStateAssessing();
		}
		if(this.state != State.Following && this.state != State.Assessing) {
			this.previousState = this.state;
		}
		this.state = state;
		AssignParent();
	}
	
	private void ChangeStateAssessing() {
		
		// make this nest assessment their first visit
		this.nestAssessmentVisitNumber = 1;
		this.assessTime = getAssessTime();
		// get start position of assessor ant
		this.move.lastPosition = this.move.transform.position;
		move.usePheromones = true;
	}
	
	// greg edit
	//
	private int getAssessTime() {
		// Eamonn B. Mallon and Nigel R. Franks - Ants estimate area using Buffonâ€™s needle
		// The median time that a scout spends within a nest cavity assessing a potential nest is 110 s per visit (interquartile range 140 s and n = 115)
		// range must be +/- 55. As 125+55=180 (110+70). And 95-55=40 (110-70)
		//greg edit		float halfInterquartileRangeAssessTime = 40f;
		float halfInterquartileRangeAssessTime = 7f;
		int averageAssessTime;
		if (this.nestAssessmentVisitNumber == 1) {
			//greg edit			averageAssessTime = 142;
			averageAssessTime = 28;
		} else {
			//greg edit			averageAssessTime = 80;
			averageAssessTime = 16;
		}
		
		float deviate = (float)rg.UniformDeviate(-1, 1);
		int duration = (averageAssessTime + (int)(halfInterquartileRangeAssessTime * deviate));
				
		if (this.nestAssessmentVisitNumber == 1) {
			this.assessmentFirstTimeHistory = duration;
		} else {
			this.assessmentSecondTimeHistory = duration;
		}
		return duration; 
	}
	//
	
	//switches ants allegiance to this nest and sends them back to their old one to recruit some more
	private void RecruitToNest(GameObject nest)
	{
		int nestID = GetNestID(nest)-1;
		if(nestID>=0)
		{
			if(this.History.NestRecruitTime[nestID] == 0)
			{
				this.History.NestRecruitTime[nestID] = this.timeStep;
			}
		}
		this.newToOld = true;
		this.myNest = nest;
		NestManager nestM = (NestManager) nest.transform.GetComponent(Naming.World.Nest);
		//check the qourum of this nest until quorum is met once.
		if(IsQuorumReached())
		{
			this.percievedQourum = this.quorumThreshold;
		}
		else
		{
			this.percievedQourum = Mathf.Round(normalRandom(nestM.GetQuorum(), this.qourumAssessNoise));
		}
		
		this.recTime = this.recTryTime;
		ChangeState(State.Recruiting);
	}
	
	private void Reverse(GameObject nest)
	{
		ChangeState(State.Reversing);
		this.revTime = this.revTryTime;
	}
	
	//returns true if this ant is nearer it's old nest than new
	public bool NearerOld()
	{
		return Vector3.Distance(transform.position, oldNest.transform.position) < Vector3.Distance(transform.position, myNest.transform.position);
	}
	
	
	private float normalRandom(float mean, float std)
	{
		try
		{
			return (float)this.rg.NormalDeviate()*std + mean;
		}
		catch
		{
			this.rg = new RandomGenerator();
			return (float)this.rg.NormalDeviate()*std + mean;
		}
	}
	
	// called once leader is 2*antennaReach away from follower
	public void tandemContactLost() {
		if (startTandemRunSeconds == 0) {
			return;
		}
		// log the time that the tandem run was lost
		System.DateTime CurrentDate = new System.DateTime();
		CurrentDate = System.DateTime.Now;
		int currentTime = (CurrentDate.Hour * 3600) + (CurrentDate.Minute * 60) + (CurrentDate.Second);
		this.timeWhenTandemLostContact = currentTime;
		// calculate the Leader Give-Up Time (LGUT)
		calculateLGUT(currentTime);		 
	}
	
	// calculate the LGUT that the leader and follower will wait for a re-connection
	private void calculateLGUT(int currentTime) {
		int tandemDuration = (currentTime - startTandemRunSeconds) * (int)Time.timeScale;;
		double exponent = 0.9651 + 0.3895 * Mathf.Log10(tandemDuration);
		this.LGUT = Mathf.Pow(10, (float)exponent);
	}
	
	// called once a follower has re-connected with the tandem leader (re-sets values)
	public void tandemContactRegained() {
		this.LGUT = 0.0f;
		this.timeWhenTandemLostContact = 0;
	}
	
	// every time step this function is called from a searching follower
	public bool hasLGUTExpired() {
		if (this.LGUT == 0.0 || this.timeWhenTandemLostContact == 0) { return false; }
		
		System.DateTime CurrentDate = new System.DateTime();
		CurrentDate = System.DateTime.Now;
		int currentTime = (CurrentDate.Hour * 3600) + (CurrentDate.Minute * 60) + (CurrentDate.Second);
		int durationLostContact = (currentTime - timeWhenTandemLostContact) * (int)Time.timeScale;;
		
		// if duration since lost contact is longer than LGUT then tandem run has failed  
		if (durationLostContact > this.LGUT) { return true; } 
		else { return false; }
	}
	
	// if duration of lost contact is greater than LGUT fail tandem run
	public void failedTandemRun() {
		if(this.state == State.Following && this.leader != null) {
			if(this.leader.state == State.Recruiting || this.leader.state == State.Reversing) {
				// failed tandem leader behaviour
				((AntManager)leader.transform.GetComponent(Naming.Ants.Behaviour)).failedTandemLeaderBehvaiour();
				// failed tandem follower behaviour
				failedTandemFollowerBehaviour();
			}
		}
	}
	
	// failed tandem leader behaviour
	private void failedTandemLeaderBehvaiour() {
		this.startTandemRunSeconds = 0;	
		
		// log failed tandem run in history
		if (forwardTandemRun == true) {
			this.History.failedFTR();
			forwardTandemRun = false;
		} else if (reverseTandemRun == true) {
			this.History.failedRTR();
			reverseTandemRun = false;
		}
		// reset tandem variables
		this.leaderWaits = false;
		this.followerWait = true;
		// turn off pheromones
		move.usePheromones = false;
		this.follower = null;
		
		// behaviour after failed tandem run
		ChangeState(State.Recruiting);
		this.failedTandemLeader = true;

		if (this.previousState == State.Reversing) {
			this.newToOld = true;		
		} else {
			this.newToOld = false;
		}
	}
	
	// failed tandem follower behaviour
	private void failedTandemFollowerBehaviour() {
		StopFollowing();
		
		// greg edit
		//TODO need to make accurate behaviour after
		if (this.myNest == this.oldNest)
		{
			ChangeState(State.Scouting);
		}
		else
		{
			ChangeState(State.Inactive);	
		}
	}
}



