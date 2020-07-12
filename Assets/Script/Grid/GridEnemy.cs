using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridEnemy : GridActor
{
    // 0 - 100 Percentage chance
    private const float WanderEnd_NormalWaitChance = 50f;
    private const float WanderEnd_PassiveWaitChance = 75f;
    private const float WanderEnd_AggressiveWaitChance = 20f;

    private const float WaitFor_NormalAmount = 3.5f;
    private const float WaitFor_PassiveAmount = 5.5f;
    private const float WaitFor_AggressiveAmount = 1.5f;

    private const float ChaseChance_NormalPercentage = 45f;
    private const float ChaseChance_PassivePercentage = 15f;
    private const float ChaseChance_AggressivePercentage = 75f;

    private const float ChaseDuration_Normal = 7.5f;
    private const float ChaseDuration_Passive = 4.5f;
    private const float ChaseDuration_Aggressive = 15f;

    private const int MaximumNumberOfChasers = 3;

    private const float TimeBetweenLosCaches = 0.1f;
    private const float TimeBetweenBreakToChaseChecks = 0.1f;
    private const float TooManyChasersCooldownCheck = -5.0f;

    public enum EnemyBehaviourStyle
    {
        Normal = 0,
        Passive,
        Aggressive
    }

    public enum EnemyPower
    {
        None,

        ConfusePlayer,
        GlitchScreen,
        SlowPlayer
    }

    public enum EnemyFsmState
    {
        State_Wander, // Wandering around the map aimlessly
        State_Wait, // Waiting on a point for a little while
        State_Chase, // Chasing after a target, normally the player
        State_Goto, // Moving to a specific point

        State_Invalid
    }

    public enum EnemyFsmSubState
    {
        Enter,
        Update,
        Exit
    }

    public bool RandomiseBehaviour;

    private Path currentPath;
    private Vector2Int targetPosition;
    private float waitForTime;

    private float stateTimer;
    private float losCacheInterval;
    private float breakToChaseStateTimer;
    private float chasingOutOfSightTimer;
    private float powerCheckTimer;

    private bool hasLosToPlayer;

    private EnemyFsmState fsmState;
    private EnemyFsmState fsmPendingState = EnemyFsmState.State_Invalid;
    private EnemyFsmSubState fsmSubState;

    public EnemyPower PowerStyle;
    public float PowerAffectDistance = 3f;
    public bool CanAffectWithoutLos = true;

    [HideInInspector]
    public bool IsAffectingPlayerWithPower;

    public EnemyBehaviourStyle BehaviourStyle;

    public bool IsChasing => fsmState == EnemyFsmState.State_Chase;

    public bool IsMoving => fsmState != EnemyFsmState.State_Wait && fsmState != EnemyFsmState.State_Invalid;

    // Start is called before the first frame update
    public override void Start() 
    {
        base.Start();

        if (RandomiseBehaviour)
        {
            BehaviourStyle = (EnemyBehaviourStyle)Random.Range(0, 3);
        }
    }

    #region States

    // Update is called once per frame
    public override void Update()
    {
        if (!Service.Flow.IsGameRunning)
        {
            return;
        }

        base.Update();

        stateTimer += GameConfig.GetDeltaTime();

        // Always cache the los to the player
        ProcessCacheLos();

        ProcessEnemyPower();

        if (fsmState != EnemyFsmState.State_Chase)
        {
            // Every internal, try to break out of the current state into a chase
            ProcessBreakToChase();
        }

        switch (fsmState)
        {
            case EnemyFsmState.State_Wander:
                StateWander();
                break;
            case EnemyFsmState.State_Wait:
                StateWait();
                break;
            case EnemyFsmState.State_Chase:
                StateChase();
                break;
            case EnemyFsmState.State_Goto:
                StateGoto();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void SetAffectingPlayerWithPower()
    {
        if (!IsAffectingPlayerWithPower)
        {
            switch (PowerStyle)
            {
                case EnemyPower.ConfusePlayer:
                    Service.Player.AreControlsConfused = true;
                    break;
                case EnemyPower.GlitchScreen:

                    break;
                case EnemyPower.SlowPlayer:
                    Service.Player.IsBeingSlowed = true;

                    break;
            }
        }

        IsAffectingPlayerWithPower = true;
    }

    void SetNotAffectingPlayerWithPower()
    {
        if (IsAffectingPlayerWithPower && !AreOtherEnemiesAffectingPlayerWithMyPower())
        {
            switch (PowerStyle)
            {
                case EnemyPower.ConfusePlayer:
                    Service.Player.AreControlsConfused = false;
                    break;
                case EnemyPower.GlitchScreen:
                    
                    break;
                case EnemyPower.SlowPlayer:
                    Service.Player.IsBeingSlowed = false;

                    break;
            }
        }

        IsAffectingPlayerWithPower = false;
    }

    void ProcessEnemyPower()
    {
        if (PowerStyle == EnemyPower.None)
        {
            return;
        }

        powerCheckTimer += GameConfig.GetDeltaTime();
        if (powerCheckTimer < 0.5f)
        {
            return;
        }

        powerCheckTimer = 0f;

        var dist = Vector3.Distance(GetWorldPosition(), Service.Player.GetWorldPosition());

        if (dist >= PowerAffectDistance)
        {
            SetNotAffectingPlayerWithPower();
            return;
        }

        if (!CanAffectWithoutLos)
        {
            if (!Service.Grid.HasGridLos(GetGridPosition(), Service.Player.GetGridPosition()))
            {
                SetNotAffectingPlayerWithPower();
                return;
            }
        }

        SetAffectingPlayerWithPower();
    }

    void ProcessCacheLos()
    {
        losCacheInterval += GameConfig.GetDeltaTime();

        if (losCacheInterval >= TimeBetweenLosCaches)
        {
            losCacheInterval = 0;

            if (Service.Player)
            {
                RaycastHit2D res;

                Vector2 fromPos = GetWorldPosition();
                Vector2 targetPos = Service.Player.GetWorldPosition();

                var hitTest = Physics2D.Linecast(fromPos, targetPos, LayerMask.GetMask("Default"));
                hasLosToPlayer = hitTest.collider == null;

                // Old way
               // hasLosToPlayer = Service.Grid.HasGridLos(GetGridPosition(), Service.Player.GetGridPosition());
            }
        }
    }

    bool ShouldWaitAtWanderExit()
    {
        var randomChance = Random.Range(0, 100f);

        switch(BehaviourStyle)
        {
            case EnemyBehaviourStyle.Normal:
                return randomChance < WanderEnd_NormalWaitChance;
                
            case EnemyBehaviourStyle.Passive:
                return randomChance < WanderEnd_PassiveWaitChance;

            case EnemyBehaviourStyle.Aggressive:
                return randomChance < WanderEnd_AggressiveWaitChance;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void ProgressState()
    {
        stateTimer = 0f;

        switch (fsmSubState)
        {
            case EnemyFsmSubState.Enter:
                fsmSubState = EnemyFsmSubState.Update;
                break;
            case EnemyFsmSubState.Update:
                fsmSubState = EnemyFsmSubState.Exit;
                break;
            case EnemyFsmSubState.Exit:

                fsmSubState = EnemyFsmSubState.Enter;

                if (fsmPendingState == EnemyFsmState.State_Invalid)
                {
                    switch (fsmState)
                    {
                        case EnemyFsmState.State_Wander:

                            if (ShouldWaitAtWanderExit())
                            {
                                fsmState = EnemyFsmState.State_Wait;
                            }
                            // else Stays in wander

                            break;

                        case EnemyFsmState.State_Wait:
                            fsmState = EnemyFsmState.State_Wander;
                            break;

                        case EnemyFsmState.State_Chase:
                            fsmState = Random.Range(0f, 100f) > 50f ? EnemyFsmState.State_Wander : EnemyFsmState.State_Wait;
                            break;

                        case EnemyFsmState.State_Goto:
                            fsmState = EnemyFsmState.State_Wait;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    fsmState = fsmPendingState;
                    fsmPendingState = EnemyFsmState.State_Invalid;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void GoIntoState(EnemyFsmState newState)
    {
        fsmState = newState;
        fsmSubState = EnemyFsmSubState.Enter;
    }

    void QuitCurrentState()
    {
        fsmSubState = EnemyFsmSubState.Exit;
    }

    void ProcessBreakToChase()
    {
        breakToChaseStateTimer += GameConfig.GetDeltaTime();
        if (breakToChaseStateTimer < TimeBetweenBreakToChaseChecks)
        {
            return;
        }

        breakToChaseStateTimer = 0f;

        if (hasLosToPlayer)
        {
            if (GridActor.GetNumberOfActorsChasing() >= MaximumNumberOfChasers)
            {
                breakToChaseStateTimer = TooManyChasersCooldownCheck;
                return;
            }

            var rand = Random.Range(0, 100f);
            bool doChase = false;

            switch (BehaviourStyle)
            {
                case EnemyBehaviourStyle.Normal:
                    doChase = (rand < ChaseChance_NormalPercentage);
                    break;
                case EnemyBehaviourStyle.Passive:
                    doChase = (rand < ChaseChance_PassivePercentage);
                    break;
                case EnemyBehaviourStyle.Aggressive:
                    doChase = (rand < ChaseChance_AggressivePercentage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (doChase)
            {
                fsmPendingState = EnemyFsmState.State_Chase;
                QuitCurrentState();
                ProgressState();
            }
        }
    }

    void StateWander()
    {
        switch (fsmSubState)
        {
            case EnemyFsmSubState.Enter:

                var currentPos = GetGridPosition();

                targetPosition = Service.Grid.GetPositionNearby(currentPos);
                currentPath = Service.Grid.GetPath(currentPos, targetPosition);
                currentPath.UpdateProgression(GetGridPosition());

                ProgressState();

                break;

            case EnemyFsmSubState.Update:

                if (TaskMoveAlongPath(ref currentPath))
                {
                    ProgressState();
                }

                break;

            case EnemyFsmSubState.Exit:

                targetPosition = GridManager.INVALID_TILE;
                currentPath = null;

                ProgressState();

                break;
        }
    }

    void StateWait()
    {
        switch (fsmSubState)
        {
            case EnemyFsmSubState.Enter:

                switch (BehaviourStyle)
                {
                    case EnemyBehaviourStyle.Normal:
                        waitForTime = WaitFor_NormalAmount;
                        break;
                    case EnemyBehaviourStyle.Passive:
                        waitForTime = WaitFor_PassiveAmount;
                        break;
                    case EnemyBehaviourStyle.Aggressive:
                        waitForTime = WaitFor_AggressiveAmount;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                ProgressState();

                break;
            case EnemyFsmSubState.Update:

                if (stateTimer >= waitForTime)
                {
                    ProgressState();
                }

                break;

            case EnemyFsmSubState.Exit:

                ProgressState();

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ImmediatelyLosePlayer()
    {
        if (fsmState != EnemyFsmState.State_Chase)
        {
            return;
        }

        hasLosToPlayer = false;
        chasingOutOfSightTimer = 0f;
    }

    void StateChase()
    {
        switch (fsmSubState)
        {
            case EnemyFsmSubState.Enter:

                ProgressState();
                break;

            case EnemyFsmSubState.Update:

                bool shouldStopChasing = false;

                if (Service.Player)
                {
                    if (hasLosToPlayer)
                    {
                        targetPosition = Service.Player.GetGridPosition();
                        chasingOutOfSightTimer = 0.6f;
                    }
                    else
                    {
                        chasingOutOfSightTimer += GameConfig.GetDeltaTime();
                        if (chasingOutOfSightTimer > 0f)
                        {
                            targetPosition = Service.Player.GetGridPosition();
                        }
                    }

                    if (currentPath != null)
                    {
                        // If the player has moved away from our path target
                        if (Vector2.Distance(currentPath.GetEndPosition(), targetPosition) > PATH_POINT_THRESHOLD)
                        {
                            currentPath = null;
                        }
                    }

                    if (currentPath == null)
                    {
                        currentPath = Service.Grid.GetPath(GetGridPosition(), targetPosition);
                        currentPath.UpdateProgression(GetGridPosition());
                    }

                    // Reach the end of the path, stop chasing (likely we have lost the player)
                    if (currentPath == null || TaskMoveAlongPath(ref currentPath))
                    {
                        shouldStopChasing = true;
                    }

                    switch (BehaviourStyle)
                    {
                        case EnemyBehaviourStyle.Normal:
                            shouldStopChasing = chasingOutOfSightTimer > ChaseDuration_Normal;
                            break;
                        case EnemyBehaviourStyle.Passive:
                            shouldStopChasing = chasingOutOfSightTimer > ChaseDuration_Passive;
                            break;
                        case EnemyBehaviourStyle.Aggressive:
                            shouldStopChasing = chasingOutOfSightTimer > ChaseDuration_Aggressive;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    shouldStopChasing = true;
                }

                if (shouldStopChasing)
                {
                    ProgressState();
                }

                break;
            case EnemyFsmSubState.Exit:

                targetPosition = GridManager.INVALID_TILE;
                currentPath = null;

                ProgressState();

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void StateGoto()
    {

    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            Service.Flow.SetGameOver();
        }
    }

    #endregion

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
#if DEBUG
        Handles.Label(transform.position, $"{fsmState}:{fsmSubState}");
#endif

        Vector2 fromPos = GetWorldPosition();
        Vector2 targetPos = Service.Player.GetWorldPosition();
        Vector2 direction = targetPos - fromPos;

        Gizmos.color = Physics2D.Linecast(fromPos, targetPos, LayerMask.GetMask("Default")).collider != null ? Color.red : Color.green;
        Gizmos.DrawLine(fromPos, targetPos);


        Gizmos.color = Color.magenta;
        if (currentPath?.IsValid ?? false)
        {
            Vector2 lastPoint = Vector2.negativeInfinity;

            foreach (var node in currentPath.GridPoints)
            {
                if (lastPoint == Vector2.negativeInfinity)
                {
                    lastPoint = node;
                }
                else
                {
                    Gizmos.DrawLine(lastPoint, new Vector3(node.x, node.y, 0));
                    lastPoint = node;
                }
            }
        }
    }
#endif
}
