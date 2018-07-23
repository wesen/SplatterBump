using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;

public class Controller2D : MonoBehaviour {
    private Collider2D _collider;
    public float SkinWidth = 0.15f;
    public int NumberOfVerticalRays = 5;
    public int NumberOfHorizontalRays = 5;
    public LayerMask CollisionMask;
    public float MaxSlopeAngle = 45;
    public float MaxDescendingSlopeAngle = 45;

    struct BoundingPoints {
        public Vector3 TopRight;
        public Vector3 TopLeft;
        public Vector3 BottomRight;
        public Vector3 BottomLeft;

        public void Compute() {
        }
    }

    public struct CollisionDistances {
        public float Left;
        public float Right;
        public float Up;
        public float Down;

        public void Reset() {
            Down = Up = Right = Left = Mathf.Infinity;
        }
    }

    public struct Angles {
        public float Down;
        public float Up;
        public float Left;
        public float Right;

        public void Reset() {
            Up = Right = Left = Down = 0.0f;
        }
    }

    public struct CollisionInfo {
        public bool IsClimbingSlope;
        public bool IsDescendingSlope;
        public float SlopeAngle;
        public Vector3 OriginalVelocity;

        public void Reset(Vector3 _originalVelocity) {
            IsClimbingSlope = IsDescendingSlope = false;
            SlopeAngle = 0.0f;
            OriginalVelocity = _originalVelocity;
        }
    }

    private BoundingPoints _boundingPoints;

    // The angle of the slope if there is a collision on the right or on the left
    [HideInInspector] public Angles SlopeAngles;

    // Distance to the collision in each direction
    [HideInInspector] public CollisionDistances Distances;

    // Where we collided with the environment
    [HideInInspector] public Directions Collision;

    [HideInInspector] public CollisionInfo Info;

    [Flags]
    public enum Directions {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3
    }

    void Start() {
        _collider = GetComponent<BoxCollider2D>();
    }

    public void Move(Vector3 velocity) {
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

    public float GetDistanceToGround() {
        return Distances.Down;
    }

    private void _computeCollisions(ref Vector3 velocity) {
        Collision = Directions.None;

        Distances.Reset();
        SlopeAngles.Reset();
        Info.Reset(velocity);

        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);
        _boundingPoints.TopRight = new Vector3(_bounds.max.x, _bounds.max.y, 0.0f);
        _boundingPoints.TopLeft = new Vector3(_bounds.min.x, _bounds.max.y, 0.0f);
        _boundingPoints.BottomRight = new Vector3(_bounds.max.x, _bounds.min.y, 0.0f);
        _boundingPoints.BottomLeft = new Vector3(_bounds.min.x, _bounds.min.y, 0.0f);

        _computeDescendingSlope(ref velocity);
        _computeHorizontalCollisions(ref velocity);
        _computeVerticalCollisions(ref velocity);

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
        Vector3 rayStart = direction > 0 ? _boundingPoints.BottomLeft : _boundingPoints.BottomRight;

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
                        Info.IsDescendingSlope = true;
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

            float x = _bounds.min.x + i * horizontalInc + velocity.x;

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

        // Now make sure the updated ray doesn't hit a wall
//        if (Info.IsClimbingSlope) {
        {
            float xDirection = Math.Sign(velocity.x);
            Vector3 rayStart = xDirection > 0.0f ? _boundingPoints.BottomRight : _boundingPoints.BottomLeft;
            rayStart.y += velocity.y;
            RaycastHit hit;
            float rayLength = Math.Abs(velocity.x) + SkinWidth;

            if (Physics.Raycast(new Ray(rayStart, Vector3.right * xDirection), out hit, rayLength, CollisionMask)) {
                velocity.x = (hit.distance - SkinWidth) * xDirection;
            }
        }

//        }
    }

    private static float ANGLE_COMPARISON_TOLERANCE = 0.01f;

    private void _computeHorizontalCollisions(ref Vector3 velocity) {
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);

        float verticalInc = _bounds.extents.y * 2 / ((float) NumberOfHorizontalRays - 1);
        float absVelocity = Math.Abs(velocity.x);
        float direction = Math.Sign(velocity.x);

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
                        if (Info.IsDescendingSlope) {
                            Info.IsDescendingSlope = false;
                            velocity = Info.OriginalVelocity;
                        }

                        Info.IsClimbingSlope = true;
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
                        Info.IsClimbingSlope = false;
                    }
                }

                if (!Info.IsClimbingSlope) {
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