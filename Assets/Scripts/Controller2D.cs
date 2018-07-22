using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.ComTypes;
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
        _computeCollisions(ref velocity);

        Debug.Log("After Collision: " + velocity.x / Time.deltaTime + " y " + velocity.y / Time.deltaTime);
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

    public bool IsSloped() {
        if ((Collision & Directions.Left) == Directions.Left && SlopeAngles.Left < MaxSlopeAngle) {
            return true;
        } else if ((Collision & Directions.Right) == Directions.Right && SlopeAngles.Right < MaxSlopeAngle) {
            return true;
        } else {
            return false;
        }
    }

    private void _computeCollisions(ref Vector3 velocity) {
        // skin the bounding box extends
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);

        Collision = Directions.None;

        float horizontalInc = _bounds.extents.x * 2 / ((float) NumberOfVerticalRays - 1);
        float verticalInc = _bounds.extents.y * 2 / ((float) NumberOfHorizontalRays - 1);

        Distances.Down = Mathf.Infinity;
        Distances.Up = Mathf.Infinity;
        Distances.Left = Mathf.Infinity;
        Distances.Right = Mathf.Infinity;

        SlopeAngles.Left = 0.0f;
        SlopeAngles.Right = 0.0f;
        SlopeAngles.Up = 0.0f;
        SlopeAngles.Down = 0.0f;

        // vertical rays
        for (int i = 0; i < NumberOfVerticalRays; i++) {
            RaycastHit hit;

            float x = _bounds.min.x + i * horizontalInc;
            if (velocity.y <= 0.00001f) {
                var bottom = new Vector2(x, _bounds.min.y);
                Ray rayBottom = new Ray(bottom, Vector2.down);
                if (Physics.Raycast(rayBottom, out hit, Mathf.Abs(velocity.y) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    Distances.Down = Mathf.Min(Distances.Down, distance);
                    if (velocity.y < -distance) {
                        Collision |= Directions.Down;
                    }

                    velocity.y = Mathf.Max(-distance, velocity.y);
                    Debug.DrawRay(bottom, Vector2.down * hit.distance, Color.red);

                    if (i == 0) {
                        SlopeAngles.Down = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.forward);
                    }
                } else {
                    Debug.DrawRay(bottom, Vector2.down, Color.green);
                }
            } else {
                var top = new Vector2(x, _bounds.max.y);
                Ray rayTop = new Ray(top, Vector2.up);

                if (Physics.Raycast(rayTop, out hit, Mathf.Abs(velocity.y) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    Distances.Up = Mathf.Min(Distances.Up, distance);
                    if (velocity.y > distance) {
                        Collision |= Directions.Up;
                    }

                    velocity.y = Mathf.Min(distance, velocity.y);
                    Debug.DrawRay(top, Vector2.up * hit.distance, Color.red);
                    SlopeAngles.Up = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.forward);
                } else {
                    Debug.DrawRay(top, Vector2.up, Color.green);
                }
            }
        }

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
                    if (velocity.x < -distance) {
                        Collision |= Directions.Left;
                    }

                    velocity.x = Mathf.Max(-distance, velocity.x);
                    Debug.DrawRay(left, Vector2.left * hit.distance, Color.red);

                    if (i == 0) {
                        SlopeAngles.Left = Vector3.SignedAngle(hit.normal, Vector3.up, Vector3.forward);
                    }
                } else {
                    Debug.DrawRay(left, Vector2.left, Color.green);
                }
            } else {
                var right = new Vector2(_bounds.max.x, y);
                Ray rayRight = new Ray(right, Vector2.right);
                if (Physics.Raycast(rayRight, out hit, Math.Abs(velocity.x) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    Distances.Right = Mathf.Min(Distances.Right, distance);
                    if (velocity.x > distance) {
                        Collision |= Directions.Right;
                    }

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

//        Debug.Log("SlopeAngles left: " + SlopeAngles.Left + " right: " + SlopeAngles.Right + " down: " +
//                  SlopeAngles.Down + " up: " + SlopeAngles.Up);
    }
}