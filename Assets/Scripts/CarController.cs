using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class CarController : MonoBehaviour {

    [SerializeField]
    private WheelJoint2D frontTire, backTire;

    [SerializeField]
    private float speed;

    private float movement, moveSpeed, fuel = 1, fuelConsumption = 0.1f;
    public float Fuel { get => fuel; set { fuel = value; } }

    public bool moveStop = false;
    public Vector3 StartPos { get; set; }

    // UDP Gesture Receiver Variables
    private Thread receiveThread;
    private UdpClient client;
    private volatile int gestureCommand = 0; // 1 = Gas, -1 = Brake, 0 = Idle

    private void Start() {
        // Start listening to Python on port 5065 in the background
        receiveThread = new Thread(new ThreadStart(ReceiveGestureData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveGestureData() {
        client = new UdpClient(5065);
        while (true) {
            try {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data).Trim();
                int.TryParse(text, out gestureCommand);
            }
            catch { }
        }
    }

    private void OnDestroy() {
        if (client != null) client.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }

    private void Update() {
        // 1. Check UI Pedals, Python UDP Gesture, OR Mouse Right-Click (for easy testing)
        bool gasActive = GameManager.Instance.GasBtnPressed 
                         || gestureCommand > 0 
                         || Input.GetKey(KeyCode.RightArrow) 
                         || Input.GetMouseButton(1); // Right-click anywhere = Gas

        // 2. Check UI Brake, Python UDP Gesture, OR Mouse Middle-Click
        bool brakeActive = GameManager.Instance.BrakeBtnPressed 
                           || gestureCommand < 0 
                           || Input.GetKey(KeyCode.LeftArrow) 
                           || Input.GetMouseButton(2); // Middle-click = Brake

        if(gasActive) {
            movement += 0.009f;
            if(movement > 1f)
                movement = 1f;
        }
        else if(brakeActive) {
            movement -= 0.009f;
            if(movement < -1f)
                movement = -1f;
        }
        else {
            movement = 0;
        }
        
        moveSpeed = movement * speed;
        GameManager.Instance.FuelConsume(); // Updates fuel consumption[cite: 21]
    }

    private void FixedUpdate() {
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, StartPos.x, transform.position.x), transform.position.y);

        if(moveSpeed.Equals(0) || fuel <= 0) { // Stops car if no input or fuel[cite: 21]
            frontTire.useMotor = false;
            backTire.useMotor = false;
        }
        else {
            frontTire.useMotor = true;
            backTire.useMotor = true;
            JointMotor2D motor = new JointMotor2D();
            motor.motorSpeed = moveSpeed;
            motor.maxMotorTorque = 10000;
            frontTire.motor = motor;
            backTire.motor = motor;
        }

        if(GameManager.Instance.isDie && moveStop) { // Halts vehicle velocity upon game over[cite: 21]
            GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        }

        fuel -= fuelConsumption * Mathf.Abs(movement) * Time.fixedDeltaTime; // Continues consuming fuel based on movement[cite: 21]
    }
}
