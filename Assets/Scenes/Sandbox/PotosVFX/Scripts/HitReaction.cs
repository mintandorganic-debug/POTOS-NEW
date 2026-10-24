using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitReaction : MonoBehaviour
{
	public List<GameObject> HitEffects = new List<GameObject>();
	public TorchDemo.WeaponType AttackType = TorchDemo.WeaponType.None;//на будущее
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	
	public void SetupHitEffects(Matrix4x4 origin){
		foreach (GameObject obj in HitEffects){
			switch (obj.name){
				case "ShockLightningMesh":
					if(AttackType == TorchDemo.WeaponType.Tazer){
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetMatrix("_HitMatrix",origin);
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetFloat("_HitStart",Time.time);
					}
				break;
				case "ShockLayer":
					if(AttackType == TorchDemo.WeaponType.Tazer){
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetMatrix("_HitMatrix",origin);
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetFloat("_HitStart",Time.time);
					}
				break;
				case "head":
					if(AttackType == TorchDemo.WeaponType.Whiskey){
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetMatrix("_HitMatrix",origin);
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetFloat("_HitStart",Time.time);
					}
				break;
				case "arm_l":
					if(AttackType == TorchDemo.WeaponType.Whiskey){
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetMatrix("_HitMatrix",origin);
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetFloat("_HitStart",Time.time);
					}
				break;
				case "arm_r":
					if(AttackType == TorchDemo.WeaponType.Whiskey){
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetMatrix("_HitMatrix",origin);
						obj.GetComponent<SkinnedMeshRenderer>().materials[0].SetFloat("_HitStart",Time.time);
					}
				break;
			}
		}
	}
}
