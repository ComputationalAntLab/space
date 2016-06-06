using UnityEngine;

public class Pheromone : MonoBehaviour 
{
	public float strength;				//strength of pheromone between 0 and 1
	public float duration; 				//total duration of pheromone in seconds, pheromone weakens linearly from 1 to 0 over duration
	public AntMovement owner; 			//the ant who laid this pheromone 
	public bool assessingPheromone;		//identifies if the pheromone is for assessing
	public bool assessingPheromoneCounted;
	
	
	//greg edit
	public void LayScouting(AntMovement owner) {
		SphereCollider myCollider = transform.GetComponent<SphereCollider>();
		//myCollider.radius = 0.5f;
		myCollider.radius = 2f;
		
		this.owner = owner;
        strength = -1.0f;
        assessingPheromone = false;
		
//		InvokeRepeating("WeakenScouting", 1, 1);
	}
	//greg edit
	private void WeakenScouting() {
        strength += (0.5f/duration);
		if(strength >= 0)
			Destroy(gameObject);
	}

	
	public void LayTandem(AntMovement owner) {

		SphereCollider myCollider = transform.GetComponent<SphereCollider>();
		myCollider.radius = 0.5f;
		
		this.owner = owner;
		
		if (this.owner.ant.state == AntManager.BehaviourState.Recruiting) {
            strength = 1.05f;
		} else {
            strength = 1.0f;
		}

        assessingPheromone = false;		
		InvokeRepeating("Weaken", 1, 1);
	}
	
	private void Weaken() {
        strength -= (0.5f/duration);
		if(strength <= 0)
			Destroy(gameObject);
	}
	
	
	public void LayAssessing(AntMovement owner) {
		SphereCollider myCollider = transform.GetComponent<SphereCollider>();
		myCollider.radius = 0.125f; 
		
		this.owner = owner;
        strength = 0.0f;
        assessingPheromone = true;
        assessingPheromoneCounted = false;
		
		// when the owner is no longer assessing the nest remove the pheromones
		InvokeRepeating("Remove", 1, 1);
		InvokeRepeating("ResetPheromoneCount", 1, 1);
	}
	
	private void Remove() {
		if(owner.ant.state != AntManager.BehaviourState.Assessing) {
			Destroy(gameObject);
		}
	}
	
	private void ResetPheromoneCount() {
		if (assessingPheromoneCounted == true) {
			if (Vector3.Distance (transform.position, owner.transform.position) > (owner.assessmentPheromoneRange * 1.1f) ) {
                assessingPheromoneCounted = false;
			}
		}
	}
	
	public void destroyPheromone() {
		Destroy(gameObject);
	}
	
}

/*
  
     \path [line] (init) -- node[midway, above, color=black] 
    	{Scouting\ \ \ \ \ \ \ \ \ \ \ \ } (switchS);
    \path [line] (init) -- node[midway, above, color=black] 
    	{\ \ \ \ \ \ \ \ \ \ \ \ Recruiting} (switchR);
    \path [line] (switchS) -- node[midway, above, color=black] 
    	{\ \ \ \ \ \ Yes} (switchSY);
    \path [line] (switchS) -- node[midway, above, color=black] 
    	{No\ \ \ \ } (switchSN);
    \path [line] (switchR) -- node[midway, above, color=black] 
    	{\ \ Yes} (switchRY);
    \path [line] (switchR) -- node[midway, above, color=black] 
    	{No\ \ \ \ \ \ } (switchRN);
    \path [line] (switchRN) -- node[midway, above, color=black] 
    	{\ \ \ \ \ \ \  $>$ 0} (passive);
    \path [line] (switchRN) -- node[midway, above, color=black] 
    	{0\ \ } (nopassive);
    

 * */