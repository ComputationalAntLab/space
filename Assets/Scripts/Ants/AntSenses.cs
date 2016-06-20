using UnityEngine;
using Assets.Scripts;

public class AntSenses : MonoBehaviour 
{
	AntManager ant;
	public float range = 3f; //the range from which this ant can sense other ants
	
	void OnTriggerEnter(Collider other) 
	{
		//initialise ant
		if(ant == null)
            ant = (AntManager) transform.parent.GetComponent(Naming.Ants.Controller);
		
		if(other.tag != Naming.Ants.Tag)
		{
			return;
		}
		
		AntManager otherAnt = (AntManager)other.transform.GetComponent(Naming.Ants.Controller);
		
		if(ant.state == AntManager.BehaviourState.Reversing && !ant.IsTandemRunning())
		{
			//only inactive scouts can be reverse tandem run
			if (otherAnt.state == AntManager.BehaviourState.Inactive && otherAnt.passive == false && otherAnt.droppedRecently == 0)
			{
				
				ant.ReverseLead(otherAnt);
				
				return;
			}
			/*
			if(otherAnt.state == AntManager.State.Scouting)
			{
				ant.ReverseLead(otherAnt);
				return;
			}
			*/
			
		}
		
		
		//only continue if this ant is recruiting, the collision was with an ant and this ant isn't currently leading or carrying
		if(ant.state != AntManager.BehaviourState.Recruiting || ant.IsTransporting() || ant.IsTandemRunning()) 
			return;
		
		//assessing and following ants can't be recruited
		if(otherAnt.state == AntManager.BehaviourState.Assessing || otherAnt.state == AntManager.BehaviourState.Following)
			return;
		
		//if ant already has allegiance to the same nest, or the other ant is currently transporting or tandem running or we can't see the other ant then ignore
		if(otherAnt.myNest == ant.myNest | otherAnt.IsTransporting() || otherAnt.IsTandemRunning() || !ant.LineOfSight(otherAnt.gameObject)) 
		{
			if (ant.inNest && ant.NearerOld())
			{
				if (ant.recTime > 0)
				{
					ant.recTime -= 1;
				}  
				else
				{
					ant.newToOld = false;
				}
			}
			
			return;
		}
		
		//if the ant is recruiting then use probabilities to decide wether they can be recruited
		if(otherAnt.state == AntManager.BehaviourState.Recruiting)
		{
			float r = RandomGenerator.Instance.Range(0f, 1f);
			if(otherAnt.IsQuorumReached())
			{
				if(r > otherAnt.carryRecSwitchProb)
					if (ant.recTime > 0)
					{
					ant.recTime -= 1;
					}  
					else
					{	
					ant.newToOld = false;
					}
				return;
			}
			else 
			{
				if(r > otherAnt.tandRecSwitchProb)
				{
					if (ant.recTime > 0)
					{
						ant.recTime -= 1;
					}  
					else
					{
						ant.newToOld = false;
					}
					return;
				}
				
			}
		}
		
		//if quorum reached then carry the other ant, otherwise lead them
		if(ant.IsQuorumReached()) 
			ant.PickUp(otherAnt);
		else
			if(otherAnt.passive == false)
		{
			ant.Lead(otherAnt);
		}
		else
		{
			if (ant.recTime > 0)
			{
				ant.recTime -= 1;
			}
			else
			{
				ant.newToOld = false;
			}
		}
	}
}