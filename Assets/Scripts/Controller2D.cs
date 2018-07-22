﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.ComTypes;
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

    void Start() {
        _collider = GetComponent<BoxCollider2D>();
    }

    public void Move(Vector3 velocity) {
        Debug.Log("Move input velocity " + velocity.x / Time.deltaTime);
        _computeCollisions(ref velocity);

        Debug.DrawRay(transform.position, velocity * 10.0f, Color.white);
        transform.Translate(velocity);
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

    private void _computeCollisions(ref Vector3 velocity) {
        // skin the bounding box extends

        Collision = Directions.None;

        Distances.Down = Mathf.Infinity;
        Distances.Up = Mathf.Infinity;
        Distances.Left = Mathf.Infinity;
        Distances.Right = Mathf.Infinity;

        SlopeAngles.Left = 0.0f;
        SlopeAngles.Right = 0.0f;
        SlopeAngles.Up = 0.0f;
        SlopeAngles.Down = 0.0f;

        _computeHorizontalCollisions(ref velocity);
        _computeVerticalCollisions(ref velocity);
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
            if (Physics.Raycast(ray, out hit, absVelocity + SkinWidth, CollisionMask)) {
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

                velocity.y = Mathf.Min(distance, absVelocity) * direction;
                Debug.DrawRay(rayStart, Vector2.up * hit.distance * direction, Color.red);
            } else {
                Debug.DrawRay(rayStart, Vector2.up * direction, Color.green);
            }
        }
    }

    private void _computeHorizontalCollisions(ref Vector3 velocity) {
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);

        float verticalInc = _bounds.extents.y * 2 / ((float) NumberOfHorizontalRays - 1);

        // horizontal rays
        // should I do both directions?
        Vector3 bottomRight = _bounds.min;
        bottomRight.x = _bounds.max.x;
        for (int i = 0; i < NumberOfHorizontalRays; i++) {
            float y = bottomRight.y + i * verticalInc;

            RaycastHit hit;

            if (velocity.x < 0.0f) {
                var left = new Vector2(_bounds.min.x, y);
                Ray rayLeft = new Ray(left, Vector2.left);
                if (Physics.Raycast(rayLeft, out hit, Math.Abs(velocity.x) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    Distances.Left = Mathf.Min(Distances.Left, distance);

                    if (i == 0) {
                        SlopeAngles.Left = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.forward);

                        if (IsSlopedLeft()) {
                            Vector3 newVelocity = new Vector3(velocity.x, 0.0f, 0.0f);

                            newVelocity = Quaternion.AngleAxis(-SlopeAngles.Left, Vector3.forward) * newVelocity;
                            newVelocity.y = Mathf.Max(velocity.y, newVelocity.y);

                            velocity = newVelocity;

                            // Mark as grounded
                            Collision |= Directions.Down;
                        }
                    }

                    if (!IsSlopedLeft()) {
                        Collision |= Directions.Left;

                        // XXX this is where we want to climb the slope

                        velocity.x = Mathf.Max(-distance, velocity.x);
                    }

                    Debug.DrawRay(left, Vector2.left * hit.distance, Color.red);
                } else {
                    Debug.DrawRay(left, Vector2.left, Color.green);
                }
            } else {
                var right = new Vector2(_bounds.max.x, y);
                Ray rayRight = new Ray(right, Vector2.right);
                if (Physics.Raycast(rayRight, out hit, Math.Abs(velocity.x) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    Distances.Right = Mathf.Min(Distances.Right, distance);
                    Collision |= Directions.Right;

                    // XXX this is where we want to climb the slope
                    velocity.x = Mathf.Min(distance, velocity.x);
                    Debug.DrawRay(right, Vector2.right * hit.distance, Color.red);

                    if (i == 0) {
                        SlopeAngles.Right = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.forward);
                    }
                } else {
                    Debug.DrawRay(right, Vector2.right, Color.green);
                }
            }
        }

        velocity = _climbSlopes(velocity);
    }

    private Vector3 _climbSlopes(Vector3 velocity) {
        Vector3 newVelocity = new Vector3(velocity.x, 0.0f, 0.0f);

        if (newVelocity.x < 0.0f && IsSlopedLeft()) {
            newVelocity = Quaternion.AngleAxis(-SlopeAngles.Left, Vector3.forward) * newVelocity;
            newVelocity.y = Mathf.Max(velocity.y, newVelocity.y);

            // Mark as grounded
            Collision |= Directions.Down;
        } else if (velocity.x > 0.0f && IsSlopedRight()) {
            newVelocity = Quaternion.AngleAxis(-SlopeAngles.Right, Vector3.forward) * newVelocity;
            Debug.Log("Sloped right " + SlopeAngles.Right + " old " + velocity.x / Time.deltaTime + " new " +
                      newVelocity.x / Time.deltaTime);
            newVelocity.y = Mathf.Max(velocity.y, newVelocity.y);

            // Mark as grounded
            Collision |= Directions.Down;
        } else {
            newVelocity = velocity;
        }

        return newVelocity;
    }
}