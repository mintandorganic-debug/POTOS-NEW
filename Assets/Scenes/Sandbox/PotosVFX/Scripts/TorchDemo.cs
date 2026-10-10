using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;//теперь использует новую систему ввода


public class TorchDemo : MonoBehaviour
{
	private bool Activated = false;
	public GameObject hands;
	public GameObject WeaponIndicator;
	public LayerMask targets;
	public float ScanDist = 5f;
	public GameObject AttackOrigin;
	public GameObject WhiskeyEmitter;
	public List <GameObject> TazerThreads = new List<GameObject>();//2 троса
	public float ThreadSpread = 0.2f;
	public enum WeaponType {
		None,
		Torch,
		Tazer,
		Dice,
		Whiskey
	}
	public WeaponType CurrentWeap = WeaponType.Torch;
	private Animator anim;
	private Vector3 hitPosition = new Vector3(0f,0f,0f);
	
	public InputAction fire1;
	public InputAction fire2;
	public InputAction fire3;
	
	public InputAction slot1;
	public InputAction slot2;
	public InputAction slot3;
	public InputAction slot4;
	
	public float ChargeSpeed = 0.05f;
	public float Charge = 0f;
	private bool CanCharge = false;//
	private bool NeedsInit = false;
	public Slider ChargeSlider;
	
	
    // Start is called before the first frame update
    void Start()
    {
		fire1.Enable();
		fire2.Enable();
		fire3.Enable();
		slot1.Enable();
		slot2.Enable();
		slot3.Enable();
		slot4.Enable();
		
        anim = hands.GetComponent<Animator>();
		anim.updateMode = AnimatorUpdateMode.Fixed;
		if (WeaponIndicator != null){
				WeaponIndicator.GetComponent<SpriteRenderer>().materials[0].SetFloat("_WeaponID",0);
			}
    }

    // Update is called once per frame
    void Update()

    {
		ChargeSlider.value = Charge;
			//направляет трос шокера на цель
			foreach (GameObject TazerThread in TazerThreads){
			//Debug.Log("direction set");
			TazerThread.GetComponent<MeshRenderer>().materials[0].SetVector("_ThreadDir",hitPosition - TazerThread.transform.position + TazerThread.transform.right*ThreadSpread);
			}
		
		if (slot1.WasPressedThisFrame() == true){//механика переключения между оружиями для удобства демонстрации
			if (!anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Ready") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Prep")){
			CurrentWeap = WeaponType.Torch;
			if (WeaponIndicator != null){
				WeaponIndicator.GetComponent<SpriteRenderer>().materials[0].SetFloat("_WeaponID",0);
			}
			Swap();
			} else {
				anim.SetTrigger("Off");
				Activated = false;
			}				
		}
		if (slot2.WasPressedThisFrame() == true){
			if (!anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Ready") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Prep")){
			CurrentWeap = WeaponType.Tazer;
			if (WeaponIndicator != null){
				WeaponIndicator.GetComponent<SpriteRenderer>().materials[0].SetFloat("_WeaponID",1);
			}
			Swap();
			} else{
				anim.SetTrigger("Off");
				Activated = false;
			}
		}
		if (slot3.WasPressedThisFrame() == true){
			if (!anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Ready") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Prep")){
			CurrentWeap = WeaponType.Dice;
			if (WeaponIndicator != null){
				WeaponIndicator.GetComponent<SpriteRenderer>().materials[0].SetFloat("_WeaponID",2);
			}
			Swap();
			} else{
				anim.SetTrigger("Off");
				this.gameObject.GetComponent<LoadedDice>().DiceVisible = false;
				Activated = false;
			}
		}
		if (slot4.WasPressedThisFrame() == true){
			if (!anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Ready") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Prep")){
			CurrentWeap = WeaponType.Whiskey;
			if (WeaponIndicator != null){
				WeaponIndicator.GetComponent<SpriteRenderer>().materials[0].SetFloat("_WeaponID",3);
			}
			Swap();
			} else{
				anim.SetTrigger("Off");
				Activated = false;
			}
		}
		
		
        if(fire2.WasPressedThisFrame() == true && !hands.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack")){//достать/убрать оружие
			Activated = !Activated;
			if (Activated == true){
				hands.GetComponent<Animator>().SetTrigger("On");
			}
			else {
				hands.GetComponent<Animator>().SetTrigger("Off");
			}
		}
		if (CurrentWeap == WeaponType.Dice){
			CanCharge = true;
		} else {
			CanCharge = false;
		}
		
		if (fire1.WasPressedThisFrame() && CanCharge == true){
			if (anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Ready")){
			anim.SetBool("IsCharging",true);//переход на состояние зарядки
			NeedsInit = true;
			}

		}
		
		if(fire1.IsPressed() && CanCharge == true){
			if (anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Charge")){
				Charge =  (Charge + ChargeSpeed * Time.deltaTime)%1f;//зарядка
				anim.SetFloat("ChargeLevel",Charge);
				anim.SetBool("IsCharging",true);
				//if (Charge >= 1f){
					//Charge = 1f;
				//}
			} else {
				//anim.SetBool("IsCharging",false);
			}
		}
		if (CanCharge == true && anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Ready") && fire1.WasReleasedThisFrame()){
			//anim.SetTrigger("ChargeSkip");//переход на состояние атаки по отпусканию кнопки
			//Debug.Log("dice thrown " + Charge);
		}

		if (CanCharge == true && anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Charge") && !fire1.IsPressed()){
			//anim.SetTrigger("ChargeEnd");//переход на состояние атаки по отпусканию кнопки
			if (NeedsInit == true){//эти действия нужно сделать только на 1 кадре
				anim.SetBool("IsCharging",false);
				//this.gameObject.GetComponent<LoadedDice>().InitializeDice();
				this.gameObject.GetComponent<LoadedDice>().DiceVisible = true;
				NeedsInit = false;
			}
			//Debug.Log("dice thrown " + Charge);
		}
		if (!anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Charge") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack")){
			anim.SetFloat("ChargeLevel",0f);//сбросить зарядку по окончанию анимации
			Charge = 0f;
		}
		
		
		if(!CanCharge && fire1.WasPressedThisFrame() == true && Activated == true && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Attack") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Prep") && !anim.GetCurrentAnimatorStateInfo(0).IsName(CurrentWeap.ToString()+"Off")){
			hands.GetComponent<Animator>().SetTrigger("Attack");
		}

    }
	void Swap()//переключение между оружиями
	{	anim.CrossFade(CurrentWeap.ToString()+"Prep",0.1f,0);//переход на стэйт локальной нод группы нужного оружия
		anim.CrossFade(CurrentWeap.ToString()+"PrepFX",0.1f,1);
		Activated = true;
	}
	public void SplashWhiskey(){
		if (WhiskeyEmitter != null)
		WhiskeyEmitter.GetComponent<ParticleSystem>().Play();
	}
	public void CauseHit(){
		RaycastHit hit;
		if (Physics.SphereCast(this.transform.position + this.transform.forward*-0.5f +Vector3.up*0.2f,0.5f,this.transform.forward,out hit,ScanDist,targets) == true){
			//Debug.Log("Activated");
			if (hit.transform.gameObject.GetComponent<HitReaction>() != null){
					Vector3 right = AttackOrigin.transform.right;
					Vector3 up = AttackOrigin.transform.up;
					Vector3 forward = AttackOrigin.transform.forward;
					AttackOrigin.transform.position = hit.point;
					hitPosition = hit.point;
					hit.transform.GetComponent<HitReaction>().AttackType = CurrentWeap;
					hit.transform.GetComponent<HitReaction>().SetupHitEffects(AttackOrigin.transform.worldToLocalMatrix);
					


			}
		}
	}
}
