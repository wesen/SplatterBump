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

    public float LandingDuration = 1.0f;
    public float FallDurationForLanding = 1.0f;

    private SpriteRenderer _sr;

    private enum State {
        IDLE,
        MOVING,
        RUNNING,
        JUMP,
        LANDING,
        AGAINST_WALL,
        CROUCHING,
        FALLING,
    }

    private float _landingStartTime;
    private float _fallingStartTime;
    private bool _isFirstJump = false;

    private State _state;
    private State _previousState;

    // Use this for initialization
    void Start() {
        _sr = GetComponent<SpriteRenderer>();
        _controller = GetComponent<Controller2D>();
        _state = State.IDLE;
        _previousState = State.IDLE;
    }

    // Update is called once per frame
    void Update() {
        // handle jumping / landing before moving, since we would like to switch to moving immediately upon landing
        if (_controller.IsGrounded()) {
            on_Grounded();
        } else {
            if (Velocity.y < 0.0f) {
                on_Floating();
            }
        }

        if (Input.GetKey(KeyCode.D)) {
            on_Move(1.0f);
        } else if (Input.GetKey(KeyCode.A)) {
            on_Move(-1.0f);
        } else {
            on_Idle();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            on_Jump();
        }

        _setAnimation();

        Velocity.y += Gravity * Time.deltaTime;
        _controller.Move(Velocity);

        if (_previousState != _state) {
            Debug.Log(_previousState + " -> " + _state);
        }

        _previousState = _state;
    }

    private void _setAnimation() {
        switch (_state) {
            case State.IDLE:
                _sr.color = Color.black;
                break;
            case State.MOVING:
                _sr.color = Color.blue;
                break;
            case State.RUNNING:
                _sr.color = Color.red;
                break;

            case State.JUMP:
                if (_isFirstJump) {
                    _sr.color = Color.white;
                } else {
                    _sr.color = Color.grey;
                }

                break;

            case State.LANDING:
                _sr.color = Color.green;
                break;
            case State.AGAINST_WALL:
                _sr.color = Color.cyan;
                break;
            case State.CROUCHING:
                _sr.color = Color.magenta;
                break;

            case State.FALLING:
                _sr.color = Color.yellow;
                break;
        }
    }

    private void on_Idle() {
        switch (_state) {
            case State.IDLE:
                break;
            case State.MOVING:
            case State.RUNNING:
                _state = State.IDLE;
                break;

            case State.JUMP:
                break;

            case State.LANDING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
            case State.FALLING:
                break;
        }

        Velocity.x = 0.0f;
    }

    private void on_Move(float direction) {
        switch (_state) {
            case State.IDLE:
                _state = State.MOVING;
                Velocity.x = direction * MoveSpeed * Time.deltaTime;
                break;
            case State.MOVING:
            case State.RUNNING:
            case State.JUMP:
                Velocity.x = direction * MoveSpeed * Time.deltaTime;
                break;

            case State.LANDING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
            case State.FALLING:
                break;
        }
    }

    private void on_Grounded() {
        switch (_state) {
            case State.IDLE:
            case State.MOVING:
            case State.RUNNING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
                break;

            case State.LANDING:
                if (Time.time - _landingStartTime > LandingDuration) {
                    _state = State.IDLE;
                }

                break;

            case State.JUMP:
                _landingStartTime = Time.time;
                _isFirstJump = false;
                break;

            case State.FALLING:
                Debug.Log("FallingDuration: " + (Time.time - _fallingStartTime));
                if (Time.time - _fallingStartTime > FallDurationForLanding) {
                    _state = State.LANDING;
                    _landingStartTime = Time.time;
                } else {
                    _state = State.IDLE;
                }

                _isFirstJump = false;
                break;
        }

        Velocity.y = 0.0f;
    }

    private void on_Jump() {
        switch (_state) {
            case State.IDLE:
            case State.MOVING:
            case State.RUNNING:
            case State.LANDING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
                _state = State.JUMP;
                _isFirstJump = true;
                Velocity.y = JumpSpeed;
                break;

            case State.JUMP:
            case State.FALLING:
                if (_isFirstJump) {
                    Velocity.y = JumpSpeed;
                    _isFirstJump = false;
                    _state = State.JUMP;
                }

                break;
        }
    }

    private void on_Floating() {
        switch (_state) {
            case State.IDLE:
            case State.MOVING:
            case State.RUNNING:
            case State.LANDING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
            case State.JUMP:
            case State.FALLING:
                _state = State.FALLING;
                _fallingStartTime = Time.time;
                break;
        }
    }
}