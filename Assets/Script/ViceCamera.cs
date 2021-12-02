﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Game {
	using Utility;
	using Network;

	public class ViceCamera : LockBehaviour {
		public enum TargetType {
			Opening,
			A,
			B
		}
		public delegate void OnEndDelegate(TargetType type);
		public static event OnEndDelegate OnEndEvent;
		private static ViceCamera INSTANCE;
		
		public static void Shake(Vector3 power, float time) {
			INSTANCE.transform.DOPunchPosition(power, time)
				.SetEase(Ease.InOutElastic)
				.OnComplete(INSTANCE.ResetShaking);
		}

		public static void Shake(Vector4 value) {
			ViceCamera.Shake(value, value.w);
		}

		public static void Move(TargetType type, float time) {
			Vector3 targetPos;
			Vector3 targetRot;
			INSTANCE.isGame = type != TargetType.Opening;
			INSTANCE.isMoving = true;

			if (INSTANCE.isGame) {
				targetPos = INSTANCE.gamePosition;
				targetRot = INSTANCE.gameRotation;

				if (type == TargetType.A) {
					targetRot.y = -targetRot.y;
				}
			} else {
				targetPos = INSTANCE.openingPosition;
				targetRot = INSTANCE.openingRotation;
			}

			Sound.Play(INSTANCE.clip);
			INSTANCE.transform.DOLocalMove(targetPos, time)
				.SetEase(Ease.InOutBack)
				.OnComplete(() => {
					ViceCamera.OnEndEvent(type);
					INSTANCE.isMoving = false;
				});
			INSTANCE.transform.DOLocalRotate(targetRot, time)
				.SetEase(Ease.InOutBack);
		}

		public static Vector3 ScreenToWorldPoint(Vector3 pos, float height=0) {
			pos.z = INSTANCE.camera.transform.position.y - height;
			return INSTANCE.camera.ScreenToWorldPoint(pos);
		}

		public static Ray ScreenToRay(Vector3 pos) {
			return INSTANCE.camera.ScreenPointToRay(pos);
		}

        public static Vector3 MousePosition {
            get {
                return ViceCamera.ScreenToWorldPoint(Input.mousePosition, 0.3f).ToFixed();
            }
        }

		[SerializeField]
		private Vector3 gamePosition = new Vector3(0, 10, 0);
		[SerializeField]
		private Vector3 gameRotation = new Vector3(90, 90, 0);
		[SerializeField]
		private Vector3 openingPosition = new Vector3(-25, 10, 0);
		[SerializeField]
		private Vector3 openingRotation = new Vector3(10, 90, 0);
		[SerializeField]
		private Transform target;
		[SerializeField]
		private float speed;
		[SerializeField]
		private AudioClip clip;

		private new Camera camera;
		private bool isGame = false;
		private bool isMoving = false;

		protected new void Awake() {
			base.Awake();

			INSTANCE = this;
			this.camera = this.GetComponent<Camera>();
		}

		protected override void LockUpdate() {
			if (!this.isGame && !this.isMoving && this.target) {
				this.transform.RotateAround(this.target.position, Vector3.up, this.speed);
			}
		}

		private void ResetShaking () {
			this.transform.localPosition = this.gamePosition;
		}
	}
}