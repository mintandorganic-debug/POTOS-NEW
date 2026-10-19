//считывает скорость движения детали и передаёт её в шейдер пламени, для эффекта колебания
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WobbleImpulse : MonoBehaviour

{
	private Vector3 OldPos = Vector3.zero;
	public float WobbleMult = 2.0f;
	public float MaxDelta = 0.3f;
    // Start is called before the first frame update
    void Start()
    {
        OldPos = this.transform.position;
		this.GetComponent<MeshRenderer>().materials[0].SetFloat("_WobbleStart",0.0f);
		this.GetComponent<MeshRenderer>().materials[0].SetVector("_WobbleOffset",new Vector3(0f,0f,0f));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
		//перемещение этого объекта со времени предыдущего кадра
        Vector3 Delta =  this.transform.position - OldPos;
		if(Delta.magnitude >= MaxDelta){//ограничение максимального смещения
			Delta = Delta.normalized * MaxDelta;
		}
		if(Delta.magnitude >= 0.01f){
			this.GetComponent<MeshRenderer>().materials[0].SetFloat("_WobbleStart",Time.time);
			this.GetComponent<MeshRenderer>().materials[0].SetVector("_WobbleOffset",Delta*WobbleMult);
		}
		OldPos = this.transform.position;
    }
}
