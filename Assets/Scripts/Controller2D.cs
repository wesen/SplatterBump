using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.UI;

public class Controller2D : MonoBehaviour {
    private Collider2D _collider;
    public float SkinWidth = 0.15f;
    public int NumberOfVerticalRays = 5;
    public int NumberOfHorizontalRays = 5;
    public LayerMask CollisionMask;

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
        transform.Translate(velocity);
    }

    public bool IsGrounded() {
        return (Collision & Directions.Down) == Directions.Down;
    }

    void Update() {
    }

    private void _computeCollisions(ref Vector3 velocity) {
        // skin the bounding box extends
        Bounds _bounds = _collider.bounds;
        _bounds.Expand(-SkinWidth * 2);

        Collision = Directions.None;

        float horizontalInc = _bounds.extents.x * 2 / ((float) NumberOfVerticalRays - 1);
        float verticalInc = _bounds.extents.y * 2 / ((float) NumberOfHorizontalRays - 1);

        for (int i = 0; i < NumberOfVerticalRays; i++) {
            RaycastHit hit;

            float x = _bounds.min.x + i * horizontalInc;
            if (velocity.y < 0.0f) {
                var bottom = new Vector2(x, _bounds.min.y);
                Ray rayBottom = new Ray(bottom, Vector2.down);
                if (Physics.Raycast(rayBottom, out hit, Mathf.Abs(velocity.y) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    if (velocity.y < -distance) {
                        Collision |= Directions.Down;
                    }

                    velocity.y = Mathf.Max(-distance, velocity.y);
                    Debug.DrawRay(bottom, Vector2.down * hit.distance, Color.red);
                } else {
                    Debug.DrawRay(bottom, Vector2.down, Color.green);
                }
            } else {
                var top = new Vector2(x, _bounds.max.y);
                Ray rayTop = new Ray(top, Vector2.up);

                if (Physics.Raycast(rayTop, out hit, Mathf.Abs(velocity.y) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    if (velocity.y > distance) {
                        Collision |= Directions.Up;
                    }

                    velocity.y = Mathf.Min(distance, velocity.y);
                    Debug.DrawRay(top, Vector2.up * hit.distance, Color.red);
                } else {
                    Debug.DrawRay(top, Vector2.up, Color.green);
                }
            }
        }

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
                    if (velocity.x < -distance) {
                        Collision |= Directions.Left;
                    }

                    velocity.x = Mathf.Max(-distance, velocity.x);
                    Debug.DrawRay(left, Vector2.left * hit.distance, Color.red);
                } else {
                    Debug.DrawRay(left, Vector2.left, Color.green);
                }
            } else {
                var right = new Vector2(_bounds.max.x, y);
                Ray rayRight = new Ray(right , Vector2.right);
                if (Physics.Raycast(rayRight, out hit, Math.Abs(velocity.x) + SkinWidth, CollisionMask)) {
                    float distance = hit.distance - SkinWidth;
                    if (velocity.x > distance) {
                        Collision |= Directions.Right;
                    }

                    velocity.x = Mathf.Min(distance, velocity.x);
                    Debug.DrawRay(right , Vector2.right * hit.distance, Color.red);
                } else {
                    Debug.DrawRay(right , Vector2.right, Color.green);
                }
            }
        }
    }
}