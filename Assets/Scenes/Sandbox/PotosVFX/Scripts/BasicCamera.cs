using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;//теперь использует новую систему ввода

public class BasicCamera : MonoBehaviour {
	public Transform cam;
	public Transform player;
	public float SpeedX = 10f;
	public float SpeedY = 10f;
	public float UpperLimit = 90f;
	public float LowerLimit = -90f;
	public Vector3 UpAxis = new Vector3(0f,1f,0f);
	public float X = 0f;
	public float Y = 0f;
	public Vector3 Offset = new Vector3(0f,2f,-4f);
	private Vector3 center;
	public Quaternion CameraRotation;
	public LayerMask mask;
	public Texture2D crosshair;
	private int centerX;
    private int centerY;
	
	public Vector3 Right;
	public Vector3 flatForward;
	public Vector3 Forward;
	public InputAction H;
	public InputAction V;
	// Use this for initialization
	void Start () {
	X = cam.rotation.eulerAngles.x;//better to have the Y up at the start
	centerX = Screen.width / 2;
	centerY = Screen.height / 2;
	Y = cam.rotation.eulerAngles.y;
	H.Enable();
	V.Enable();
	//cam.parent = null;
	}
	
	// Update is called once per frame
	void Update () {
		

		

	}
	void FixedUpdate(){
		X = Mathf.Clamp(X - V.ReadValue<float>()*SpeedY,LowerLimit,UpperLimit);
		Y = Y + H.ReadValue<float>()*SpeedX;
		//CameraRotation = Quaternion.AngleAxis(Y,UpAxis)*Quaternion.Euler(X,0f,0f);
		CameraRotation = Quaternion.AngleAxis(Y,UpAxis)*Quaternion.AngleAxis(X,new Vector3(1f,0f,0f));
		//Debug.DrawRay(cam.position,Vector3.Cross(UpAxis,cam.forward)*100f,Color.green);
		Right =  CameraRotation*new Vector3 (1f,0f,0f);
		Forward = CameraRotation*new Vector3 (0f,0f,1f);
		flatForward = Vector3.Cross(Right,UpAxis);
	}
	void LateUpdate (){
	
		cam.rotation = CameraRotation;
		center = player.position;

		Vector3 VerticalOffset = center + UpAxis*Offset.y;
		Vector3 RotatedOffset = CameraRotation*new Vector3(Offset.x,0f,Offset.z);
		cam.position = VerticalOffset + RotatedOffset;
		RaycastHit wall = new RaycastHit();
		if (Physics.Raycast(VerticalOffset,cam.position - VerticalOffset,out wall,(cam.position - VerticalOffset).magnitude,mask)){
			cam.position = wall.point + (VerticalOffset-cam.position).normalized * 0.1f;
		}
	}
	     void OnGUI()
     {    
		if (crosshair != null){
         GUI.DrawTexture(new Rect(centerX, centerY, 10, 10), crosshair);
		}
	 }
	 
	 Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal) {
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
	}
}
