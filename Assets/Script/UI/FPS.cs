﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
	public class FPS : MonoBehaviour {
		private Text text;
		private float timer;
		private int frame;

		protected void Awake() {
			this.text = this.GetComponent<Text>();
		}

		protected void Update() {
			this.timer += Time.deltaTime;
			this.frame += 1;

			if (this.timer >= 1) {
				this.timer = 1 - this.timer;
				this.text.text = this.frame.ToString();
				this.frame = 0;
			}
		}
	}
}