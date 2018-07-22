using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class Controller2D : MonoBehaviour {
    private Collider2D _collider;
    public float SkinWidth = 0.15f;
    public int NumberOfVerticalRays = 5;
    public int NumberOfHorizontalRays = 5;
    public LayerMask CollisionMask;
    public float MaxSlopeAngle = 45;
    public float MaxDescendingSlopeAngle = 45;

    public struct CollisionDistances {
        public float Left;
        public float Right;
        public float Up;
        public float Down;
    }

    public struct Angles {
        public float Down;
        public float Up;
        public float Left;
        public float Right;
    }

    // The angle of the slope if there is a collision on the right or on the left
    [HideInInspector] public Angles SlopeAngles;

    // Distance to the collision in each direction
    [HideInInspector] public CollisionDistances Distances;

    // Where we collided with the environment
    [HideInInspector] public Directions Collision;

    [Flags]
    public enum Directions {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3
    }

    private Recorder _recorder;

    [Range(0, 100)] public int ReplayFrame = 0;

    void Start() {
        _recorder = GetComponent<Recorder>();
        _collider = GetComponent<BoxCollider2D>();
    }

    public void Move(Vector3 velocity) {
        if (_recorder.IsRecording()) {
            if ((_lastPosition - transform.position).magnitude > 0.001 ||
                (_lastVelocity - velocity).magnitude > 0.001) {
                _lastPosition = transform.position;
                _lastVelocity = velocity;

                _recorder.RecordFrame(transform.position, velocity);
            }
        } else {
            Recorder.Frame frame = _recorder.GetFrame(ReplayFrame);
            transform.position = frame.Position;
            velocity = frame.Velocity;
        }

        _computeCollisions(ref velocity);
    }

    public bool IsGrounded() {
        return (Collision & Directions.Down) == Directions.Down;
    }

    public bool IsHitLeft() {
        return (Collision & Directions.Left) == Directions.Left;
    }

    public bool IsHitRight() {
        return (Collision & Directions.Right) == Directions.Right;
    }

    public bool IsSlopedLeft() {
        return (Collision & Directions.Left) == Directions.Left && SlopeAngles.Left < MaxSlopeAngle;
    }

    public bool IsSlopedRight() {
        return (Collision & Directions.Right) == Directions.Right && SlopeAngles.Right < MaxSlopeAngle;
    }

    private Vector3 _lastPosition = new Vector3();
    private Vector3 _lastVelocity = new Vector3();

    private void _computeCollisions(ref Vector3 velocity) {
        Collision = Directions.None;

        Distances.Down = Mathf.Infinity;
        Distances.Up = Mathf.Infinity;
        Distances.Left = Mathf.Infinity;
        Distances.Right = Mathf.Infinity;

        SlopeAngles.Left = 0.0f;
        SlopeAngles.Right = 0.0f;
        SlopeAngles.Up = 0.0f;
        SlopeAngles.Down = 0.0f;

        _computeDescendingSlope(ref velocity);
        _computeHorizontalCollisions(ref velocity);
//        Debug.DrawRay(transform.position, velocity.x * Vector3.right, Color.white);
//        transform.Translate(velocity.x * Vector3.right);
        _computeVerticalCollisions(ref velocity);
//        Debug.DrawRay(transform.position, velocity.y * Vector3.up, Color.white);
//        transform.Translate(velocity.y * Vector3.up);
        Debug.DrawRay(transform.position, velocity, Color.white);
        transform.Translate(velocity);
    }

    private void _computeDescendingSlope(ref Vector3 velocity) {
        if (Math.Abs(velocity.x) < 0.001f) {
            // not moving
            return;
        }

        if (velocity.y > 0.001f) {
            // jumping
            return;
        }

        float direction = Math.Sign(velocity.x);

        // if moving left, we want to cast a ray from our bottom right corner
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);
        Vector2 rayStart = new Vector3(0.0f, _bounds.min.y);
        rayStart.x = direction > 0 ? _bounds.min.x + SkinWidth : _bounds.max.x - SkinWidth;

        RaycastHit hit;
        Ray ray = new Ray(rayStart, Vector2.down);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, CollisionMask)) {
            float angle = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.back);
            if (Math.Abs(angle) < MaxDescendingSlopeAngle) {
                SlopeAngles.Down = angle;
                // check that we are descending in the direction of the normal
                if (Math.Abs(Math.Sign(hit.normal.x) - direction) < 0.0001f) {
                    Vector3 newVelocity = new Vector3(velocity.x, 0.0f, 0.0f);
                    newVelocity = Quaternion.AngleAxis(-angle, Vector3.back) * newVelocity;

                    Debug.DrawRay(transform.position, velocity, Color.yellow);
                    Debug.DrawRay(transform.position, Vector3.down * Math.Abs(newVelocity.y), Color.blue);
                    Debug.DrawRay(transform.position + new Vector3(0.1f, 0, 0),
                        Vector3.down * (hit.distance - SkinWidth), Color.green);

                    if (Math.Abs(newVelocity.y) > (hit.distance - SkinWidth)) {
                        newVelocity.y += velocity.y;
                        velocity = newVelocity;
                        Collision |= Directions.Down;
                    }

                    Debug.DrawRay(transform.position, newVelocity, Color.red);
                }
            }
        }
    }

    private void _computeVerticalCollisions(ref Vector3 velocity) {
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);
        float horizontalInc = _bounds.extents.x * 2 / ((float) NumberOfVerticalRays - 1);

        float direction = velocity.y > 0.0f ? 1.0f : -1.0f;
        float absVelocity = Math.Abs(velocity.y);

        for (int i = 0; i < NumberOfVerticalRays; i++) {
            RaycastHit hit;

            float x = _bounds.min.x + i * horizontalInc;

            var rayStart = new Vector2(x, direction > 0.0f ? _bounds.max.y : _bounds.min.y);

            Ray ray = new Ray(rayStart, Vector2.up * direction);
            var rayLength = (absVelocity + SkinWidth);
            if (Physics.Raycast(ray, out hit, rayLength, CollisionMask)) {
                float distance = hit.distance - SkinWidth;

                if (direction <= 0.0f) {
                    Distances.Down = Mathf.Min(Distances.Down, distance);
                    if (absVelocity >= distance) {
                        Collision |= Directions.Down;
                    }
                } else {
                    Distances.Up = Mathf.Min(Distances.Up, distance);
                    if (absVelocity >= distance) {
                        Collision |= Directions.Up;
                    }
                }

                velocity.y = Mathf.Min(distance, Math.Abs(velocity.y)) * direction;
                Debug.DrawRay(rayStart, Vector2.up * hit.distance * direction, Color.red);
            } else {
                Debug.DrawRay(rayStart, Vector2.up * direction * rayLength, Color.green);
            }
        }
    }

    private static float ANGLE_COMPARISON_TOLERANCE = 0.01f;

    private void _computeHorizontalCollisions(ref Vector3 velocity) {
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);

        float verticalInc = _bounds.extents.y * 2 / ((float) NumberOfHorizontalRays - 1);
        float absVelocity = Math.Abs(velocity.x);
        float direction = Math.Sign(velocity.x);
        
        bool isSloped = false;
        float lastSlopeAngle = 0.0f;

        Vector3 bottomRight = _bounds.min;
        bottomRight.x = _bounds.max.x;
        for (int i = 0; i < NumberOfHorizontalRays; i++) {
            float y = bottomRight.y + i * verticalInc;

            RaycastHit hit;

            var rayStart = direction < 0.0f ? new Vector2(_bounds.min.x, y) : new Vector2(_bounds.max.x, y);
            Ray ray = new Ray(rayStart, Vector2.right * direction);

            if (Physics.Raycast(ray, out hit, absVelocity + SkinWidth, CollisionMask)) {
                float distance = hit.distance - SkinWidth;

                float angle = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.forward);
                if (i == 0 || (i > 0 && Math.Abs(angle - lastSlopeAngle) > ANGLE_COMPARISON_TOLERANCE)) {
                    if (Math.Abs(angle) < MaxSlopeAngle) {
                        isSloped = true;
                        lastSlopeAngle = angle;

                        Vector3 newVelocity = new Vector3(velocity.x, 0.0f, 0.0f);
                        newVelocity = Quaternion.AngleAxis(angle, Vector3.back) * newVelocity;
                        // in case we are jumping
                        if (velocity.y > newVelocity.y) {
                            newVelocity.y = velocity.y;
                        } else {
                            // Mark as grounded
                            Collision |= Directions.Down;
                        }

                        velocity = newVelocity;
                    } else {
                        isSloped = false;
                    }
                }

                if (!isSloped) {
                    if (direction < 0.0f) {
                        Collision |= Directions.Left;
                    } else {
                        Collision |= Directions.Right;
                    }

                    velocity.x = direction * Mathf.Min(distance, Math.Abs(velocity.x));
                }

                if (direction < 0.0f) {
                    Distances.Left = Mathf.Min(Distances.Left, distance);
                } else {
                    Distances.Right = Mathf.Min(Distances.Right, distance);
                }

                Debug.DrawRay(rayStart, direction * Vector2.right * hit.distance, Color.red);
            } else {
                Debug.DrawRay(rayStart, direction * Vector2.right * absVelocity, Color.green);
            }
        }
    }
}