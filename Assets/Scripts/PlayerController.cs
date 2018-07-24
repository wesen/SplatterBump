﻿using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Controller2D _controller;

//    private Controller2DPlus _controller;
    public float MoveSpeed = 1.0f;
    public Vector3 Velocity;

    public float Gravity = -2.0f;
    public float JumpSpeed = 8.0f;

    public float LandingDuration = 1.0f;
    public float FallDurationForLanding = 1.0f;

    public float JumpZone = 0.2f;

    private SpriteRenderer _sr;

    public bool Verbose = false;

    public enum State {
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

    // TODO: downwards slopes
    // TODO: slide on slopes
    // TODO: ice
    // TODO: allow jump before hitting ground
    // TODO: dashing
    // TODO: crouching
    // TODO: ledge grab
    // TODO: climbing
    // TODO: swimming
    // TODO: rope swinging
    // TODO: against walls
    // TODO: tweak physics?
    // TODO: particles on jump / walk
    // TODO: running / running stamina
    // TODO: dying animation

    // [X] transition from jump to fall
    // TODO: slide forward velocity from jump
    // TODO: Particle smoke when jumping
    // TODO: Sounds on actions
    // TODO: Sticking to walls

    private Animator _animator;

    private Recorder _recorder;
    [Range(0, 100)] public int ReplayFrame = 0;
    private Vector3 _lastPosition = new Vector3();
    private Vector3 _lastVelocity = new Vector3();

    private float _xScale;
    private bool _triggerAnimationNextFrame = true;

    // Use this for initialization
    void Start() {
        _sr = GetComponentInChildren<SpriteRenderer>();
//        _controller = GetComponent<Controller2DPlus>();
        _controller = GetComponent<Controller2D>();
        _recorder = GetComponent<Recorder>();
        _animator = GetComponent<Animator>();

        _xScale = transform.localScale.x;

        _animator.Play("Idle");

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

        Velocity.y += Gravity * Time.deltaTime;

        _recordOrReplayStep();

        _controller.Move(Velocity);

        if (Velocity.x < 0) {
            transform.localScale = new Vector2(-_xScale, transform.localScale.y);
        } else if (Velocity.x > 0) {
            transform.localScale = new Vector2(_xScale, transform.localScale.y);
        }

        if (_previousState != _state || !_recorder.IsRecording() || _triggerAnimationNextFrame) {
            _triggerAnimationNextFrame = false;
            _setAnimation();
            if (Verbose) {
                Debug.Log(_previousState + " -> " + _state);
            }
        }

        _previousState = _state;
    }

    private void _recordOrReplayStep() {
        if (_recorder.IsRecording()) {
            if ((_lastPosition - transform.position).magnitude > 0.001 ||
                (_lastVelocity - Velocity).magnitude > 0.001) {
                _lastPosition = transform.position;
                _lastVelocity = Velocity;

                _recorder.RecordFrame(transform.position, Velocity, _state, _previousState);
            }
        } else {
            Recorder.Frame frame = _recorder.GetFrame(ReplayFrame);
            transform.position = frame.Position;
            Velocity = frame.Velocity;
            _previousState = frame.PreviousPlayerState;
            _state = frame.PlayerState;
        }
    }

    private Vector3 _computeVelocity(float direction) {
        Vector3 ret = new Vector3(0, 0, 0);
        ret.x = direction * MoveSpeed * Time.deltaTime;
        ret.y += Velocity.y;
        return ret;
    }

    private void _setAnimation() {
        switch (_state) {
            default:
                break;
            case State.IDLE:
                _animator.Play("Idle");
                _sr.color = Color.white;
                break;
            case State.MOVING:
                _animator.Play("Walk");
                _sr.color = Color.white;
                break;
            case State.RUNNING:
                _sr.color = Color.red;
                break;

            case State.JUMP:
                _animator.Play("Jump");
                _sr.color = Color.white;

                if (_isFirstJump) {
                } else {
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
                if (_previousState == State.JUMP) {
                    _animator.Play("Apex");
                    _triggerAnimationNextFrame = true;
                } else {
                    _animator.Play("Fall");
                }

                _sr.color = Color.white;
                break;
        }
    }

    private void on_Idle() {
        switch (_state) {
            default:
                break;
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
            default:
                break;
            case State.IDLE:
                _state = State.MOVING;
                Velocity = _computeVelocity(direction);
                break;
            case State.MOVING:
            case State.RUNNING:
            case State.JUMP:
                Velocity = _computeVelocity(direction);
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
            default:
                break;
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
                if (Verbose) {
                    Debug.Log("FallingDuration: " + (Time.time - _fallingStartTime));
                }

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
            default:
                break;
            case State.IDLE:
            case State.MOVING:
            case State.RUNNING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
                _state = State.JUMP;
                _isFirstJump = true;
                Velocity.y = JumpSpeed;
                break;

            case State.LANDING:
                break;

            case State.JUMP:
            case State.FALLING:
                if (_isFirstJump || _controller.GetDistanceToGround() < JumpZone) {
                    Velocity.y = JumpSpeed;
                    _isFirstJump = false;
                    _state = State.JUMP;
                }

                break;
        }
    }

    private void on_Floating() {
        switch (_state) {
            default:
                break;
            case State.IDLE:
            case State.MOVING:
            case State.RUNNING:
            case State.LANDING:
            case State.AGAINST_WALL:
            case State.CROUCHING:
            case State.JUMP:
                _state = State.FALLING;
                _fallingStartTime = Time.time;
                break;
            
            case State.FALLING:
                break;
        }
    }
}