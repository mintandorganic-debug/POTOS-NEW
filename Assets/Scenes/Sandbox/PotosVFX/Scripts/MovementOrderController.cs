using UnityEngine;
using UnityEngine.InputSystem;//теперь использует новую систему ввода
using System.Collections;
using System.Collections.Generic;

public class MovementOrderController : MonoBehaviour {
	public float WalkSpeed = 2f;
	public float RunSpeed = 5f;
	public float JumpSpeed = 2f;
	public float FlyingSpeed = 10f;
	public float JumpHeight = 2f;
	public Transform cam;
	private Rigidbody rb;
	private Transform me;
	private CapsuleCollider coll;
	public bool IsGrounded = false;
	public bool IsSuspended = false;
	public bool IsTouched = false;
	public bool _IsTouched = false;
	public bool IsAiming = false;
	public bool IsFlying = false;
	//private bool _IsAiming = false;
	public bool IsNearStep = false;
	public bool _IsNearStep = false;
	private Vector3 GroundNormal = new Vector3(0f,1f,0f);
	private Vector3 moveDir;
	private Vector3 moveDirFlat;
	private Vector3 moveDirFlying;
	private Vector3 refDir;
	private Vector3 prevDir;
	private Vector3 lookDir;
	public Vector3 UpAxis = new Vector3(0f,1f,0f);
	public LayerMask envMask;
	public float H = 0f;
	public float V = 0f;
	public List<Order> MoveOrderQueue = new List<Order>();
	public Order MoveOrder = new Order();
	private Vector3 FakeGravity = new Vector3();
	public AnimationCurve dodgeCurve;//curve for dodge move with recovery time at the end
	public AnimationCurve jumpCurve;
	private float RotAngle;
	private float RotAngleAlt;
	private Vector3 prevMoveDir;
	
	private float StepVelocity;
	
	private float SpeedMult = 0f;
	//private float AccelStep = 0.5f;
	
	private float BaseGravity = 9.81f;
	private float FlyingGravity = 0f;
	private float ActiveGravity = 9.81f;
	
	public InputAction horizontal;
	public InputAction vertical;

	public InputAction jump;
	public InputAction fire1;
	public InputAction fire2;
	public InputAction fire3;
	
	
	// Use this for initialization
	void Start () {

		horizontal.Enable();//не работает без этого
		vertical.Enable();
		jump.Enable();
		fire1.Enable();
		fire2.Enable();
		fire3.Enable();
		
		me = this.transform;
		rb = this.gameObject.GetComponent<Rigidbody>();
		coll = this.gameObject.GetComponent<CapsuleCollider>();
		//Vector3 right = me.right;
		H = horizontal.ReadValue<float>();
		V = vertical.ReadValue<float>();
		prevDir = me.forward;
		prevMoveDir = me.forward;
		moveDirFlat = me.forward;
		lookDir = me.forward;
	}
	
	// Update is called once per frame
	void Update () {

		//Vector3 frontRef = me.GetComponent<BasicCamera>().flatForward;
		H = horizontal.ReadValue<float>();
		V = vertical.ReadValue<float>();
		


		//Debug.DrawRay(me.position,moveDir*100f,Color.blue);
		Vector3 front = Vector3.Cross(me.GetComponent<BasicCamera>().Right,GroundNormal).normalized;
		//Vector3 right = Vector3.Cross(GroundNormal,me.GetComponent<BasicCamera>().flatForward).normalized;

			
		//if (Input.GetButtonDown("Backstep") == true){
			//Order backstep = new Order();
			//float duration = 0.5f;
			//backstep.timer = duration;
			//backstep.duration = duration;
			//backstep.name = "Backstep";
			//backstep.moveVector = -front*7f/duration;
			//backstep.composition = Order.Composition.replace;
			//backstep.priority = 1;//should be more than 0
			//backstep.curve = dodgeCurve;
			//MoveOrderQueue.Add(backstep);//place movement order to queue
		//}
		
		//if (Input.GetButtonDown("Flying") == true){
			//IsFlying = !IsFlying;
		//}
		
		if (IsFlying == true){
			ActiveGravity = FlyingGravity;
		} else {
			ActiveGravity = BaseGravity;
		}

		
		if (jump.WasPressedThisFrame() == true && IsGrounded == true || jump.WasPressedThisFrame() == true && IsSuspended == true){
			Order jump = new Order();
			float duration = 1f;
			jump.timer = duration;
			jump.duration = duration;
			jump.name = "Jump";
			jump.moveVector = new Vector3(0f,1f,0f)*Mathf.Sqrt(2f*9.81f*JumpHeight);
			jump.composition = Order.Composition.additive;
			MoveOrderQueue.Add(jump);//place movement order to queue
		}
		if (fire3.WasPressedThisFrame() == true){
			IsAiming = !IsAiming;
		}
		

		
	
	}
	
	void FixedUpdate () {
		IsGrounded = GroundCheckAlt();
		
		Vector3 front = Vector3.Cross(me.GetComponent<BasicCamera>().Right,GroundNormal).normalized;
		Vector3 right = Vector3.Cross(GroundNormal,me.GetComponent<BasicCamera>().flatForward).normalized;
		//if (new Vector2(H,V).magnitude != 0f){
			//SpeedMult += AccelStep*Time.fixedDeltaTime;
		//} else {
			//SpeedMult = AccelStep*Time.fixedDeltaTime;
		//}
		SpeedMult =1f;
		moveDir = (right*H + front*V).normalized*SpeedMult;
		moveDirFlat = (me.GetComponent<BasicCamera>().Right*H + me.GetComponent<BasicCamera>().flatForward*V).normalized;
		moveDirFlying = (me.GetComponent<BasicCamera>().Right*H + me.GetComponent<BasicCamera>().Forward*V).normalized; 
		
		Vector3 Obstacle = ScanObstacle(me.position,moveDir.normalized,UpAxis,coll.radius*1.2f+rb.linearVelocity.magnitude*0.1f,20,coll.height*1.2f,0.5f,envMask);
		Vector3 scanOrigin = me.position;
		
		
		if (Obstacle != new Vector3(0.123456789f,0f,0f) && Vector3.Distance(Obstacle,scanOrigin) < coll.height*2f){
			IsNearStep = true;
		} else {
			IsNearStep = false;
		}
		if (IsGrounded == false){
			FakeGravity += -UpAxis*ActiveGravity*Time.fixedDeltaTime;
		} else {
			FakeGravity = Vector3.zero;
		}
		
		
		MoveOrder = new Order();
		MoveOrder.timer = Time.fixedDeltaTime;
		MoveOrder.name = "Character Movement";
		if (IsFlying == false){
			if (IsGrounded == true){
				MoveOrder.moveVector = moveDir * WalkSpeed;
			} else {
				MoveOrder.moveVector = moveDir * JumpSpeed;
			}
		} else {
			MoveOrder.moveVector = moveDirFlying * FlyingSpeed;
		}
		MoveOrder.composition = Order.Composition.additive;
		MoveOrderQueue.Add(MoveOrder);//place movement order to queue
		
		if (IsTouched != _IsTouched && IsTouched == true){//stop jump on hitting something
			List<Order> ToRemove = new List<Order>();
			foreach (Order order in MoveOrderQueue) {
				if (order.name == "Jump"){
					ToRemove.Add(order);
				}
			}
			foreach (Order _order in ToRemove) {
				MoveOrderQueue.Remove(_order);
			}
		}
		_IsTouched = IsTouched;
		
		if (IsNearStep != _IsNearStep && IsNearStep == true){
			StepVelocity = Mathf.Sqrt(2f*9.81f*Vector3.Distance(Obstacle,scanOrigin));
			FakeGravity = Vector3.zero;//reset gravity so the "climbing" can be initiated in air
			RemoveOrder("Jump");
		}
		_IsNearStep = IsNearStep;
		if (IsNearStep == true){
			Order StepCorrection = new Order();
			StepCorrection.timer = Time.fixedDeltaTime;
			StepCorrection.duration = Time.fixedDeltaTime;
			StepCorrection.name = "Go over obstacle";
			StepCorrection.moveVector = UpAxis * StepVelocity;
			StepCorrection.composition = Order.Composition.additive;
			MoveOrderQueue.Add(StepCorrection);
		}
		
			if (MoveOrderQueue.Count != 0f){
			Vector3 SummedForces = new Vector3(0f,0f,0f);
			Order Override = new Order();
			Vector3 OverrideForce  = new Vector3(0f,0f,0f);
			List<Order> ToRemove = new List<Order>();
			bool Replace = false;
			foreach (Order order in MoveOrderQueue) {
				if (order.composition == Order.Composition.additive){
					SummedForces += order.moveVector;
					order.timer -= Time.fixedDeltaTime;
					if (order.timer <= 0f){
						ToRemove.Add(order);
					}
				}
				if (order.composition == Order.Composition.replace){
					Replace = true;
					float CurveMult = order.curve.Evaluate(1f-(order.timer/order.duration));
					if (order.priority > Override.priority){
						Override = order;
						OverrideForce = order.moveVector*CurveMult;
					}
					order.timer -= Time.fixedDeltaTime;
					if (order.timer <= 0f){
						ToRemove.Add(order);
					}
				}
			}
			foreach (Order _order in ToRemove) {
				MoveOrderQueue.Remove(_order);
			}
			if (Replace == true){
				SummedForces = OverrideForce;
			}
			rb.AddForce((SummedForces - rb.linearVelocity)+FakeGravity,ForceMode.VelocityChange);//execute queued movement
		
		}
		
		//calculating rotation angle for aiming camera mode
		if (IsAiming == true){
			lookDir = Vector3.Cross(me.GetComponent<BasicCamera>().Right,UpAxis);
			//directions come from purely virtual axes; no dependency loop should be in effect
		} else {
			if (H != 0f || V != 0f){//if the horizontal or vertical inputs present, overwrite look direction
				lookDir = (me.GetComponent<BasicCamera>().Right*H + me.GetComponent<BasicCamera>().flatForward*V).normalized;
			} 
		}
		if (Vector3.Dot(lookDir,me.right) > Vector3.Dot(lookDir,-me.right)){
			RotAngle = Vector3.Angle(prevDir,lookDir);
		} else {
			RotAngle = Vector3.Angle(prevDir,lookDir);
			RotAngle = RotAngle*-1f;
		}
		Quaternion aimRotation = Quaternion.AngleAxis(RotAngle,UpAxis);
		rb.rotation = Quaternion.RotateTowards(me.rotation,me.rotation*aimRotation,Mathf.Abs(RotAngle));
		prevDir = lookDir;
		RotAngleAlt = RotAngle;
		
	}
	void LateUpdate(){

	}
	
	bool GroundCheckAlt (){//constantly check distance to ground. Check spherecast to ground and capsule simultaneously
		RaycastHit ground = new RaycastHit();
		RaycastHit ground1 = new RaycastHit();
		bool CapsuleHit = false;
		bool RayHit = false;
		if (Physics.CheckCapsule(me.position + UpAxis*coll.radius,me.position + UpAxis*(coll.height - coll.radius),coll.radius*1.01f,envMask) == true){
			CapsuleHit = true;
			IsTouched = true;
		} else {
			IsTouched = false;
		}
		if (Physics.Raycast(me.position+UpAxis*coll.height,-UpAxis,out ground,100f,envMask) == true){
				GroundNormal = ground.normal;

				if (Physics.Raycast(me.position+UpAxis*coll.height,-UpAxis,out ground1,100f,envMask) == true){
					GroundNormal = ground1.normal;
					
				}
				if (Physics.CheckCapsule(me.position + UpAxis*coll.radius*0.9f,me.position + UpAxis*(coll.height - coll.radius*0.99f),coll.radius*0.99f,envMask) == true){
					RayHit = true;					
				}
				
				if (Vector3.Distance(me.position,ground.point) <= coll.radius*1f && CapsuleHit == false){
					IsSuspended = true;
				} else {
					IsSuspended = false;
				}
		} 
		

		if (CapsuleHit == true && RayHit == true){
			return true;
		} else {
			return false;
		}//if both are positive, we have ground detection

		}
		
		private void RemoveOrder(string name){//search and remove order from MoveOrderQueue
			List<Order> ToRemove = new List<Order>();
			foreach (Order order in MoveOrderQueue) {
				if (order.name == name){
						ToRemove.Add(order);
					}
				}
			foreach (Order _order in ToRemove) {
				MoveOrderQueue.Remove(_order);
			}
			
		}
		
		private Order GetOrder(string name){//search and remove order from MoveOrderQueue
			Order _order = new Order();
			_order.name = "Not found";//mark it in case it doesnt exist
			foreach (Order order in MoveOrderQueue) {
				if (order.name == name){
						_order = order;
					}
				}
			return _order;
		}
		
		Vector3 ScanObstacle(Vector3 origin, Vector3 forwardDir, Vector3 sweepDir, float scanLength, int NumSamples, float MaxHeight, float checkDot, LayerMask mask){
			//performs a raycast sweep to detect obstacle and determine where it ends
			Vector3 StepHeight = sweepDir.normalized * (MaxHeight/NumSamples);
			Vector3 ObstacleEnd = new Vector3(0.123456789f,0f,0f);//default set to strange value in order to be able to check against it
			Vector3 Offset = new Vector3();
			int count = 0;
			bool obstacleHit = false;
			bool _obstacleHit = false;
			RaycastHit hit1 = new RaycastHit();
			do {//rough scan
				Offset += StepHeight;
				obstacleHit = Physics.Raycast(origin + Offset,forwardDir,out hit1,scanLength,mask);
				count += 1;
				Debug.DrawRay(origin + Offset,forwardDir*scanLength,Color.red);
				if (obstacleHit != _obstacleHit && obstacleHit == false){
					break;
				}
				_obstacleHit = obstacleHit;
			} while (count < NumSamples);
			//initiate precise scan
			Vector3 PreciseStepHeight = StepHeight/NumSamples;
			Offset -= StepHeight;//go back one step
			count = 0;
			do {
				Offset += PreciseStepHeight;
				obstacleHit = Physics.Raycast(origin + Offset,forwardDir,out hit1,scanLength,mask);
				count += 1;
				Debug.DrawRay(origin + Offset,forwardDir*scanLength,Color.blue);
				if (obstacleHit != _obstacleHit && obstacleHit == false){
					ObstacleEnd = hit1.point;
					break;
				}
				_obstacleHit = obstacleHit;
			} while (count < NumSamples);
				
			if (ObstacleEnd != new Vector3(0.123456789f,0f,0f) && Vector3.Dot(-forwardDir,hit1.normal) >= checkDot){
				return ObstacleEnd;
			} else {
				return new Vector3(0.123456789f,0f,0f);
			}
	
		}
}
