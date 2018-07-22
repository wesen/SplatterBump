using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recorder : MonoBehaviour {
    private bool _isRecording = true;
    
    public int MaxLength = 100;
    
    public struct Frame {
        public Vector3 Position;
        public Vector3 Velocity;

        public Frame(Vector3 _position, Vector3 _velocity) {
            Position = _position;
            Velocity = _velocity;
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

    public void RecordFrame(Vector3 position, Vector3 velocity) {
        _history.Enqueue(new Frame(position, velocity));
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
            return new Frame(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
        }
        return _finalized[_finalized.Length - i - 1];

    }
}