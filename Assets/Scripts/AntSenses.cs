using UnityEngine;
using Assets.Scripts;

public class AntSenses : MonoBehaviour 
{
	AntManager ant;
	public float range = 3f; //the range from which this ant can sense other ants
	
	void OnTriggerEnter(Collider other) 
	{
		//initialise ant
		if(this.ant == null) 
			this.ant = (AntManager) transform.parent.GetComponent(Naming.Ants.Controller);
		
		if(other.tag != Naming.Ants.Tag)
		{
			return;
		}
		
		AntManager otherAnt = (AntManager)other.transform.GetComponent(Naming.Ants.Controller);
		
		if(this.ant.state == AntManager.State.Reversing && !this.ant.isTandemRunning())
		{
			//only inactive scouts can be reverse tandem run
			if (otherAnt.state == AntManager.State.Inactive && otherAnt.passive == false && otherAnt.droppedRecently == 0)
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
		if(this.ant.state != AntManager.State.Recruiting || this.ant.isTransporting() || this.ant.isTandemRunning()) 
			return;
		
		//assessing and following ants can't be recruited
		if(otherAnt.state == AntManager.State.Assessing || otherAnt.state == AntManager.State.Following)
			return;
		
		//if ant already has allegiance to the same nest, or the other ant is currently transporting or tandem running or we can't see the other ant then ignore
		if(otherAnt.myNest == this.ant.myNest | otherAnt.isTransporting() || otherAnt.isTandemRunning() || !ant.LineOfSight(otherAnt.gameObject)) 
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
		if(otherAnt.state == AntManager.State.Recruiting)
		{
			float r = Random.Range(0f, 1f);
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
