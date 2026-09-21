// using System;
// using System.Collections;
// using System.Collections.Generic;
// using GeekyHouse.Architecture.Pooling;
// using Sirenix.OdinInspector;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace Sadalmalik.ProcAnim
// {
// 	public class FXHolder : PoolableBehaviour<FXHolder>
// 	{
// 		public ParticleSystem particles;
// 		public TrailRenderer trail;
// 		public SpriteRenderer sprite;
// 		public Image image;
//
// 		public bool clearOnPlay = true;
// 		public bool isNeedToDestroy = false;
// 		public bool ignoreRemainsParticles = true;
//
// 		public float Duration { get; private set; }
//
// 		private float _endTime;
//
// 		public event Action OnDispose;
//
// 		private void Awake()
// 		{
// 			Duration = 0;
// 			if (particles != null)
// 				Duration = particles.main.duration;
// 			if (trail != null)
// 				Duration = Mathf.Max(Duration, trail.time);
//
// 			Play();
// 		}
//
// 		public void SetImage(Sprite icon)
// 		{
// 			if (sprite)
// 				sprite.sprite = icon;
// 			if (image)
// 				image.sprite = icon;
// 		}
//
//
// #region Life cycle
//
// 		[Sirenix.OdinInspector.Button]
// 		public void Play()
// 		{
// 			if (trail)
// 				trail.enabled = true;
// 		    if (clearOnPlay)
// 		    {
// 			    // trail.Clear();
// 			    particles.Clear();
// 		    }
// 			particles.Play();
//
// 			_endTime = float.PositiveInfinity;
// 			if (isNeedToDestroy)
// 				Dispose();
// 		}
//
// 		[Sirenix.OdinInspector.Button]
// 		public void Stop()
// 		{
// 			particles.Stop();
// 			if (trail)
// 				trail.enabled = false;
// 		}
//
// 		private void Update()
// 		{
// 			if (_endTime < Time.time && (ignoreRemainsParticles || !particles.IsAlive()))
// 			{
// 				DisposeImmediate();
// 			}
// 		}
//
// 		public void Dispose(bool instant=false)
// 		{
// 			if (instant)
// 				DisposeImmediate();
// 			_endTime = Time.time + Duration;
// 		}
//
// 		public void DisposeImmediate()
// 		{
// 			OnDispose?.Invoke();
//
// 			if (pool != null)
// 			{
// 				pool.Free(this);
// 			}
// 			else
// 			{
// 				Destroy(gameObject);
// 			}
// 		}
//
// #endregion
//
//
// #region Pooling
//
// 		public override void OnLock()
// 		{
// 			this.Play();
// 		}
//
// 		public override void OnFree()
// 		{
// 			this.Stop();
// 		}
// 		
// #endregion
//
// 	}
// }