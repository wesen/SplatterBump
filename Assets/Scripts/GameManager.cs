using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour {
    public Transform[] SpawnPoints;

    public float RespawnTime = 1.0f;

    public struct PlayerGameInfo {
        public bool IsDead;
        public int Score;
        public Vector3 LastDeathPosition;
        public float TimeOfDeath;
        public PlayerController Player;

        public PlayerGameInfo(bool _IsDead, int _Score) {
            IsDead = _IsDead;
            Score = _Score;
            LastDeathPosition = new Vector3();
            TimeOfDeath = 0.0f;
            Player = null;
        }
    }

    public PlayerGameInfo[] PlayerInfos;

    void Start() {
        PlayerController[] _players = FindObjectsOfType<PlayerController>();
        PlayerInfos = new PlayerGameInfo[_players.Length];

        for (int i = 0; i < PlayerInfos.Length; i++) {
            int idx = _players[i].PlayerIndex;
            PlayerInfos[idx] = new PlayerGameInfo(false, 0);
            PlayerInfos[idx].Player = _players[i];
        }

        for (int i = 0; i < PlayerInfos.Length; i++) {
            _spawnPlayer(i);
        }
    }

    void _spawnPlayer(int playerIndex) {
        PlayerInfos[playerIndex].IsDead = false;

        // we need to make sure it wasn't used recently ??
        PlayerInfos[playerIndex].Player.Spawn(SpawnPoints[Random.Range(0, SpawnPoints.Length)].position);
    }

    public void OnPlayerKilled(int indexKiller, int indexKilled) {
        if (!PlayerInfos[indexKilled].IsDead) {
            Debug.Log("Player " + indexKiller + " killed " + indexKilled);
            PlayerInfos[indexKilled].Player.Die();
            PlayerInfos[indexKiller].Score += 1;
            PlayerInfos[indexKilled].IsDead = true;
            PlayerInfos[indexKilled].TimeOfDeath = Time.time;
        }
    }

    void LateUpdate() {
        for (int i = 0; i < PlayerInfos.Length; i++) {
            if (PlayerInfos[i].IsDead && (Time.time - PlayerInfos[i].TimeOfDeath) > RespawnTime) {
                _spawnPlayer(i);
            }
        }
    }
}