﻿using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Invisibly created singleton that's created when an Animate call to SLayout is made, and runs
/// all the animations so that individual SLayouts can be super lightweight, not even having an Update call.
/// You can also choose to create an SLayoutAnimator and stick it in your scene if you want.
/// </summary>
public sealed class SLayoutAnimator : MonoBehaviour
{
	public bool IsAnimating(SLayout target) {
		foreach(var a in _animations) {
			if( a.owner == target ) return true;
		}
		return false;
	}

	public void Animate(float duration, float delay, AnimationCurve customCurve, Action animAction, Action completeAction, SLayout owner)
	{
		// The constructor runs the animAction
		var newAnim = new SLayoutAnimation(duration, delay, animAction, completeAction, customCurve, owner);

		if( !newAnim.isComplete )
			_animations.Add(newAnim);
	}

	public void AddDelay(float extraDelay)
	{
		var anim = SLayoutAnimation.AnimationUnderDefinition();
		if( anim != null ) {
			anim.AddDelay(extraDelay);
		}
	}

	public void AddDuration(float extraDuration)
	{
		var anim = SLayoutAnimation.AnimationUnderDefinition();
		if( anim != null ) {
			anim.AddDuration(extraDuration);
		}
	}

	public void Animatable(Action<float> customAnim)
	{
		var anim = SLayoutAnimation.AnimationUnderDefinition();
		if( anim != null ) {
			anim.AddCustomAnim(customAnim);
		} else {
			customAnim(1.0f);
		}
	}

	public void CancelAnimations(SLayout target)
	{
		_animations.RemoveAll(anim => {
			if( anim.owner == target ) {
				anim.Cancel();
				return true;
			}
			return false;
		});
	}

	public void CompleteAnimations(SLayout target)
	{
		foreach(var anim in _animations) {
			if( anim.owner != target ) continue;

			anim.CompleteImmediate();
		}
	}
		
	public void SetCanvasAnimationsPaused(Canvas canvas, bool paused, float delay = 0.0f)
	{
		if( delay == 0.0f ) 
			SetCanvasAnimationsPausedImmediate(canvas, paused);
		else
			CoroutineHelper.Delay(() => SetCanvasAnimationsPausedImmediate(canvas, paused), delay);
	}

	void SetCanvasAnimationsPausedImmediate(Canvas canvas, bool paused)
	{
		if( paused )
			TransferAnimationsWithCanvas(_animations, _pausedAnimations, canvas);
		else
			TransferAnimationsWithCanvas(_pausedAnimations, _animations, canvas);
	}

	void TransferAnimationsWithCanvas(List<SLayoutAnimation> from, List<SLayoutAnimation> to, Canvas canvas)
	{
		to.AddRange(from.Filter(a => a.owner.canvas == canvas));
		from.RemoveAll(a => a.owner.canvas == canvas);
	}


	public static SLayoutAnimator instance {
		get {
			if( _instance == null ) {
				var ownerGO = new GameObject("SLayoutAnimator");
				ownerGO.hideFlags = HideFlags.HideAndDontSave;
				ownerGO.AddComponent<SLayoutAnimator>();
			}
			return _instance;
		}
	}

	// Cope with potentially 2 scenes both containing animators -
	// one scene might be on the way out, so if so, we allow the
	// latest one to become the singleton. Accidentally having
	// extra animators is harmless.
	void Awake() {
		_instance = this;
	}
	void OnDestroy() {
		if( _instance == this ) _instance = null;
	}
		
	void Update() {

		if( _animations.Count > 0 ) {

			// Don't foreach, since a new animation could be added to the list
			// as part of the Update (due to a completion callback)
			for(int i=0; i<_animations.Count; ++i) {
				var anim = _animations[i];

				// If owner object has been deleted, stop the animation
				// (Definitely don't attempt to Update the anim since it'll likely
				//  access properties of the object that has been deleted.)
				if( !anim.canAnimate ) {
					_animationsToRemove.Add(anim);
					continue;
				}

				anim.Update();

				if( anim.isComplete )
					_animationsToRemove.Add(anim);
			}

			foreach(var anim in _animationsToRemove)
				_animations.Remove(anim);

			_animationsToRemove.Clear();
		}
	}

	List<SLayoutAnimation> _animations = new List<SLayoutAnimation>();
	List<SLayoutAnimation> _animationsToRemove = new List<SLayoutAnimation>();
	List<SLayoutAnimation> _pausedAnimations = new List<SLayoutAnimation>();

	public static SLayoutAnimator _instance;
}
