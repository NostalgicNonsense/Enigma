using System.Collections.Generic;
using Assets.Enigma.Enums;
using UnityEngine;

namespace Assets.Enigma.Components.Base_Classes.Buildings.Turrets.Targeting
{
    public class ProximityTargeter : MonoBehaviour, ITargeter
    {
        private List<GameObject> _targetsInZone;

        public GameEntityType TargetType;
        private string _targetTag;
        public GameObject Target { get; private set; }

        public void Start()
        {
            _targetsInZone = new List<GameObject>();
            _targetTag = TargetType.ToString();
        }

        public void FixedUpdate()
        {
            if (Target == null && _targetsInZone.Count > 0)
            {
                FindNewTarget();
            }
        }

        public void OnTriggerEnter(Collider collision)
        {
            Debug.Log("new target? collision: " + collision.name);
            if (collision.gameObject.tag == _targetTag)
            {
                _targetsInZone.Add(collision.gameObject);
            }
        }
        
        public void OnTriggerExit(Collider collision)
        {
            if (collision.gameObject.tag == _targetTag)
            {
                _targetsInZone.Remove(collision.gameObject);
            }
        }

        private void FindNewTarget()
        {
            foreach (var target in _targetsInZone)
            {
                if (HasLineOfSight(target))
                {
                    Target = target;
                    break;
                }
            }
        }

        private bool HasLineOfSight(GameObject target)
        {
            RaycastHit hit;
            Physics.Linecast(transform.position, target.transform.position, out hit);
            if (hit.collider.gameObject.Equals(target))
            {
                return true;
            }
            return false;
        }
    }
}
