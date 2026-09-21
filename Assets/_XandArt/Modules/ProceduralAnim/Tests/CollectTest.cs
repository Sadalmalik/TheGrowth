using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.ProcAnim
{
	public class CollectTest : SerializedMonoBehaviour
	{
		public List<Transform> transforms;
		[Space]
		public Transform spawnAnchor;
		public float     spawnRadius;
		public float     spawnDelay;
		public float     collectDelay;
		[Space]
		public float     cooldown;

		public Transform targetAnchor;
		
		[Space(15)]
		[InlineEditor]
		public AnimationProfile spawnAnimationProfile;
		[InlineEditor]
		public AnimationProfile collectAnimationProfile;

		public void Start()
		{
			StartCoroutine(StartAnimation());
		}
		
		public IEnumerator StartAnimation()
		{
			while (true)
			{
				foreach (var trans in transforms)
				{
					trans.position = spawnAnchor.position;
				}
			
				yield return new WaitForSeconds(cooldown / 2);
				
				var seq = DOTween.Sequence();
				var time = 0f;
				
				foreach (var trans in transforms)
				{
					var tween = spawnAnimationProfile.GetFullTween(
						trans, spawnAnchor.position + (Vector3)Random.insideUnitCircle * spawnRadius
					);
					
					seq.Insert(time, tween);
					time += spawnDelay;
				}
				
				yield return seq.WaitForCompletion();
				
				seq = DOTween.Sequence();
				time = 0;
				
				foreach (var trans in transforms)
				{
					var tween = collectAnimationProfile.GetFullTween(trans, targetAnchor.position);
					
					seq.Insert(time, tween);
					time += collectDelay;
				}
				
				yield return seq.WaitForCompletion();
				
				yield return new WaitForSeconds(cooldown / 2);
			}
		}
	}
}