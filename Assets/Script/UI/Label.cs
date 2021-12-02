﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.UI {
	using Utility;

	public class Label : MonoBehaviour {
		[SerializeField]
		private GameObject next;
		[SerializeField]
		private float showingTime;
		[SerializeField]
		private string targetString;
		[SerializeField]
		private float loopInterval;

		private Text text;
		private Tweener tweener;

		public string Text {
			set {
				this.tweener.Kill();
				this.text.text = value;
			}
		}

		protected void Awake() {
			this.text = this.GetComponent<Text>();
			this.text.DOColor(this.text.color, this.showingTime).OnComplete(this.NewNext);
			this.tweener = this.text.DOText(this.targetString, this.loopInterval)
				.SetLoops(-1)
				.SetEase(Ease.Linear);

			Color cur = this.text.color;
			cur.a = 0;
			this.text.color = cur;
		}

		private void NewNext() {
			if (this.next != null) {
				GameObject.Instantiate(this.next, this.transform.parent);
			}
		}
	}
}