using System.Collections.Generic;
using UnityEngine;
namespace VectorWhitebox
{
    public class WhiteboxHazard : MonoBehaviour
    {
        Collider2D area;
        readonly List<Collider2D> overlaps = new List<Collider2D>();

        void Awake() { area = GetComponent<Collider2D>(); }
        void OnTriggerEnter2D(Collider2D other) { ApplyHazard(other); }
        void OnTriggerStay2D(Collider2D other) { ApplyHazard(other); }

        void FixedUpdate()
        {
            if (!area || !area.enabled) return;
            // Static turrets do not produce trigger callbacks against a static spike pit.
            area.Overlap(new ContactFilter2D { useTriggers = true }, overlaps);
            foreach (var other in overlaps)
                if (other.GetComponentInParent<WhiteboxTurret>()) ApplyHazard(other);
        }

        void ApplyHazard(Collider2D other)
        {
            var turret = other.GetComponentInParent<WhiteboxTurret>();
            if (turret) turret.Hit();
            if (other.GetComponentInParent<WhiteboxPlayer>() && WhiteboxGame.Instance)
                WhiteboxGame.Instance.Damage(1000);
        }
    }
}
