using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sadalmalik.Utils
{
    public static class AnimatorUtils
    {
        public static List<string> GetAvailableAnimations(this Animator animator, bool includeEmpty = true)
        {
            if (animator == null)
                return null;
            
            var list = animator.runtimeAnimatorController.animationClips.Select(element => element.name).ToList();
            if (includeEmpty)
                list.Insert(0, "");
            
            return list;
        }

        public static IEnumerator PlayCoroutine(this Animator animator, string animName, Action callback = null)
        {
            animator.Play(animName);
            
            var duration = 0f;
            var animations = animator.runtimeAnimatorController.animationClips;
            foreach (var anim in animations)
            {
                if (anim.name == animName)
                    duration = Mathf.Max(duration, anim.length);
            }

            if (duration>0) 
                yield return new WaitForSeconds(duration);

            callback?.Invoke();
        }
    }
}