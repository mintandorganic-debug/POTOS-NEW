using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Order : System.Object {
	//task for movement or other action
	public float timer = 0f;//time to move towards fixed distance
	public float duration = 1f;//duration of movement
	public Vector3 moveVector = new Vector3();//distance and direction to move
	public string name = "default";
	public enum Composition{
		additive,
		replace
	}
	public int priority = 0;//priority of the Replace mode. Dodge moves override basic movement. Stagger/knockback override everything
	public Composition composition = Composition.additive;
	public AnimationCurve curve;//animation curve of the movement

	// Update is called once per frame
	void Update () {
	
	}
}
