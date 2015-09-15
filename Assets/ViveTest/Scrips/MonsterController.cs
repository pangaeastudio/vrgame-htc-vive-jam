using UnityEngine;
using System.Collections.Generic;
using RootMotion.Demos;
using NodeCanvas.Framework;
using NodeCanvas.BehaviourTrees;
using RootMotion.FinalIK;

public class MonsterController : MonoBehaviour
{
    [SerializeField]
    GameObject targetPlayer;
    [SerializeField]
    GameObject[] wayPoints;

    public int HP = 100;
    private int currentHP;

    HitReaction hitReaction;
    Rigidbody rigidbody;

    BehaviourTreeOwner behaviourTree;
    Blackboard blackBoard;

    Animator animator;

    //public float HitReactTime = 1.0f;
    [SerializeField]
    //private bool isHitReact;
    private int hitReactType = -1;
    public float KnockDownRecoverTime = 1.0f;
    private bool isDownFront, isDownBack;
    private float getUpTime;

    public float LightHitForceGrade = 100;
    public float MediumHitForceGrade = 1000;
    public float HardHitForceGrade = 10000;

    private bool isDead;

    RagdollUtility ragdollUtility;

    public float DyingTime = 3;
    private float destoryTime;

    //0: normal/ai 1: hit reaction, 2: grab, 3: dead
    private MonsterStateEnum actionState = MonsterStateEnum.Normal;

    void Awake()
    {
        animator = GetComponent<Animator>();

        hitReaction = GetComponent<HitReaction>();

        rigidbody = GetComponent<Rigidbody>();

        if (targetPlayer == null)
        {
            targetPlayer = GameObject.FindGameObjectWithTag("Player");
        }

        if (wayPoints.Length == 0)
        {
            wayPoints = GameObject.FindGameObjectsWithTag("WayPoint");
        }

        if(blackBoard == null)
            blackBoard = gameObject.GetComponent<Blackboard>();

        if (behaviourTree == null)
            behaviourTree = gameObject.GetComponent<BehaviourTreeOwner>();

        if (blackBoard != null)
            blackBoard.SetValue("target", targetPlayer);

        if (behaviourTree != null)
        {
            List<GameObject> wps = new List<GameObject>(wayPoints);
            blackBoard.SetValue("PatrolWayPoints", wps);
        }

        currentHP = HP;

        ragdollUtility = GetComponent<RagdollUtility>();
    }

    public void SetState(MonsterStateEnum state)
    {
        if (actionState == MonsterStateEnum.Normal)
        {
            if (behaviourTree)
                behaviourTree.PauseBehaviour();
        }

        actionState = state;

        if (actionState == MonsterStateEnum.Normal)
        {
            if (behaviourTree)
                behaviourTree.StartBehaviour();
        }

        if (actionState != MonsterStateEnum.HitReact)
        {
            hitReactType = -1;
        }

        if (actionState == MonsterStateEnum.Dead)
        {
            destoryTime = Time.time + DyingTime;
        }

    }
	public void Hit(Collider collider, Vector3 direction, Vector3 point, int damage)
    {
        //Debug.LogWarning(direction.magnitude);
        //if (isDead)
        //    return;

        if (actionState != MonsterStateEnum.Normal)
            return;

        //if (isHitReact)
        //    return;

        currentHP -= damage;
        if (currentHP < 0)
            isDead = true;

        //isHitReact = true;
        SetState(MonsterStateEnum.HitReact);

        float hitForce = direction.magnitude;
        if (hitForce < LightHitForceGrade)
        {
            // Use the HitReaction
            hitReaction.Hit(collider, direction, point);
            if (isDead)
            {
                animator.SetBool("IsDead", true);
                SetState(MonsterStateEnum.Dead);
            }
            else
            {
                animator.SetBool("IsHit", true);
                hitReactType = 0;
            }
            //direction.y = 0;
            //transform.Translate(direction);
            ////rigidbody.velocity = (direction.normalized + (Vector3.up * 1)) * 1 * 1;
            //rigidbody.velocity = direction;
        }
        else if (hitForce < MediumHitForceGrade)
        {
            // Use the HitReaction
            hitReaction.Hit(collider, direction, point);

            //Vector3 forward = transform.TransformDirection(Vector3.forward);
            if (Vector3.Dot(transform.forward, direction) < 0)
            {
                animator.SetBool("IsKnockDownFront", true);
            }
            else
            {
                animator.SetBool("IsKnockDownBack", true);
            }
            hitReactType = 1;
        }
        else if (hitForce < HardHitForceGrade)
        {
            //animator.SetBool("IsKnockDown", true);

            ragdollUtility.EnableRagdoll();
            hitReactType = 2;

            Rigidbody rigi = collider.gameObject.GetComponent<Rigidbody>();
            if (rigi != null)
            {
                rigidbody.velocity = direction;
            }
            currentHP = 0;
            isDead = true;

            SetState(MonsterStateEnum.Dead);

        }
        else
        {
            //Knock Away
        }
    }

    public void Grab(Vector3 direction)
    {
        if (actionState != MonsterStateEnum.Normal)
            return;

        SetState(MonsterStateEnum.Grabed);

        ragdollUtility.EnableRagdoll();

        currentHP = 0;
        isDead = true;
    }

    public void Drop()
    {
        if (actionState != MonsterStateEnum.Grabed)
            return;

        SetState(MonsterStateEnum.Dead);
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(actionState == MonsterStateEnum.HitReact)
        {
            if (hitReactType == 0)
            {
                //isHitReact = false;
                SetState(MonsterStateEnum.Normal);
            }
            else if (hitReactType == 1)
            {

                if (!(isDownBack || isDownFront))
                {
                    isDownFront = animator.GetBool("IsDownFront");
                    isDownBack = animator.GetBool("IsDownBack");

                    if ((isDownBack || isDownFront))
                    {
                        if (isDead)
                        {
                            SetState(MonsterStateEnum.Dead);
                        }
                        else
                            getUpTime = Time.time + KnockDownRecoverTime;
                    }
                }
                else
                {
                    if (Time.time > getUpTime)
                    {
                        if (isDownFront)
                        {
                            animator.SetBool("IsGetUpFront", true);
                            isDownFront = false;
                        }
                        if (isDownBack)
                        {
                            animator.SetBool("IsGetUpBack", true);
                            isDownBack = false;
                        }
                        SetState(MonsterStateEnum.Normal);
                    }
                }
            }
            else if (hitReactType == 2)
            {
                //if(ragdollUtility.ragdollToAnimationTime)
            }

            //if (!isHitReact)
            //{
            //    hitReactType = -1;
            //    if (behaviourTree)
            //        behaviourTree.StartBehaviour();
            //}
        }
        else if (actionState == MonsterStateEnum.Grabed)
        {

        }
        else if (actionState == MonsterStateEnum.Dead)
        {
            if (Time.time > destoryTime)
            {
                Destroy(this);
            }
        }
    }

    public enum MonsterStateEnum
    {
        Normal = 0,
        HitReact = 1,
        Grabed = 2,
        Dead = 3
    }
}
