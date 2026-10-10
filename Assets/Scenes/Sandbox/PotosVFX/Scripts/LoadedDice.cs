using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class LoadedDice : MonoBehaviour
//Механика поведения костей; разворачиваются нужной гранью вверх, переключаются между физикой и ключевыми кадрами на лету
{
	public GameObject DiePrefab;
	public Transform MainCamera;
	private List<GameObject> dice = new List<GameObject>();
	public List<GameObject> DiceControlBones = new List<GameObject>();
	public List<GameObject> UIDice = new List<GameObject>();//отображение кости в интерфейсе
	public int[] score = {1,2,3};
	public float DieRadius = 0.04f;
	//Rigidbody следует за анимированными костями
	public float FollowWeight = 1.0f;
	//"Доводка" физики костей до нужного результата
	public float TweakForce = 5.0f;
	public float GatherForce = 10f;
	//1 напротив 6, 2 напротив 5, 3 напротив 4
	//иными словами X-Y-Z- = 123, Z+Y+X+ = 456
	private Vector3 DirOne = new Vector3(-1.0f,0.0f,0.0f);
	private Vector3 DirTwo = new Vector3(0.0f,1.0f,0.0f);
	private Vector3 DirThree = new Vector3(0.0f,0.0f,-1.0f);
	private Vector3 DirFour= new Vector3(0.0f,0.0f,1.0f);
	private Vector3 DirFive = new Vector3(0.0f,-1.0f,0.0f);
	private Vector3 DirSix = new Vector3(1.0f,0.0f,0.0f);
	public Transform Plate;//центр тарелки
	public float PlateRadius = 0.5f;//радиус тарелки
	public float CatchRadius = 3f;//радиус улавливания кубиков
	public float MinVelocity = 0.2f;//пороговое значение скорости, используемое в некоторых анимациях
	
	//эффекты появления и свечения кубиков
	public bool DiceVisible = false;
	private float DiceTimer = 0f;//таймер появления/исчезновения
	public float DiceAppearSpeed = 1.5f;//скорость возрастания таймера
	public float DiceDisappearSpeed = 0.5f;//скорость убывания таймера
	
	private Quaternion[] RotFrames = {Quaternion.identity,Quaternion.identity,Quaternion.identity};//сохранить "ключевой кадр" для дальнейшей интерполяции
	public float RotFrameRate = 0.1f;
	public float[] RotFrameTimers = {0.1f,0.1f,0.1f};
	
	public float ScalePulse = 0.15f;
	private float _ScalePulse;
	private bool[] DiceStop = {true,true,true};//проверка всех кубиков на остановку
	private bool _AllStopped;
	
	public float[] FlashTimers = {0f,0f,0f};//индивидуальные таймеры для вспышек
	public float FlashDuration = 0.33f;
	
	public float ForceVariation = 0.33f;//добавить вариацию силы броска
	public float[] ForceMultipliers = {1f,1f,1f};
	
	private Animator anim;
	
	public float VerticalLimit = 2f;//ограничение скорости при движении вверх
	
	public float ChargeMin = 0.75f;
	public float ChargeMax = 1.5f;//дополнительное увеличение силы от шкалы "заряда" (помимо анимации)
	public Transform DiceOrigin;
	
	//private float MaxVelocity = 0f;

	
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeDice();
		anim = this.gameObject.GetComponent<Animator>();
		anim.updateMode = AnimatorUpdateMode.Fixed;
    }

    //called once per simulation frame
    void FixedUpdate()
    {
		UIDiceVisibility();
        FollowAnimation();
		TweakScore();
		//KeepInside();
		LimitBounce();
		DoThingsWhenStopped();
		RotateUIDice();
    }
	public void InitializeDice()//генерировать объекты, если нужно, и поместить их на начальную точку анимации
		{
			//Debug.Log("initialize");
		if (dice.Count == 0){//физические кубики еще не существуют
			dice.Add(Instantiate(DiePrefab,DiceControlBones[0].transform.position, Quaternion.identity));
			dice.Add(Instantiate(DiePrefab,DiceControlBones[1].transform.position, Quaternion.identity));
			dice.Add(Instantiate(DiePrefab,DiceControlBones[2].transform.position, Quaternion.identity));
		} 
		foreach (GameObject d in dice){//обнулить скорость и перенести на начальную позицию
			int count = 0;
			d.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
			d.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
			d.GetComponent<Rigidbody>().isKinematic = true;
			d.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
			d.layer = LayerMask.NameToLayer("Ignore Raycast");
			d.GetComponent<Rigidbody>().position = DiceControlBones[count].transform.position;
			d.layer = LayerMask.NameToLayer("DiceSimulation");
			d.GetComponent<Collider>().enabled = true;
			d.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			d.GetComponent<Rigidbody>().isKinematic = false;
			d.GetComponent<Rigidbody>().rotation = DiceControlBones[count].transform.rotation;
			d.transform.rotation = Quaternion.identity;
			d.transform.GetChild(0).rotation = Quaternion.identity;
			ForceMultipliers[count] = 1f +Random.Range(0f,ForceVariation);
			count += 1;
		}
		}
	void FollowAnimation(){//следование за кейфреймами с помощью физики
		int count = 0;
		foreach (GameObject d in dice){
			if (anim.GetCurrentAnimatorStateInfo(0).IsName("DiceCharge")){//используем другую методику, если кубики в руке в момент "заряда"
				d.GetComponent<Rigidbody>().position = DiceControlBones[count].transform.position;
				d.GetComponent<Rigidbody>().rotation = DiceControlBones[count].transform.rotation;
			} else {
				float ChargeMult = Mathf.Lerp(ChargeMin,ChargeMax,this.GetComponent<TorchDemo>().Charge);
				Vector3 Local =  DiceOrigin.InverseTransformPoint(DiceControlBones[count].transform.position);
				Local = Vector3.Scale(Local,new Vector3(1f,0.9f,ChargeMult*ForceMultipliers[count]));//управление траекторией кубиков 
				Vector3 ScaledPos = DiceOrigin.TransformPoint(Local);
				
				Vector3 FollowCenter = (ScaledPos - d.transform.position)/Time.fixedDeltaTime-d.GetComponent<Rigidbody>().linearVelocity;
				//использую AddForceAtPosition чтобы усилить крутящий момент
				d.GetComponent<Rigidbody>().AddForceAtPosition(FollowCenter*FollowWeight,d.transform.position+Vector3.up*DieRadius*0.5f,ForceMode.VelocityChange);
				float CheckOutside = Vector3.Dot( Vector3.Scale((d.transform.position - Plate.position),new Vector3(1f,0f,1f)),FollowCenter.normalized);
				if ( Vector3.Scale((d.transform.position - Plate.position),new Vector3(1f,0f,1f)).magnitude >= PlateRadius){
					d.layer = LayerMask.NameToLayer("Ignore Raycast");//пока кубик снаружи, он проходит сквозь коллайдер
					d.GetComponent<Collider>().enabled = false;
				} else {
					d.layer = LayerMask.NameToLayer("DiceSimulation");
					d.GetComponent<Collider>().enabled = true;
				}
			}
			
			count += 1;
		}
		//LimitBounce();
	}
	
	void TweakScore(){//подтасовка результатов
		int count = 0;
		foreach (GameObject d in dice){
			Vector3 LookUp = Vector3.up*DieRadius;
			Vector3 RequiredFace = Vector3.zero;
			switch (score[count]){
				case 1:
				RequiredFace = (d.transform.GetChild(0).rotation*DirOne)*DieRadius;
				break;
				case 2:
				RequiredFace = (d.transform.GetChild(0).rotation*DirTwo)*DieRadius;
				break;
				case 3:
				RequiredFace = (d.transform.GetChild(0).rotation*DirThree)*DieRadius;
				break;
				case 4:
				RequiredFace = (d.transform.GetChild(0).rotation*DirFour)*DieRadius;
				break;
				case 5:
				RequiredFace = (d.transform.GetChild(0).rotation*DirFive)*DieRadius;
				break;
				case 6:
				RequiredFace = (d.transform.GetChild(0).rotation*DirSix)*DieRadius;
				break;
			}
			Quaternion TargetRot = Quaternion.FromToRotation(RequiredFace.normalized,Vector3.up);
			float SnapOrNot = 1f - Mathf.Clamp(d.GetComponent<Rigidbody>().linearVelocity.magnitude/MinVelocity,0f,1f);
			//два приложения силы, которые "тянут" в противоположные стороны
			//d.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up*d.GetComponent<Rigidbody>().mass*TweakForce,d.transform.position+RequiredFace*DieRadius*0.5f,ForceMode.Impulse);
			//d.GetComponent<Rigidbody>().AddForceAtPosition(-Vector3.up*d.GetComponent<Rigidbody>().mass*TweakForce,d.transform.position+RequiredFace*DieRadius*-0.5f,ForceMode.Impulse);
			
			//этот алгоритм подкручивания не влияет на физику
			d.transform.GetChild(0).rotation = Quaternion.Slerp(d.transform.GetChild(0).rotation,TargetRot*d.transform.GetChild(0).rotation,SnapOrNot*TweakForce);
			count += 1;
		}
	}
	void KeepInside(){//предотвращает вылет за пределы тарелки
		int count = 0;
		foreach (GameObject d in dice){

			Vector3 DirToCenter = Vector3.Scale( (Plate.position-d.transform.position),new Vector3(1f,0f,1f)  );
			Vector3 ToCenter = DirToCenter.normalized*(1.0f - Mathf.Clamp((DirToCenter.magnitude-PlateRadius)/CatchRadius,0f,1f))*GatherForce*ForceMultipliers[count];
			if (DirToCenter.magnitude > PlateRadius){
				d.GetComponent<Rigidbody>().AddForce(ToCenter,ForceMode.Force);
				//Debug.Log("attract to center");
			}
			count += 1;
		}
	}
	bool AllStopped(){//проверить скорость (что она ниже минимальной) и состояние аниматора (выбраны кубики) для всех кубиков
		int count = 0;
		foreach (GameObject d in dice){
			if (d.gameObject.GetComponent<Rigidbody>().linearVelocity.magnitude <= MinVelocity){
				DiceStop[count] = true;
			} else {
				DiceStop[count] = false;
			}
			}
			count += 1;
		if (DiceStop[0] == true && DiceStop[1] == true && DiceStop[2] == true){
			if (anim.GetCurrentAnimatorStateInfo(0).IsName("DiceAttack")|| anim.GetCurrentAnimatorStateInfo(0).IsName("DiceReady")){
				return true;
			} else {
				return false;
			}
		} else {
			return false;
		}
	}
	void DoThingsWhenStopped(){//когда кубики остановились, управляем их вспышкой и, возможно, проверяем комбинации очков
		if (_AllStopped != AllStopped() && AllStopped() == true){//на первом фрейме, когда все остановились, проводим инициализацию
		//Debug.Log("all stopped");
		int _count = 0;
		foreach (GameObject d in dice){
			FlashTimers[_count] = FlashDuration*_count + FlashDuration;
			_count+=1;
		}
		
		}
		int count = 0;
		foreach (GameObject d in dice){
			if (AllStopped() == true){
				FlashTimers[count] = Mathf.Clamp(FlashTimers[count] - Time.fixedDeltaTime,0f,(FlashDuration*count + FlashDuration));//подсвечивание кубиков по очереди
				UIDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_BrightnessMult",Mathf.Lerp(0.3f,1f,1f- Mathf.Clamp(FlashTimers[count]/FlashDuration,0f,1f) ));
				UIDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_BrightnessMult", (1f- Mathf.Clamp(FlashTimers[count]/FlashDuration,0f,1f))*(1f-FollowWeight) );
				UIDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_ScalePulse", ScalePulse*(1f- Mathf.Clamp(FlashTimers[count]/FlashDuration,0f,1f)) );
				UIDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_ScalePulse", ScalePulse*(1f- Mathf.Clamp(FlashTimers[count]/FlashDuration,0f,1f)) );
			} else {
				UIDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_BrightnessMult",0.3f);
				UIDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_BrightnessMult",0f);
				UIDice[count].GetComponent<MeshRenderer>().materials[0].SetFloat("_ScalePulse",0f);
				UIDice[count].GetComponent<ParticleSystemRenderer>().material.SetFloat("_ScalePulse",0f);
			}
			if (FlashTimers[count] == 0f){
				//RotFrameTimers[count] = 0f;//остановить интерполяцию вращения
			}
			count += 1;
		}
		_AllStopped = AllStopped();
	}
	void RotateUIDice(){//вращает копии кубиков в интерфейсе
		if (dice.Count != 0 && UIDice.Count != 0){
			TorchDemo.WeaponType CurrentWeap = this.GetComponent<TorchDemo>().CurrentWeap;
			int count = 0;
			Vector4 CamRight = new Vector4(MainCamera.right.x,MainCamera.right.y,MainCamera.right.z,0f);
			Vector4 CamUp = new Vector4(MainCamera.forward.x,MainCamera.forward.y,MainCamera.forward.z,0f)*-1f;
			Vector4 CamForward = new Vector4(MainCamera.up.x,MainCamera.up.y,MainCamera.up.z,0f);
			Matrix4x4 CameraAlign = new Matrix4x4();
			CameraAlign.SetColumn(0,CamRight);
			CameraAlign.SetColumn(1,CamUp);
			CameraAlign.SetColumn(2,CamForward);
			CameraAlign.SetColumn(3,new Vector4(0f,0f,0f,1f));
		
			
			foreach (GameObject d in dice){
				bool SetKeyframe = false;
				RotFrameTimers[count] -= Time.fixedDeltaTime;
				if (RotFrameTimers[count] <= 0f){
					SetKeyframe = true;
					RotFrameTimers[count] = RotFrameRate;
				}
				float SnapOrNot = 1f - Mathf.Clamp(d.GetComponent<Rigidbody>().linearVelocity.magnitude/MinVelocity,0f,1f);//интерполяция между исходным и выровненным вращением, также влияет на шейдерные эффекты
				
				Quaternion Rot = d.transform.GetChild(0).rotation;//использовать вращение пустышки вместо основного узла, из-за нового алгоритма подтасовки очков
				Quaternion DesiredRot = CameraAlign.rotation * Rot;
				
				//определить, какой стороной упал кубик
				Vector3 _x = d.transform.GetChild(0).right;
				Vector3 _y = d.transform.GetChild(0).up;
				Vector3 _z = d.transform.GetChild(0).forward;
				float axis = 0f;
				if (Mathf.Abs(_x.y)> Mathf.Abs(_y.y)){//определить, какая ось параллельнее оси Y мира и её направление
					if (Mathf.Abs(_x.y)> Mathf.Abs(_z.y)){
						axis = 1f*Mathf.Sign(_x.y);
						} else {
						axis = 3f*Mathf.Sign(_z.y);
					}
				} else {
					if (Mathf.Abs(_y.y) > Mathf.Abs(_z.y)){
							axis = 2f*Mathf.Sign(_y.y);
					} else {
						axis = 3f*Mathf.Sign(_z.y);
					}
				}
				
				Vector3 ChooseSide = Vector3.zero;
				switch (axis){
				case -3f:
					ChooseSide = new Vector3(90f,0f,0f);
				break;
				case -2f:
					ChooseSide = new Vector3(180f,0f,0f);
				break;
				case -1f:
					ChooseSide = new Vector3(0f,0f,-90f);
				break;
				case 1f:
					ChooseSide = new Vector3(0f,90f,90f);
				break;
				case 2f:
					ChooseSide = new Vector3(0f,-90f,0f);
				break;
				case 3f:
					ChooseSide = new Vector3(-90f,0f,0f);
				break;
				}
				Quaternion SnapRot = Quaternion.Euler(ChooseSide);//выровнять по заранее заданным направлениям 
				if (SetKeyframe == true){
					RotFrames[count] = SnapRot;
				}
				
				
				Quaternion _SnapRot = CameraAlign.rotation * RotFrames[count];
				Debug.DrawRay(UIDice[count].transform.position,_SnapRot*new Vector3(0f,0f,1f),Color.red);
				Debug.DrawRay(UIDice[count].transform.position,UIDice[count].transform.rotation*new Vector3 (0f,0f,1f),Color.green);
				Debug.DrawRay(UIDice[count].transform.position,_SnapRot*new Vector3(0f,1f,0f),Color.blue);
				Debug.DrawRay(UIDice[count].transform.position,UIDice[count].transform.rotation*new Vector3 (0f,1f,0f),Color.blue);
				//UIDice[count].transform.rotation = Quaternion.Slerp(UIDice[count].transform.rotation,_SnapRot,1f - RotFrameTimers[count]/RotFrameRate);//вращает кости "скачками"
				UIDice[count].transform.rotation = TwoAxisCompoundRotation(UIDice[count].transform.rotation,_SnapRot,1f - RotFrameTimers[count]/RotFrameRate);
				//UIDice[count].transform.rotation = _SnapRot;
				SetKeyframe = false;
				count += 1;
			}
			


		}
	}
	void UIDiceVisibility(){//эффекты появления кубиков
	if (DiceVisible == true){
				DiceTimer += Time.fixedDeltaTime*DiceAppearSpeed;
			}
		if (DiceVisible == false){
				DiceTimer -= Time.fixedDeltaTime*DiceDisappearSpeed;
			}

		DiceTimer = Mathf.Clamp(DiceTimer,0f,1f);
		foreach (GameObject d in UIDice){
			d.GetComponent<MeshRenderer>().materials[0].SetFloat("_ScaleMult",DiceTimer);
			d.GetComponent<ParticleSystemRenderer>().material.SetMatrix("_CubeMatrix",d.transform.worldToLocalMatrix);
			d.GetComponent<ParticleSystemRenderer>().material.SetFloat("_ScaleMult",DiceTimer);
			//d.GetComponent<ParticleSystemRenderer>().material.SetFloat("_BrightnessMult",-1f );
		}
	}
	//void UIDiceFlash(float _FlashTimer){//активирует вспышку
		//FlashTimer = _FlashTimer;
		//FlashDuration = _FlashTimer;
	//}
	void LimitBounce(){
		foreach (GameObject d in dice){
			Vector3 NewVel = d.GetComponent<Rigidbody>().linearVelocity;
			float LimitedVel = Mathf.Clamp(NewVel.y,0f,VerticalLimit);
			if (NewVel.y > 0f){//Ограничиваем отскок если кубик движется вверх
				d.GetComponent<Rigidbody>().linearVelocity = new Vector3(NewVel.x,Mathf.Lerp(LimitedVel,NewVel.y,FollowWeight),NewVel.z);
			if (d.GetComponent<Rigidbody>().linearVelocity.magnitude <= 0.01f)
				d.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
			}
		}
	}
	Quaternion TwoAxisCompoundRotation(Quaternion _from,Quaternion rot,float t){//вращение в сторону целевого кватерниона в "два приёма"
		Vector3 TargetForward =rot*new Vector3(0f,0f,1f);
		Vector3 TargetUp = rot*new Vector3(0f,1f,0f);
		
		Quaternion halfway = Quaternion.FromToRotation(_from*new Vector3(0f,0f,1f),TargetForward);
		
		Vector3 halfUp = Quaternion.Slerp(_from,halfway*_from,Mathf.Clamp(t/0.5f,0f,1f)) *new Vector3(0f,1f,0f);
		float targetAngle = Vector3.Angle(halfUp,TargetUp);
		targetAngle *= Mathf.Sign(Vector3.Dot(halfUp,TargetUp));
		Quaternion AxisRot = Quaternion.AngleAxis(targetAngle,TargetForward);
		Quaternion final = halfway*AxisRot;
		
		return final*_from;
		//return   Quaternion.AngleAxis(targetAngle * Mathf.Clamp((t-0.5f)/0.5f,0f,1f),TargetForward) * Quaternion.Slerp(_from,halfway*_from,Mathf.Clamp(t/0.5f,0f,1f));

	}
	
}
