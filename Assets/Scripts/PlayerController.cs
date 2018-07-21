using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Controller2D _controller;
    public float MoveSpeed = 1.0f;
    public Vector3 Velocity;

    public float Gravity = -2.0f;
    public float JumpSpeed = 8.0f;

    // Use this for initialization
    void Start() {
        _controller = GetComponent<Controller2D>();
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKey(KeyCode.D)) {
            Velocity.x = MoveSpeed * Time.deltaTime;
        } else if (Input.GetKey(KeyCode.A)) {
            Velocity.x = -MoveSpeed * Time.deltaTime;
        } else {
            Velocity.x = 0.0f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && _controller.IsGrounded()) {
            Velocity.y = JumpSpeed;
        }

        Velocity.y += Gravity * Time.deltaTime;
//		Velocity.y = -1.0f * Time.deltaTime;

        _controller.Move(Velocity);

        if (_controller.IsGrounded()) {
            Velocity.y = 0.0f;
        }
    }
}