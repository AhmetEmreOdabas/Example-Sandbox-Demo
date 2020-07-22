using UnityEngine;
using RPG.Combat;
using RPG.Core;
using RPG.Movement;
using RPG.Attributes;
using GameDevTV.Utils;
using System;

namespace RPG.Control
{
    public class AIController : MonoBehaviour
    {
       [SerializeField] float chaseDistance = 5f;
       [SerializeField] float suspicionTime = 5f;
        [SerializeField] float aggroTime = 5f;
       [SerializeField] PatrolPath patrolPath;
       [SerializeField] float wayPointTolerance = 1f;
       [SerializeField] float  wayPointDwellTime = 3f;
       [SerializeField] float agroDistance = 5f;
       [Range(0,1)]
       [SerializeField] float patrolSpeedFraction = 0.2f;
      
       Fighter fighter;
       Health health;
       Mover mover;
       GameObject player;
       float timeSinceLastSawPlayer = Mathf.Infinity;
        float timeSinceArriwedAtWaypoint = Mathf.Infinity;
        float timeSinceAggrevated = Mathf.Infinity;
       int currentWaypointIndex = 0;
 
        LazyValue<Vector3> guardPosition;
          
          private void Awake() 
          {
            health = GetComponent<Health>();
            fighter = GetComponent<Fighter>();
            mover = GetComponent<Mover>();
            player = GameObject.FindWithTag("Player");

            guardPosition = new LazyValue<Vector3>(GetGuardPosition);
          }

          private Vector3 GetGuardPosition()
          {
            return transform.position;
          }
       
       private void Start() 
       {
           
            guardPosition.ForceInit();

       }
       
       private void Update()
        {
            if (health.IsDead()) return;

            if (IsAggevated() && fighter.CanAttack(player))
            {
                
                AttackBehaviour();

            }
            else if (timeSinceLastSawPlayer < suspicionTime)
            {
                SuspicionBehaviour();
            }
            else
            {
                PatrolBehaviour();
            }
            UpdateTimers();
        }
        
        public void Aggro()
        {
            timeSinceAggrevated = 0;
        }

        private void UpdateTimers()
        {
            timeSinceLastSawPlayer += Time.deltaTime;
            timeSinceArriwedAtWaypoint += Time.deltaTime;
            timeSinceAggrevated += Time.deltaTime;
        }

        private void PatrolBehaviour()
        {
            Vector3 nextPositon = guardPosition.value;
            if (patrolPath != null)
            {
                if(AtWaypoint())
                {
                    timeSinceArriwedAtWaypoint = 0;
                    CycleWaypoint();
                }
                nextPositon = GetCurrentWaypoint();
            }
            if (timeSinceArriwedAtWaypoint > wayPointDwellTime)
            {
                mover.StartMoveAction(nextPositon,patrolSpeedFraction);
            }
            
        }

        private bool AtWaypoint()
        {
            float distanceToWaypoint = Vector3.Distance(transform.position,GetCurrentWaypoint());
            return distanceToWaypoint < wayPointTolerance;
        }

        private void CycleWaypoint()
        {
            currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
        }

        private Vector3 GetCurrentWaypoint()
        {
            return patrolPath.GetWaypoint(currentWaypointIndex);
        }

        private void SuspicionBehaviour()
        {
            GetComponent<ActionScheduler>().CancelCurrentAction();
        }

        private void AttackBehaviour()
        {
            timeSinceLastSawPlayer = 0;
            fighter.Attack(player);

            AggoNearbyEnemies();
        }

        private void AggoNearbyEnemies()
        {
           RaycastHit[] hits = Physics.SphereCastAll(transform.position, agroDistance, Vector3.up, 0);

           foreach (RaycastHit hit in hits)
           {
                AIController ai = hit.collider.GetComponent<AIController>();
                if(ai == null) continue;

                ai.Aggro();
           }
        }

        private bool IsAggevated()
        {

            float distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);
            return distanceToPlayer < chaseDistance || timeSinceAggrevated < aggroTime;
        }

        private void OnDrawGizmosSelected() 
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position,chaseDistance);
        }
    }

}