﻿using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Game.Field {
	using Utility;
	using Network;
	using Brick_;
	using UI;

	public enum GameType {
		NONE,
		PVP,
		PVE,
		HELP
	}

	public enum PlayerType {
		A,
		B
	}

	public class Judge : LockBehaviour {
		[System.Serializable]
		public class Team {
			public Wall wall;
			public Shooter shooter;
			public Brick brick;
			[System.NonSerialized]
			public int score;
			[System.NonSerialized]
			public string addr;

			public void AddScore() {
				this.score += 1;
				this.wall.SetLength((float)this.score / (float)INSTANCE.scoreMax);
			}

			public void Reset() {
				this.score = 0;
				this.wall.Reset();
				this.brick.Reset();
			}
		}

		private static Judge INSTANCE;

		public static bool IsRunning {
			set {
				INSTANCE.isRunning = value;

				if (INSTANCE.isRunning) {
					INSTANCE.Shoot(0.1f);
					Sound.PlayMusic(INSTANCE.music);
				} else {
					Sound.PlayMusic();
					Networkmgr.Disconnect();
				}
				
				INSTANCE.teamA.brick.isRunning = value;
				INSTANCE.teamB.brick.isRunning = value;
			}
			get {
				return INSTANCE.isRunning;
			}
		}

		public static PlayerType PlayerType {
			set {
				INSTANCE.playerTeam = value == PlayerType.A ? INSTANCE.teamA : INSTANCE.teamB;
				INSTANCE.opponentTeam = value == PlayerType.A ? INSTANCE.teamB : INSTANCE.teamA;
				INSTANCE.playerTeam.brick.isPlayer = true;
				INSTANCE.opponentTeam.brick.isPlayer = false;
			}
			get {
				return INSTANCE.playerTeam == INSTANCE.teamA ? PlayerType.A : PlayerType.B;
			}
		}

		public static GameType GameType {
			get;
			set;
		}

		public static float Rate {
			get {
				return INSTANCE.rate.ToFixed();	
			}
		}

		public static string Comparison {
			get {
				var sb = new StringBuilder();
				
				var pos = Ball.Position;
				var vel = Ball.Velocity;

				sb.Append(pos.x + ",");
				sb.Append(pos.y + ",");
				sb.Append(pos.z + ",");
				sb.Append(vel.x + ",");
				sb.Append(vel.y + ",");
				sb.Append(vel.z + ",");
				sb.Append(INSTANCE.teamA.brick.transform.localScale.x + ",");
				sb.Append(INSTANCE.teamA.brick.transform.position.x + ",");
				sb.Append(INSTANCE.teamA.brick.transform.position.z + ",");
				sb.Append(INSTANCE.teamB.brick.transform.localScale.x + ",");
				sb.Append(INSTANCE.teamB.brick.transform.position.x + ",");
				sb.Append(INSTANCE.teamB.brick.transform.position.z + ",");
				sb.Append(INSTANCE.teamA.wall.transform.position.z + ",");
				sb.Append(INSTANCE.teamB.wall.transform.position.z + ",");

				/*
				sb.Append(INSTANCE.teamA.brick.transform.localScale.x + ",");
				sb.Append(INSTANCE.teamA.brick.transform.position.x + ",");
				sb.Append(INSTANCE.teamB.brick.transform.localScale.x + ",");
				sb.Append(INSTANCE.teamB.brick.transform.position.x + ",");
				sb.Append(INSTANCE.teamA.brick.transform.position.z + ",");
				sb.Append(INSTANCE.teamB.brick.transform.position.z + ",");
				*/
				//sb.Append(INSTANCE.teamA.wall.scale.x + ",");
				//sb.Append(INSTANCE.teamB.wall.scale.x + ",");
				/*
				sb.Append(INSTANCE.teamA.wall.transform.position.z + ",");
				sb.Append(INSTANCE.teamB.wall.transform.position.z + ",");
				*/
				/*
				var md5 = MD5.Create();
				var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
				sb = new StringBuilder();

				for (int i = 0; i < bytes.Length; i++) {
					sb.Append(bytes[i].ToString("x2"));
				}*/

				return sb.ToString();
			}
		}

		public static void SetAddr(string a, string b) {
			INSTANCE.teamA.addr = a;
			INSTANCE.teamB.addr = b;
		}

		public static void Gain(Vector3 position) {
			if (position.x < 0) {
				INSTANCE.teamA.AddScore();
			} else {
				INSTANCE.teamB.AddScore();
			}

			for (int i = 0; i < INSTANCE.sounds.Length; i++) {
				Sound.Play(INSTANCE.sounds [i], INSTANCE.volume);
			}

			if (INSTANCE.teamA.score == INSTANCE.scoreMax || INSTANCE.teamB.score == INSTANCE.scoreMax) {
				Judge.IsRunning = false;
				UI.Interface.Result(INSTANCE.playerTeam.score >= INSTANCE.scoreMax, 0.5f);
			} else {
				INSTANCE.Shoot(INSTANCE.shootingTime);
			}
		}

		public static void Input(string addr, InputData inputData) {
			var team = INSTANCE.teamA.addr == addr ? INSTANCE.teamA : INSTANCE.teamB;
			team.brick.Input(inputData);
		}

		public static void Input(InputData inputData) {
			INSTANCE.teamB.brick.Input(inputData);
		}

		[SerializeField]
		private Team teamA;
		[SerializeField]
		private Team teamB;
		[SerializeField]
		private AudioClip[] sounds;
		[SerializeField]
		private AudioClip music;
		[SerializeField]
		private float volume = 0.5f;
		[SerializeField]
		private float acceleration = 0.00005f;
		[SerializeField]
		private float shootingTime = 2;
		[SerializeField]
		private int scoreMax = 5;

		private bool aShooted;
		private bool isRunning;
		private Team playerTeam;
		private Team opponentTeam;
		private Timer timer;
		private float rate = 1;

		protected new void Awake() {
			base.Awake();

			INSTANCE = this;
			ViceCamera.OnEndEvent += this.Reset;
			this.timer = new Timer();
		}

		protected override void LockUpdate() {
			if (!this.isRunning) {
				return;
			}

			this.timer.Update();
			this.rate += this.acceleration;
			Sound.MusicPitch = this.rate;
		}

		public void Shoot(float time) {
			this.timer.Enter(time, this.TickShoot);
		}

		private void Reset(ViceCamera.TargetType type) {
			if (type == ViceCamera.TargetType.Opening) {
				this.aShooted = false;
				this.rate = 1;
				this.teamA.Reset ();
				this.teamB.Reset ();
			}
		}

		private void TickShoot() {
			if (this.aShooted) {
				this.teamA.shooter.Shoot();
			} else {
				this.teamB.shooter.Shoot();
			}

			this.aShooted = !this.aShooted;
		}
	}
}