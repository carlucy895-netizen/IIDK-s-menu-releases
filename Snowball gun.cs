using GorillaLocomotion;
using StupidTemplate.Classes;
using UnityEngine;
using UnityEngine.XR;
using static Collision.Model;
using static StupidTemplate.Menu.Main;

namespace StupidTemplate.Mods
{
	public class SnowballGun
	{
		private static GameObject grabbedObject;
		private static Rigidbody grabbedRb;

		// Tweakable parameters
		public static float pullSpeed = 35f;
		public static float holdDistance = 0.2f;
		public static float throwStrength = 10.5f;

		private static bool previousTrigger;

        // Call this in the menu execution loop (toggable button)
        [System.Obsolete]
        public static void SnowballGrab()
		{
			// Only operate while the player is in-game and using the right-hand gun
			if (!NetworkSystem.Instance.InRoom) return;

			if (!ControllerInputPoller.instance.rightGrab) return;

			var GunData = RenderGun();
			RaycastHit ray = GunData.Ray;

			float trigger = ControllerInputPoller.TriggerFloat(XRNode.RightHand);

			// If we are not currently holding anything, attempt to grab when trigger pressed
			if (grabbedObject == null)
			{
				if (trigger > 0.5f)
				{
					GameObject target = null;

					if (ray.collider != null)
						target = ray.collider.gameObject;

					// Try attached rigidbody object if collider is part of a child
					if (target == null && ray.rigidbody != null)
						target = ray.rigidbody.gameObject;

					// If ray found nothing, look for nearby small pickups at the ray point
					if (target == null)
					{
						Vector3 searchCenter = ray.point;
						if (searchCenter == Vector3.zero) searchCenter = GorillaTagger.Instance.rightHandTransform.position + GorillaTagger.Instance.rightHandTransform.forward * 0.5f;
						Collider[] nearby = Physics.OverlapSphere(searchCenter, 0.6f);
						float bestDist = float.MaxValue;
						foreach (var c in nearby)
						{
							if (c == null) continue;
							if (IsSnowballLike(c.gameObject))
							{
								float d = Vector3.Distance(searchCenter, c.transform.position);
								if (d < bestDist)
								{
									bestDist = d;
									target = c.gameObject;
								}
							}
						}
					}

					if (target != null && IsSnowballLike(target))
					{
						grabbedObject = target;
						grabbedRb = grabbedObject.GetComponent<Rigidbody>();

						if (grabbedRb != null)
						{
							grabbedRb.isKinematic = true;
							grabbedRb.useGravity = false;
						}
					}
				}
			}
			else // We are holding an object: move it toward the right hand and release/throw on trigger
			{
				try
				{
					if (grabbedObject == null)
					{
						grabbedRb = null;
						return;
					}

					Transform hand = GorillaTagger.Instance.rightHandTransform;
					Vector3 targetPos = hand.position + hand.forward * holdDistance;

					// Smoothly move the object to the target hold position
					grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, targetPos, Time.deltaTime * pullSpeed);
					grabbedObject.transform.rotation = Quaternion.Lerp(grabbedObject.transform.rotation, hand.rotation, Time.deltaTime * pullSpeed);

					// On trigger press again, throw / release
					if (trigger > 0.5f && !previousTrigger)
					{
						if (grabbedRb != null)
						{
							grabbedRb.isKinematic = false;
							grabbedRb.useGravity = true;
							grabbedRb.velocity = Vector3.zero;
							grabbedRb.AddForce(hand.forward * throwStrength, ForceMode.VelocityChange);
						}

						grabbedObject = null;
						grabbedRb = null;
					}
				}
				catch
				{
					// Release on trigger RELEASE: if trigger is no longer held, drop at the ray hit point (if valid)
					if (trigger <= 0.7f && previousTrigger)
					{
						if (grabbedObject != null)
						{
							try
							{
								if (grabbedRb != null)
								{
									// Place at ray hit point if available
									if (ray.collider != null)
									{
										grabbedObject.transform.position = ray.point;
										grabbedObject.transform.rotation = Quaternion.identity;
										grabbedRb.isKinematic = false;
										grabbedRb.useGravity = true;
										grabbedRb.velocity = Vector3.zero;
									}
									else
									{
										// No ray hit: drop slightly in front of hand
										grabbedObject.transform.position = hand.position + hand.forward * 0.5f;
										grabbedRb.isKinematic = false;
										grabbedRb.useGravity = true;
										grabbedRb.velocity = Vector3.zero;
									}
								}
							}
							catch
							{
								// ignore
							}
							finally
							{
								grabbedObject = null;
								grabbedRb = null;
							}
						}
					}
				}
			}

			previousTrigger = trigger > 0.5f;
		}

		private static bool IsSnowballLike(GameObject grab = 90f);
		{
			if (grab == null) return false;

			// Check for snowball in name
			if (go.name != null && go.name.ToLower().Contains("snowball")) return true;

			// Check for Snowball tag
			if (go.CompareTag("snowball")) return true;

			// Check parent and child objects for snowball indicators
			if (go.transform.parent != null && go.transform.parent.name.ToLower().Contains("snowball")) return true;
			foreach (Transform child in go.transform)
			{
				if (child.name.ToLower().Contains("snowball")) return true;
			}

			return false;
		}
	}
}

