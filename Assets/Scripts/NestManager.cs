using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NestManager : MonoBehaviour 
{
	public float quality = 0.5f; //between zero and one
	public GameObject door = null;
	
	void OnTriggerEnter(Collider other) 
	{
		//if other isn't an ant or an ants collider has intersected with nest collider in an area that isn't the entrance then ignore
		if(other.tag != "Ant" || (door != null && Vector3.Distance(other.transform.position, door.transform.position) > 12)) 
			return;
		
		//let the ant know it has entered the nest
		AntManager ant = (AntManager)other.transform.GetComponent("AntManager");
		ant.EnteredNest(gameObject);
	}
	
	
	void OnTriggerExit(Collider other) 
	{
		//if other isn't an ant or an ants collider has intersected with nest collider in an area that isn't the entrance then ignore
		if(other.tag != "Ant" || (door != null && Vector3.Distance(other.transform.position, door.transform.position) > 12)) 
			return;
		AntManager ant = (AntManager)other.transform.GetComponent("AntManager");
		
		//if ant is passive and somehow reaches edge of nest then turn around, otherwise let the ant know it has left the nest
		if(ant.state == AntManager.State.Inactive) 
			ant.transform.rotation = Quaternion.Euler(0, (ant.transform.rotation.eulerAngles.y + 180) % 360, 0);
		else 
			ant.LeftNest();
	}
	
	public int GetQuorum()
	{
		int id = GetNestID(gameObject);
		int total = GameObject.Find("P" + id).transform.childCount;
		Transform a = GameObject.Find("A" + id).transform;

		for (int i = 0; i < a.childCount; i++)
		{
			AntManager antM = (AntManager) a.GetChild(i).GetComponent("AntManager");
			if (antM.inNest && !antM.NearerOld())
				total += 1;
		}
		
		Transform r = GameObject.Find("R" + id).transform;
		for(int i = 0; i < r.childCount; i++)
		{
			AntManager antM = (AntManager) r.GetChild(i).GetComponent("AntManager");
			//if ant recruiting ant is in a nest and it is nearer its new nest than old then it is counted as part of the qourum
			if(antM.inNest && !antM.NearerOld())
				total += 1;
		}
		//no need to count self so minus one
		return total - 1;
	}
	
	public int GetPassive()
	{
		return GameObject.Find("P" + GetNestID(gameObject)).transform.childCount;
	}
	
	//returns the ID of the nest that is passed in
	private int GetNestID(GameObject nest)
	{
		SimulationManager simManager = (SimulationManager) GameObject.Find("OldNest").transform.GetComponent("SimulationManager");
		return simManager.nests.IndexOf(nest.transform);
	}
}