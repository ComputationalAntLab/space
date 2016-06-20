using UnityEngine;
using Assets.Scripts;
using Assets.Scripts.Extensions;
using Assets.Scripts.Ticking;

public class AntManager : MonoBehaviour, ITickable
{
    public int AntId { get; set; }

    //Individuals properties
    public SimulationManager simulation;
    AntMovement move;                       //controls movement
    Collider sensesCol;                     //the collider used to sense ants and doors
    Transform carryPosition;                //where to carry ant 
    public BehaviourState state;                     //which state the ant is currently in
    BehaviourState previousState;                    //the state which the ant was in prior to assessing
    public GameObject myNest;               //recruit to this nest
    public GameObject oldNest;              //recruit from this nest
    public GameObject nestToAssess;         //nest that the ant is currently assessing
    public AntManager leader, follower;     //the ants that are leading or following this ant
    public bool inNest;                     //true when this ant is in a nest (needed for direction
    public bool newToOld;                   //true when this ant is heading towards the nest they are recruiting TO from the nest they're recruiting to FROM
    bool finishedRecruiting;                //true when this ant has finished recruiting and is returning to its nest
    public float nextAssesment;             //when the next assessment of the nest that this ant is will be carried out
    public float percievedQuality;          //the quality that this ant percieves this.myNest to be
    public float percievedQourum;           //the qourum that this ant percieves this.myNest to have
    public float nestThreshold;             //this individual's threshold for nest quality
    public int quorumThreshold;             //qourum threshold where recruiting ant carries rather than tandem runs
    public SimData History;
    public int revTime;
    public int recTime;
    public int assessTime;

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
    private float startTandemRunSeconds = 0;
    private float timeWhenTandemLostContact = 0;
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
    public bool passive = true;             //is this a passive ant or not
    public bool comparisonAssess = false;   //when true this allows the ant to compare new nests it encounters to the nest it currently has allegiance to
    public float qourumAssessNoise = 2f;    //std of normal distrubtion with mean equal to the nests actual qourum from which percieved qourum is drawn
    public float assessmentNoise = 0.1f;    //std of normal distrubtion with mean equal to the nests actual quality from which percieved quality is drawn
    public float maxAssessmentWait = 10f;   //maximum wait between asseesments of a nest when a !passive ant is in the Inactive state in a nest
    public float qualityThreshNoise = 0.1f; //the std of the normal distibution from which this ants quality threshold for nests is picked
    public float qualityThreshMean = 0.5f;  //the mean of the normal distibution from which this ants quality threshold for nests is picked
    public float tandRecSwitchProb = 0.3f;  //the probability that an ant that is recruiting via tandem (though not leading at this time) can be recruited by another ant
    public float carryRecSwitchProb = 0.1f; //the probability that an ant that is recruiting via transports (though not at this time) can be recruited by another ant
    public float pRecAssessOld = 0.1f;      //the probability that a recruiter assesses its old nest when it enters it
    public float pRecAssessNew = 0.2f;      //the probability that a recruiter assesses its new nest when it enters it
    public int timeStep = 0;                //Stores time through emigration
    public int revTryTime = 2;              //No. of seconds an ant spends trying to RTR.
    public int droppedRecently = 0;         //Flag to show if an ant has been recently dropped or not.
    public int droppedWait = 5;
    public int recTryTime = 20;             //No. of collisions with passive ants before a recruiter gives up.

    //Other	
    public bool ShouldBeRemoved { get { return false; } }


    // Use this for initialization
    void Start()
    {
        oldNest = GameObject.Find(Naming.World.InitialNest);
        carryPosition = transform.Find(Naming.Ants.CarryPosition);
        sensesCol = (Collider)transform.Find(Naming.Ants.SensesArea).GetComponent("Collider");
        move = gameObject.AntMovement();
        nestThreshold = RandomGenerator.Instance.NormalRandom(qualityThreshMean, qualityThreshNoise);
        percievedQuality = float.MinValue;
        finishedRecruiting = false;
        History = gameObject.AntData();
        //make sure the value is within contraints
        if (nestThreshold > 1)
            nestThreshold = 1;
        else if (nestThreshold < 0)
            nestThreshold = 0;
    }

    private float _elapsed;
    public void Tick(float elapsedSimulationMS)
    {
        if (Ticker.Should(elapsedSimulationMS, ref _elapsed, 1000))
        {
            WriteHistory();
            DecrementCounters();
        }

        move.Tick(elapsedSimulationMS);

        //BUGFIX: sometimes assessors leave nest without triggering OnExit in NestManager
        if (state == BehaviourState.Assessing && Vector3.Distance(nestToAssess.transform.position, transform.position) >
           Mathf.Sqrt(Mathf.Pow(nestToAssess.transform.localScale.x, 2) + Mathf.Pow(nestToAssess.transform.localScale.z, 2)))
            LeftNest();

        //BUGFIX: occasionly when followers enter a nest there enterednest function doesn't get called, this forces that
        if (state == BehaviourState.Following && Vector3.Distance(LeadersNest().transform.position, transform.position) < LeadersNest().transform.localScale.x / 2f)
            EnteredNest(LeadersNest());

        //makes Inactive and !passive ants assess nest that they are in every so often
        if (!passive && state == BehaviourState.Inactive && nextAssesment > 0 && Time.timeSinceLevelLoad >= nextAssesment)
        {
            AssessNest(myNest);
            nextAssesment = Time.timeSinceLevelLoad + RandomGenerator.Instance.Range(0.5f, 1f) * maxAssessmentWait;
        }

        //if an ant is carrying another and is within x distance of their nest's centre then drop the ant
        if (carryPosition.childCount > 0 && Vector3.Distance(myNest.transform.position, transform.position) < myNest.transform.localScale.x / 4f)
        {
            var c0 = carryPosition.GetChild(0);
            var carriedAnt = carryPosition.Find(Naming.Ants.Tag);
            var carriedAntBehaviour = carriedAnt.gameObject.AntManager();

            carriedAntBehaviour.Dropped(myNest);

            // drop social carry "follower" calculate total timesteps for social carry
            if (socialCarrying == true)
            {
                carryingTimeSteps = -1 * (carryingTimeSteps - timeStep);
            }
            // get end position of social carry
            endPos = transform.position;
            // calculate total distance and speed of social carry
            float TRDistance = Vector3.Distance(endPos, startPos);
            float TRSpeed = TRDistance / carryingTimeSteps;
            // update history with social carry and social carry speed
            if (socialCarrying == true)
            {
                History.carryingTimeSteps.Add(TRSpeed);
                socialCarrying = false;
            }
            Reverse(myNest);
        }

        //BUGFIX: Sometimes new to old is incorrectly set for recruiters - unclear why as of yet.
        if (state == BehaviourState.Recruiting && follower != null && inNest && NearerOld())
        {
            newToOld = false;
        }

    }

    private void DecrementCounters()
    {
        //Only try reverse tandem runs for a certain amount of time
        if (state == BehaviourState.Reversing && inNest && !NearerOld() && follower == null)
        {
            if (revTime < 1)
            {
                RecruitToNest(myNest);
            }
            else
            {
                revTime -= 1;
            }
        }

        if (droppedRecently > 0)
        {
            droppedRecently -= 1;
        }

        if (state == BehaviourState.Assessing && assessTime > 0)
        {
            assessTime -= 1;
        }
        else if (state == BehaviourState.Assessing)
        {
            if (assessmentStage == 0)
            {
                NestAssessmentVisit();
            }
        }

    }

    private void WriteHistory()
    {
        //Update timestep
        timeStep++;
        if (state == BehaviourState.Recruiting)
        {
            if (IsTransporting())
            {
                History.StateHistory.Add(AntManager.BehaviourState.Carrying);
            }
            else if (IsTandemRunning())
            {
                History.StateHistory.Add(AntManager.BehaviourState.Leading);
            }
            else
            {
                History.StateHistory.Add(AntManager.BehaviourState.Recruiting);
            }
        }
        else if (state == BehaviourState.Reversing)
        {
            if (IsTandemRunning())
            {
                History.StateHistory.Add(AntManager.BehaviourState.ReversingLeading);
                //				this.History.StateHistory.Add(AntManager.State.Reversing);
            }
            else
            {
                History.StateHistory.Add(AntManager.BehaviourState.Reversing);
            }
        }
        else
        {
            History.StateHistory.Add(state);
        }

    }

    private void AssignParent(GameObject nest, string prefix, Color? colour)
    {
        if (nest != null)
        {
            prefix += simulation.GetNestID(nest);
        }
        if (transform.parent.name != prefix)
        {
            transform.parent = GameObject.Find(prefix).transform;
            if (colour.HasValue)
                GetComponent<Renderer>().material.color = colour.Value;
        }
    }

    //makes sure that ants are always under correct parent object
    private void AssignParent()
    {
        if (transform.parent.tag == Naming.Ants.CarryPosition)
            return;
        else if (state == BehaviourState.Recruiting)
        {
            AssignParent(myNest, Naming.Ants.BehavourState.Recruiting, Color.blue);
        }
        else if (state == BehaviourState.Inactive)
        {
            AssignParent(myNest, Naming.Ants.BehavourState.Inactive, Color.black);
        }
        else if (state == BehaviourState.Scouting && transform.parent.name != "S")
        {
            AssignParent(null, Naming.Ants.BehavourState.Scouting, Color.white);
        }
        else if (state == BehaviourState.Assessing)
        {
            AssignParent(nestToAssess, Naming.Ants.BehavourState.Assessing, Color.red);
        }
        else if (state == BehaviourState.Reversing)
        {
            AssignParent(myNest, Naming.Ants.BehavourState.Reversing, Color.yellow);
        }
    }

    //returns true if this ant is carrying another
    public bool IsTransporting()
    {
        if (transform.parent.tag == Naming.Ants.CarryPosition || carryPosition.childCount > 0)
            return true;
        else
            return false;
    }

    //returns true if this ant is leading or being led
    public bool IsTandemRunning()
    {
        if (follower != null || leader != null)
            return true;
        else
            return false;
    }

    private GameObject LeadersNest()
    {
        return leader.gameObject.AntManager().myNest;
    }

    //tell this ant to lead 'follower' to preffered nest
    public void Lead(AntManager follower)
    {
        if (failedTandemLeader == true && state == BehaviourState.Recruiting)
        {
            failedTandemLeader = false;
            History.failedLeaderFoundFollowerAdd();
        }

        startTandemRunSeconds = simulation.TickManager.TotalElapsedSimulatedSeconds;

        // set start of forward tandem run (log start position and timestep)
        forwardTandemRun = true;
        startPos = transform.position;
        tandemTimeSteps = timeStep;

        leaderPositionContact = transform.position;

        // allow FTR leader to lay pheromones at rate 1.85 per sec (Basari, Trail Laying During Tandem Running)
        move.usePheromones = true;

        //let following ant know that you're leading it
        this.follower = follower;
        this.follower.Follow(this);
        newToOld = false;

        //turn this ant around to face towards chosen nest
        transform.LookAt(myNest.transform);

        //Update History
        if (History.firstTandem == 0)
        {
            History.firstTandem = timeStep;
        }
        History.numTandem++;
    }

    public void ReverseLead(AntManager follower)
    {
        startTandemRunSeconds = simulation.TickManager.TotalElapsedSimulatedSeconds;

        // set start of reverse tandem run (log start position and timestep)
        reverseTandemRun = true;
        startPos = transform.position;
        tandemTimeSteps = timeStep;

        move.usePheromones = true;
        leaderPositionContact = transform.position;

        //let following ant know that you're leading it
        this.follower = follower;
        this.follower.Follow(this);
        newToOld = true;

        //turn this ant around to face towards chosen nest
        transform.LookAt(oldNest.transform);

        //Update History
        if (History.firstRev == 0)
        {
            History.firstRev = timeStep;
        }
        History.numRev++;

    }

    public void StopLeading()
    {
        startTandemRunSeconds = 0;
        followerWait = true;
        leaderWaits = false;

        // get total time steps taken for tandem run
        tandemTimeSteps = -1 * (tandemTimeSteps - timeStep);

        // get end poistion of tandem run
        endPos = transform.position;
        // calculate distance covered for tandem run
        float TRDistance = Vector3.Distance(endPos, startPos);
        // calculate the speed of tandem run (Unity Distance / Unity timesteps) 
        float TRSpeed = TRDistance / tandemTimeSteps;

        // update forward / reverse tandem run speed and successful tandem run
        if (forwardTandemRun)
        {
            History.forwardTandemTimeSteps.Add(TRSpeed);
            History.completeFTR();
            forwardTandemRun = false;
        }
        else if (reverseTandemRun)
        {
            History.reverseTandemTimeSteps.Add(TRSpeed);
            History.completeRTR();
            reverseTandemRun = false;
        }

        // leader has stopped leader => does not lay pheromones [as frequently]
        // (Basari, Trail Laying During Tandem Running)
        move.usePheromones = false;

        follower = null;
        RecruitToNest(myNest);
    }

    //returns true if there is a line of sight between this ant and the given object
    public bool LineOfSight(GameObject obj)
    {
        float distance = 20f;
        if (leader != null) { distance = 4.5f; }
        RaycastHit hit;
        if (Physics.Raycast(transform.position, obj.transform.position - transform.position, out hit, distance))
        {
            if (hit.collider.transform == obj.transform)
                return true;
        }
        return false;
    }

    //follow the leader ant 
    public void Follow(AntManager leader)
    {
        startTandemRunSeconds = simulation.TickManager.TotalElapsedSimulatedSeconds;

        //start following leader towards nest
        ChangeState(BehaviourState.Following);
        newToOld = false;
        this.leader = leader;

        //we want to turn to follow the leader now
        move.ChangeDirection();

        followerWait = true;
    }

    public void StopFollowing()
    {
        followerWait = true;
        leaderWaits = false;
        startTandemRunSeconds = 0;
        estimateNewLeaderPos = Vector3.zero;
        leader = null;
    }

    //makes this ant pick up 'otherAnt' and carry them back to preffered nest
    public void PickUp(AntManager otherAnt)
    {
        socialCarrying = true;
        startPos = transform.position;
        carryingTimeSteps = timeStep;

        otherAnt.PickedUp(transform);
        newToOld = false;
        transform.LookAt(myNest.transform);

        if (History.firstCarry == 0)
        {
            History.firstCarry = timeStep;
        }
        History.numCarry++;
    }

    //lets this ant know that it has been picked up by carrier
    public void PickedUp(Transform carrier)
    {
        //stop moving
        move.Disable();

        //get into position
        Transform carryPosition = carrier.Find(Naming.Ants.CarryPosition);
        transform.parent = carryPosition;
        transform.position = carryPosition.position;
        transform.rotation = Quaternion.Euler(0, 0, 90);

        //turn off senses
        sensesCol.enabled = false;
    }

    //lets this ant know that it has been put down, sets it upright and turns senses back on 
    public void Dropped(GameObject nest)
    {
        //turn the right way up 
        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(transform.position.x + 1, 1.08f, transform.position.z);
        move.Enable();

        if (transform.parent.tag == Naming.Ants.CarryPosition)
        {
            int id = simulation.GetNestID(nest);
            transform.parent = GameObject.Find("P" + id).transform;
        }

        //make ant inactive in this nest
        oldNest = nest;
        myNest = nest;
        droppedRecently = droppedWait;
        //this.nextAssesment = Time.timeSinceLevelLoad + RandomGenerator.Instance.Range(0.5f, 1f) * this.maxAssessmentWait;
        ChangeState(BehaviourState.Inactive);

        //turns senses on if non passive ant
        if (!passive)
            sensesCol.enabled = true;

        //Store history
        int nestID = simulation.GetNestID(nest) - 1;
        if (nestID >= 0)
        {
            History.NestDiscoveryType[nestID] = SimData.DiscoveryType.Lead;
            History.NestDiscoveryTime[nestID] = timeStep;
        }
    }

    //returns true if ant is within certain range of nest centre and there are no more passive ants ro recruit there
    public bool OldNestOccupied()
    {
        if (oldNest == null)
            return false;
        int id = simulation.GetNestID(oldNest);
        if (GameObject.Find("P" + id).transform.childCount == 0 && Vector3.Distance(oldNest.transform.position, transform.position) < 10)
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
        if (failedTandemLeader == true && state == BehaviourState.Recruiting && nest != oldNest)
        {
            failedTandemLeader = false;
            History.failedLeaderNewNestAdd();
        }

        inNest = true;

        //Deal with History
        int nestID = simulation.GetNestID(nest) - 1;
        if (nestID >= 0)
        {
            if (History.NestDiscoveryTime[nestID] == 0)
            {
                History.NestDiscoveryTime[nestID] = timeStep;
                if (state == BehaviourState.Following)
                {
                    History.NestDiscoveryType[nestID] = SimData.DiscoveryType.Lead;
                }
                else
                {
                    History.NestDiscoveryType[nestID] = SimData.DiscoveryType.Found;
                }
            }
        }

        //ignore ants that have just been dropped here
        if (nest == myNest && state == BehaviourState.Inactive)
            return;

        //ignore ants that are carrying or are being carried
        if (carryPosition.childCount > 0 || transform.parent.tag == Naming.Ants.CarryPosition)
            return;

        //if this ant has been lead to this nest then tell leader that it's done its job
        if (state == BehaviourState.Following && leader != null)
        {
            if (leader.state == BehaviourState.Recruiting && nest != leader.oldNest)
            {
                leader.StopLeading();
                StopFollowing();
                if (passive)
                {
                    myNest = nest;
                    ChangeState(BehaviourState.Inactive);
                    return;
                }
                else
                {
                    nestToAssess = nest;
                    ChangeState(BehaviourState.Assessing);
                }
            }
            else if (leader.state == BehaviourState.Reversing && nest == leader.oldNest)
            {
                myNest = leader.myNest;
                oldNest = leader.oldNest;
                leader.StopLeading();
                StopFollowing();
                RecruitToNest(myNest);
            }
        }

        //if entering own nest and finished recruiting then become inactive
        if (state == BehaviourState.Recruiting && nest == myNest)
        {

            if (finishedRecruiting == true)
            {
                ChangeState(BehaviourState.Inactive);
                finishedRecruiting = false;
                return;
            }
            else
            {
                if (follower == null && RandomGenerator.Instance.Range(0f, 1f) < pRecAssessNew && !IsQuorumReached())
                {
                    nestToAssess = nest;
                    ChangeState(BehaviourState.Assessing);
                }
                else
                {
                    RecruitToNest(nest);
                }
            }
        }

        if (state == BehaviourState.Recruiting && nest == oldNest)
        {
            NestManager nestM = nest.Nest();

            //if no passive ants left in old nest then turn around and return home
            if (finishedRecruiting || nestM.GetPassive() == 0)
            {
                newToOld = false;
                finishedRecruiting = true;
                return;
            }
            //if recruiting and this is old nest then assess with probability pRecAssessOld
            else if (RandomGenerator.Instance.Range(0f, 1f) < pRecAssessOld)
            {
                nestToAssess = nest;
                ChangeState(BehaviourState.Assessing);
                return;
            }
        }

        if (state == BehaviourState.Reversing && nest == oldNest)
        {
            NestManager nestM = nest.Nest();

            if (nestM.GetPassive() == 0)
            {
                newToOld = false;
                ChangeState(BehaviourState.Recruiting);
                finishedRecruiting = true;
                return;
            }
        }


        //if either ant is following or this isn't one of the ants known nests then assess it
        if (((state == BehaviourState.Scouting || state == BehaviourState.Recruiting) && nest != oldNest && nest != myNest) || state == BehaviourState.Following && leader.state != BehaviourState.Reversing && nest != leader.oldNest)
        {
            nestToAssess = nest;
            ChangeState(BehaviourState.Assessing);
        }
        else
        {
            int id = simulation.GetNestID(nest);
            //if this nest is my old nest and there's nothing to recruit from it then stop coming here
            if (nest == oldNest && GameObject.Find("P" + id).transform.childCount == 0)
                //oldNest = null;    

                //if recruiting and this is your nest then go back to looking around for ants to recruit 
                if (state == BehaviourState.Recruiting && nest == myNest && follower == null)
                    RecruitToNest(myNest);
        }
    }

    private void NestAssessmentVisit()
    {

        if (nestAssessmentVisitNumber == 1)
        {
            // store lenght of first visit and reset length to zero
            assessmentFirstLengthHistory = move.assessingDistance;
            move.assessingDistance = 0f;
            assessmentStage = 1;
            print("finished assessment");
            return;
        }
        assessmentSecondLengthHistory = move.assessingDistance;
        move.assessingDistance = 0f;

        // store buffon needle assessment values and reset (once assessment has finishe)
        StoreAssessmentHistory();

        AssessNest(nestToAssess);
    }

    public void NestAssessmentSecondVisit()
    {
        GetComponent<Renderer>().material.color = Color.grey;
        assessmentStage = 0;
        nestAssessmentVisitNumber = 2;
        assessTime = GetAssessTime();
        move.usePheromones = false;
    }

    private void StoreAssessmentHistory()
    {
        if (nestAssessmentVisitNumber != 2)
        {
            return;
        }

        // store history once assessment finished
        History.assessmentFirstLength.Add(assessmentFirstLengthHistory);
        History.assessmentSecondLength.Add(assessmentSecondLengthHistory);
        History.assessmentFirstTime.Add(assessmentFirstTimeHistory);
        History.assessmentSecondTime.Add(assessmentSecondTimeHistory);

        if (move.intersectionNumber != 0f)
        {

            float area = (2.0f * assessmentFirstLengthHistory * assessmentSecondLengthHistory) / (3.14159265359f * move.intersectionNumber);
            currentNestArea = area;

            int ID = simulation.nests.IndexOf(nestToAssess.transform);
            History.assessmentAreaResult.Add("nest_" + ID + "\":'" + area);

        }

        // reset values
        assessmentFirstLengthHistory = 0;
        assessmentSecondLengthHistory = 0;
        assessmentFirstTimeHistory = 0;
        assessmentSecondTimeHistory = 0;
        nestAssessmentVisitNumber = 0;
        move.intersectionNumber = 0f;
    }
    //

    //assesses nest and takes appropriate action
    private void AssessNest(GameObject nest)
    {
        move.usePheromones = false;

        //make assessment of this nest's quality
        int nestID = simulation.GetNestID(nest) - 1;
        if (nestID >= 0 && nest != myNest)
        {
            History.numAssessments[nestID]++;
        }

        //reset current nest area
        currentNestArea = 0f;

        // Old nest quality measurement
        NestManager nestM = nest.Nest();
        float q = RandomGenerator.Instance.NormalRandom(nestM.quality, assessmentNoise);
        if (q < 0f)
            q = 0f;
        else if (q > 1f)
            q = 1f;


        //if an !passive ant decides that his current isn't good enough then go look for another
        if (state == BehaviourState.Inactive && nest == myNest)
        {
            percievedQuality = q;
            if (q < nestThreshold)
            {
                oldNest = myNest;
                ChangeState(BehaviourState.Scouting);
            }
        }
        else
        {
            //if not using comparison then check if this nest is as good or better than threshold
            if (!comparisonAssess && q >= nestThreshold)
            {
                if (nest != myNest)
                {
                    oldNest = myNest;
                }
                if (follower != null)
                {
                    follower.myNest = nest;
                    StopLeading();
                }
                percievedQuality = q;
                RecruitToNest(nest);
                if (nestID >= 0)
                {
                    History.numAcceptance[nestID]++;
                }
                if (myNest != null)
                {
                    History.numSwitch++;
                }
            }
            //if not using comparison then check if this reaches threshold and is better than previous nest
            else if (comparisonAssess && q >= nestThreshold && (myNest == null || q > percievedQuality))
            {
                if (nest != myNest)
                {
                    oldNest = myNest;
                    if (nestID >= 0)
                    {
                        History.numAcceptance[nestID]++;
                    }

                    if (myNest != null)
                    {
                        History.numSwitch++;
                    }
                }

                if (follower != null)
                {
                    follower.myNest = nest;
                    StopLeading();
                }
                percievedQuality = q;
                RecruitToNest(nest);

            }
            else
            {
                if (previousState == BehaviourState.Scouting)
                    ChangeState(BehaviourState.Scouting);
                else if (previousState == BehaviourState.Recruiting)
                    RecruitToNest(myNest);
            }
        }
    }

    //returns true if quorum has been reached in this.myNest 
    public bool IsQuorumReached()
    {
        return percievedQourum >= quorumThreshold;
    }

    //called whenever an ant leaves a nest
    public void LeftNest()
    {
        inNest = false;

        //when an assessor leaves the nest then make decision about wether to recruit TO that nest
        if (state == BehaviourState.Assessing && assessTime == 0)
        {
            if (assessmentStage == 0)
            {
                NestAssessmentVisit();
            }

        }

        //Update Emigration History Data
        if (History.LeftOld == 0)
        {
            History.LeftOld = timeStep;
        }
    }

    //changes state of ant and assigns the correct parent in gameobject heirachy
    public void ChangeState(BehaviourState state)
    {
        if (state == BehaviourState.Assessing)
        {
            ChangeStateAssessing();
        }
        if (this.state != BehaviourState.Following && this.state != BehaviourState.Assessing)
        {
            previousState = this.state;
        }
        this.state = state;
        AssignParent();
    }

    private void ChangeStateAssessing()
    {

        // make this nest assessment their first visit
        nestAssessmentVisitNumber = 1;
        assessTime = GetAssessTime();
        // get start position of assessor ant
        move.lastPosition = move.transform.position;
        move.usePheromones = true;
    }

    private int GetAssessTime()
    {
        // Eamonn B. Mallon and Nigel R. Franks - Ants estimate area using Buffon's needle
        // The median time that a scout spends within a nest cavity assessing a potential nest is 110 s per visit (interquartile range 140 s and n = 115)
        // range must be +/- 55. As 125+55=180 (110+70). And 95-55=40 (110-70)
        //greg edit		float halfInterquartileRangeAssessTime = 40f;
        float halfInterquartileRangeAssessTime = 7f;
        int averageAssessTime;
        if (nestAssessmentVisitNumber == 1)
        {
            //greg edit			averageAssessTime = 142;
            averageAssessTime = 28;
        }
        else {
            //greg edit			averageAssessTime = 80;
            averageAssessTime = 16;
        }

        float deviate = (float)RandomGenerator.Instance.UniformDeviate(-1, 1);
        int duration = (averageAssessTime + (int)(halfInterquartileRangeAssessTime * deviate));

        if (nestAssessmentVisitNumber == 1)
        {
            assessmentFirstTimeHistory = duration;
        }
        else {
            assessmentSecondTimeHistory = duration;
        }
        return duration;
    }
    //

    //switches ants allegiance to this nest and sends them back to their old one to recruit some more
    private void RecruitToNest(GameObject nest)
    {
        int nestID = simulation.GetNestID(nest) - 1;
        if (nestID >= 0)
        {
            if (History.NestRecruitTime[nestID] == 0)
            {
                History.NestRecruitTime[nestID] = timeStep;
            }
        }
        newToOld = true;
        myNest = nest;
        NestManager nestM = nest.Nest();
        //check the qourum of this nest until quorum is met once.
        if (IsQuorumReached())
        {
            percievedQourum = quorumThreshold;
        }
        else
        {
            percievedQourum = Mathf.Round(RandomGenerator.Instance.NormalRandom(nestM.GetQuorum(), qourumAssessNoise));
        }

        recTime = recTryTime;
        ChangeState(BehaviourState.Recruiting);
    }

    private void Reverse(GameObject nest)
    {
        ChangeState(BehaviourState.Reversing);
        revTime = revTryTime;
    }

    //returns true if this ant is nearer it's old nest than new
    public bool NearerOld()
    {
        return Vector3.Distance(transform.position, oldNest.transform.position) < Vector3.Distance(transform.position, myNest.transform.position);
    }

    // called once leader is 2*antennaReach away from follower
    public void TandemContactLost()
    {
        if (startTandemRunSeconds == 0)
        {
            return;
        }
        // log the time that the tandem run was lost
        timeWhenTandemLostContact = simulation.TickManager.TotalElapsedSimulatedSeconds;
        // calculate the Leader Give-Up Time (LGUT)
        CalculateLGUT();
    }

    // calculate the LGUT that the leader and follower will wait for a re-connection
    private void CalculateLGUT()
    {
        float tandemDuration = (simulation.TickManager.TotalElapsedSimulatedSeconds - startTandemRunSeconds);
        double exponent = 0.9651 + 0.3895 * Mathf.Log10(tandemDuration);
        LGUT = Mathf.Pow(10, (float)exponent);
    }

    // called once a follower has re-connected with the tandem leader (re-sets values)
    public void TandemContactRegained()
    {
        LGUT = 0.0f;
        timeWhenTandemLostContact = 0;
    }

    // every time step this function is called from a searching follower
    public bool HasLGUTExpired()
    {
        if (LGUT == 0.0 || timeWhenTandemLostContact == 0) { return false; }

        float durationLostContact = (simulation.TickManager.TotalElapsedSimulatedSeconds - timeWhenTandemLostContact);

        // if duration since lost contact is longer than LGUT then tandem run has failed  
        return durationLostContact > LGUT;
    }

    // if duration of lost contact is greater than LGUT fail tandem run
    public void FailedTandemRun()
    {
        if (state == BehaviourState.Following && leader != null)
        {
            if (leader.state == BehaviourState.Recruiting || leader.state == BehaviourState.Reversing)
            {
                // failed tandem leader behaviour
                leader.FailedTandemLeaderBehvaiour();
                // failed tandem follower behaviour
                FailedTandemFollowerBehaviour();
            }
        }
    }

    // failed tandem leader behaviour
    private void FailedTandemLeaderBehvaiour()
    {
        startTandemRunSeconds = 0;

        // log failed tandem run in history
        if (forwardTandemRun == true)
        {
            History.failedFTR();
            forwardTandemRun = false;
        }
        else if (reverseTandemRun == true)
        {
            History.failedRTR();
            reverseTandemRun = false;
        }
        // reset tandem variables
        leaderWaits = false;
        followerWait = true;
        // turn off pheromones
        move.usePheromones = false;
        follower = null;

        // behaviour after failed tandem run
        ChangeState(BehaviourState.Recruiting);
        failedTandemLeader = true;

        if (previousState == BehaviourState.Reversing)
        {
            newToOld = true;
        }
        else {
            newToOld = false;
        }
    }

    // failed tandem follower behaviour
    private void FailedTandemFollowerBehaviour()
    {
        StopFollowing();

        // greg edit
        //TODO need to make accurate behaviour after
        if (myNest == oldNest)
        {
            ChangeState(BehaviourState.Scouting);
        }
        else
        {
            ChangeState(BehaviourState.Inactive);
        }
    }

    public enum BehaviourState
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
}



