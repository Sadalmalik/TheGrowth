using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.ProcAnim
{
	public class AnimationTests : SerializedMonoBehaviour
	{
		public float cooldown;
		public Transform initial;
		public TrailRenderer trail;
		
		[Space(15)]
		public Transform objTransform = null;
		public Transform target = null;
		public Vector3? startScale = null;
		public Vector3? endScale   = null;
		public SpriteRenderer sprite = null;
		public Image          image  = null;
		
		[Space(15)]
		[InlineEditor]
		public AnimationProfile profile;
		
		[Sirenix.OdinInspector.Button]
		public void CheckBasic()
		{
			initial = gameObject.transform.Find("AnchorStart");
			target = gameObject.transform.Find("AnchorEnd");
			objTransform = gameObject.transform.Find("TestObject");
		}
		
		public void Start()
		{
			StartCoroutine(StartAnimation());
		}
		
		public IEnumerator StartAnimation()
		{
			while (true)
			{
				if (objTransform!=null)
				{
					objTransform.position = initial.position;
					objTransform.localScale = initial.localScale;
					trail.Clear();
				}
				
				yield return new WaitForSeconds(cooldown / 2);
				
				var tween = profile.GetFullTween(
					objTransform, target.position,
					startScale, endScale,
					sprite, image
				);
				
				if (tween!=null)
					yield return tween.WaitForCompletion();
				
				yield return new WaitForSeconds(cooldown / 2);
			}
		}
	}
}