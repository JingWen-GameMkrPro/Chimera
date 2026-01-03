using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager: MonoBehaviour
{
    [Header("References")]
    public Transform Player;
    public Transform PlayerMesh;
    public Transform PlayerOrientation;

    public float rotationSpeed;

    private void Update()
    {
        var viewDir = (Player.position - new Vector3(transform.position.x, Player.position.y, transform.position.z));
        PlayerOrientation.forward = viewDir.normalized;

        var x = Keyboard.current.aKey.isPressed ? -1 : (Keyboard.current.dKey.isPressed ? 1 : 0);
        var y = Keyboard.current.wKey.isPressed ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0);
        var inputDir = PlayerOrientation.forward * y + PlayerOrientation.right * x;
        if (inputDir != Vector3.zero)
        {
           PlayerMesh.forward = Vector3.Slerp(PlayerMesh.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
        }
    }

}
