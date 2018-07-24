using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recorder : MonoBehaviour {
    private bool _isRecording = true;

    public int MaxLength = 100;

    public struct Frame {
        public Vector3 Position;
        public Vector3 Velocity;
        public PlayerController.State PlayerState;
        public PlayerController.State PreviousPlayerState;

        public Frame(Vector3 _position, Vector3 _velocity,
            PlayerController.State _playerState,
            PlayerController.State _previousPlayerState) {
            Position = _position;
            Velocity = _velocity;
            PlayerState = _playerState;
            PreviousPlayerState = _previousPlayerState;
        }
    }

    private Queue<Frame> _history = new Queue<Frame>();
    private Frame[] _finalized;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            _isRecording = !_isRecording;
            Debug.Log("Recording: " + _isRecording);

            if (!_isRecording) {
                Finalize();
            }
        }
    }

    public bool IsRecording() {
        return _isRecording;
    }

    public void Reset() {
        _history.Clear();
    }

    public void RecordFrame(Vector3 position, Vector3 velocity,
        PlayerController.State playerState, PlayerController.State previousPlayerState) {
        _history.Enqueue(new Frame(position, velocity, playerState, previousPlayerState));
        if (_history.Count > MaxLength) {
            _history.Dequeue();
        }
    }

    public void Finalize() {
        _finalized = _history.ToArray();
    }

    public int GetLength() {
        return _finalized.Length;
    }

    public Frame GetFrame(int i) {
        if (i >= _finalized.Length) {
            return new Frame(new Vector3(0, 0, 0), new Vector3(0, 0, 0), PlayerController.State.IDLE,
                PlayerController.State.IDLE);
        }

        return _finalized[_finalized.Length - i - 1];
    }
}