using UnityEngine;

namespace VectorWhitebox
{
    [DisallowMultipleComponent, RequireComponent(typeof(DirectionTarget))]
    public sealed class CrateScrapeAudio : MonoBehaviour
    {
        DirectionTarget target;
        Rigidbody2D body;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[32];
        void Awake() { target = GetComponent<DirectionTarget>(); body = GetComponent<Rigidbody2D>(); }

        void LateUpdate()
        {
            bool active = body && target && !target.isPlayer && body.simulated && body.bodyType == RigidbodyType2D.Dynamic && Time.timeScale > 0;
            var game = WhiteboxGame.Instance;
            active &= !game || (!game.Dead && !game.Completed && !game.InputLocked);
            float speed = 0;
            if (active)
            {
                Vector2 gravity = target.EffectiveGravityDirection;
                Vector2 lateral = new Vector2(-gravity.y, gravity.x);
                speed = Mathf.Abs(Vector2.Dot(body.linearVelocity, lateral));
                bool surface = false, pushingPlayer = false;
                int count = body.GetContacts(contacts);
                for (int i = 0; i < count; i++)
                {
                    var contact = contacts[i];
                    var other = contact.collider && contact.collider.attachedRigidbody == body ? contact.otherCollider : contact.collider;
                    if (!other || other.isTrigger) continue;
                    var player = other.GetComponentInParent<WhiteboxPlayer>();
                    if (player && player.body)
                    {
                        float towardCrate = body.position.x - player.body.position.x;
                        float input = (WhiteboxControls.Held(WhiteboxAction.Right) ? 1 : 0) -
                            (WhiteboxControls.Held(WhiteboxAction.Left) ? 1 : 0);
                        pushingPlayer |= Mathf.Abs(Vector2.Dot(contact.normal, lateral)) > .5f &&
                            input * towardCrate > .01f && player.body.linearVelocity.x * towardCrate > .02f &&
                            player.body.linearVelocity.x * body.linearVelocity.x > .01f;
                    }
                    else surface |= Mathf.Abs(Vector2.Dot(contact.normal, gravity)) > .55f;
                }
                active = speed > .12f && surface && pushingPlayer;
            }
            LaboratoryAudio.SetLoop(this, LaboratorySound.CrateScrape, active, transform.position,
                Mathf.Clamp(speed / 5f, .15f, 1f), 1f);
        }

        void OnDisable() { LaboratoryAudio.StopLoops(this); }
    }
}
