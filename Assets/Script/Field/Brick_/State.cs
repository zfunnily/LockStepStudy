﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Field.Brick_ {
	public class State {
		public class Data : ScriptableObject {
			public Type type;

			protected void OnEnable() {
				this.type = Type.GetType(this.GetType().ToString() + "State");
			}
		}

		protected GameObject gameObject;
		protected Statemgr statemgr;

		public State(GameObject gameObject, Data data) {
			this.gameObject = gameObject;
			this.statemgr = this.gameObject.GetComponent<Statemgr>();
		}

		public virtual void Update() {}
		public virtual void Enter() {}
		public virtual void Exit() {}
		public virtual void OnCollide(Collider collider) {}
		public virtual void OnDrag(Vector3 oldPos, Vector3 newPos) {}
		public virtual void OnDrawGizmosSelected() {}
	}
}