using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Guard : MonoBehaviour {
    public float speed = 5;
    public float waitTime = .3f;
    public float turnSpeed = 90;
    public float timeToSpotPlayer = .5f;
    public float meshResolution;

    public float viewDistance;
    public LayerMask viewMask;
    public LayerMask obstacleMask;

    public MeshFilter nearViewMeshFilter;
    Mesh nearViewMesh;
    public MeshFilter farViewMeshFilter;
    Mesh farViewMesh;
    public MeshFilter detectViewMeshFilter;
    Mesh detectViewMesh;
    public int edgeResolveIterations;
    public float edgeDstThreshhold;

    public float nearViewDst;
    public float farViewDst;
    private float detectViewDst;
    [Range(0, 360)]
    public float viewAngle;

    private float currentViewDstPenalty;
    private float currentAnglePenalty;

    float playerVisibleTimer;

    public Transform pathHolder;
    Transform player;
    Color originalSpotlightColour;

    private NavMeshAgent agent;

    private IEnumerator followPathCoroutine;
    bool isFollowNormalRunning = false;


    private IEnumerator followPathCautionCoroutine;
    bool isFollowCautionRunning = false;

    private IEnumerator lookAroundCoroutine;
    bool isLookingAround = true;

    private Vector3 chasePoint;
    private int targetWaypointIndex = 1;

    private float guardSpeed; // It's 3.5
    [Range (-90,90)]
    public float turnViewAngle;

    void Start() {

        nearViewMesh = new Mesh();
        nearViewMesh.name = "Near View Mesh";
        nearViewMeshFilter.mesh = nearViewMesh;

        farViewMesh = new Mesh();
        farViewMesh.name = "Far View Mesh";
        farViewMeshFilter.mesh = farViewMesh;

        detectViewMesh = new Mesh();
        detectViewMesh.name = "Detect View Mesh";
        detectViewMeshFilter.mesh = detectViewMesh;

        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;
        guardSpeed = agent.speed;

        currentViewDstPenalty = 0f;
        currentAnglePenalty = 0f;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        GameManager.informGuardAlarmIsSetOff += alarmsBeenSetOffMate;
        GameManager.informGuardCautionIsOn += cautionIsOnMate;
        GameManager.informGuardReturnToNormal += returnToNormalMate;

        WaypointInfo[] waypoints = new WaypointInfo[pathHolder.childCount];
        for (int i = 0; i < waypoints.Length; i++) {
            waypoints[i] = new WaypointInfo(pathHolder.GetChild(i).position, pathHolder.GetChild(i).gameObject.GetComponent<Waypoint>().desiredViewAngle, pathHolder.GetChild(i).gameObject.GetComponent<Waypoint>().waitTime);
        }

        lookAroundCoroutine = LookAround();
        followPathCautionCoroutine = FollowPathCaution(waypoints);
        followPathCoroutine = FollowPath(waypoints);
        StartCoroutine(followPathCoroutine);
        isFollowNormalRunning = true;
    }

    void Update() {
        SpotPlayer();
    }

    void LateUpdate() {
        DrawFielOfView(nearViewMesh, (nearViewDst + currentViewDstPenalty));
        DrawFielOfView(farViewMesh, (farViewDst + currentViewDstPenalty));
        DrawFielOfView(detectViewMesh, detectViewDst);
    }

    void SpotPlayer() {
        if (!GameManagerStatic.gameManager.isCautionOn() && !GameManagerStatic.gameManager.isAlarmSetOff()) {

            if (CanSeePlayer((nearViewDst + currentViewDstPenalty)) || CanSeePlayer(detectViewDst)) {
                GameManagerStatic.gameManager.reportPlayerPosition();
                GameManagerStatic.gameManager.setOffAlarm();
            }
            else {
                if (CanSeePlayer(farViewDst + currentViewDstPenalty)) {
                    playerVisibleTimer += Time.deltaTime;
                }
                else {
                    playerVisibleTimer -= Time.deltaTime;
                }
                playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
                detectViewDst = (farViewDst + currentViewDstPenalty) * (playerVisibleTimer / timeToSpotPlayer < 1 ? playerVisibleTimer / timeToSpotPlayer : 1);
            }
        }
        else if (GameManagerStatic.gameManager.isCautionOn()) {

            if (CanSeePlayer((nearViewDst + currentViewDstPenalty)) || CanSeePlayer(detectViewDst)) {
                GameManagerStatic.gameManager.reportPlayerPosition();
                GameManagerStatic.gameManager.setOffAlarm();
            }
            else {
                if (CanSeePlayer(farViewDst + currentViewDstPenalty)) {
                    playerVisibleTimer += Time.deltaTime;
                    if (isLookingAround) {
                        StopCoroutine(lookAroundCoroutine);
                        isLookingAround = false;
                    }
                    if (isFollowCautionRunning) {
                        agent.isStopped = true;
                        StopCoroutine(followPathCautionCoroutine);
                        isFollowCautionRunning = false;
                    }
                }
                else {
                    playerVisibleTimer -= Time.deltaTime;
                    if (!isLookingAround) {
                        StartCoroutine(lookAroundCoroutine);
                        isLookingAround = true;
                    }
                    if (!isFollowCautionRunning) {
                        StartCoroutine(followPathCautionCoroutine);
                        isFollowCautionRunning = true;
                    }
                }
                playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
                detectViewDst = (farViewDst + currentViewDstPenalty) * (playerVisibleTimer / timeToSpotPlayer < 1 ? playerVisibleTimer / timeToSpotPlayer : 1);
            }
        }
        else if (!GameManagerStatic.gameManager.isSearchOn()) {
            if (CanSeePlayer(detectViewDst))
                GameManagerStatic.gameManager.reportPlayerPosition();

            agent.destination = GameManagerStatic.gameManager.getPlayerLocation();

            if (!agent.pathPending && agent.remainingDistance < 0.3f && !CanSeePlayer(detectViewDst))
                GameManagerStatic.gameManager.reportMissingPlayer();
        }
        else {
            if (CanSeePlayer(detectViewDst)) {
                GameManagerStatic.gameManager.setOffAlarm();
                GameManagerStatic.gameManager.reportPlayerPosition();
                agent.destination = GameManagerStatic.gameManager.getPlayerLocation();
            }
            else {
                if (!agent.pathPending && agent.remainingDistance < 0.3f && !CanSeePlayer(detectViewDst)) {
                    if (RandomPoint(transform.position, 10f, out chasePoint))
                        agent.destination = chasePoint;
                }
            }
        }
    }

    void noCorutinesRunning() {
        isFollowNormalRunning = false;
        isFollowCautionRunning = false;
        isLookingAround = false;
    }

    void alarmsBeenSetOffMate() {
        detectViewDst = farViewDst + GameManagerStatic.gameManager.getViewDstPenalty();
        playerVisibleTimer = timeToSpotPlayer;
        StopAllCoroutines();
        noCorutinesRunning();
        StartCoroutine(ResetViewAngle());;
        isLookingAround = false;
        agent.isStopped = false;
        agent.speed = guardSpeed + GameManagerStatic.gameManager.getGuardAlarmSpeedPenalty();
        agent.destination = GameManagerStatic.gameManager.getPlayerLocation();
        currentViewDstPenalty = GameManagerStatic.gameManager.getViewDstPenalty();
        currentAnglePenalty = GameManagerStatic.gameManager.getViewAnglePenalty();
    }

    void cautionIsOnMate() {
        StopAllCoroutines();
        noCorutinesRunning();
        StartCoroutine(followPathCautionCoroutine);
        isFollowCautionRunning = true;
        StartCoroutine(lookAroundCoroutine);
        isLookingAround = true;
        agent.speed = guardSpeed + GameManagerStatic.gameManager.getGuardCautionSpeedPenalty();
    }

    void returnToNormalMate() {
        StopAllCoroutines();
        noCorutinesRunning();
        StartCoroutine(ResetViewAngle());
        StartCoroutine(followPathCoroutine);
        isFollowNormalRunning = true;
        currentViewDstPenalty = 0f;
        currentAnglePenalty = 0f;
        agent.speed = guardSpeed;
    }

    bool CanSeePlayer(float dst) {
        if (GameManagerStatic.gameManager.isGameOver())
            return false;
        if (Vector3.Distance(transform.position, player.position) < dst) {
            if (viewAngle + currentAnglePenalty >= 360)
                return true;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (Mathf.Sign(Vector3.Dot(transform.right, dirToPlayer)) < 0)
                angleBetweenGuardAndPlayer = 360 - angleBetweenGuardAndPlayer;

            float leftRangeAngle = ((360 - ((viewAngle + currentAnglePenalty) / 2f)) + turnViewAngle) % 360;
            float rightRangeAngle = (((viewAngle + currentAnglePenalty) / 2f) + turnViewAngle) % 360;
            if (rightRangeAngle < 0)
                rightRangeAngle += 360;

            Debug.Log("Left: " + leftRangeAngle + " Right: " + rightRangeAngle + " Player: " + angleBetweenGuardAndPlayer);

            if (((leftRangeAngle > rightRangeAngle) && (angleBetweenGuardAndPlayer > leftRangeAngle || angleBetweenGuardAndPlayer < rightRangeAngle)) 
                || (leftRangeAngle < angleBetweenGuardAndPlayer && rightRangeAngle > angleBetweenGuardAndPlayer )) {
                if (!Physics.Linecast(transform.position, player.position, obstacleMask)) {
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator FollowPath(WaypointInfo[] waypoints) {
        isFollowNormalRunning = true;

        Vector3 targetWaypoint = waypoints[targetWaypointIndex].position;

        while (true) {
            agent.destination = targetWaypoint;
            agent.isStopped = false;
            if (!agent.pathPending && agent.remainingDistance < 0.3f) {
                agent.isStopped = true;
                yield return StartCoroutine(TurnToFace(waypoints[targetWaypointIndex].desiredViewAngle));
                yield return new WaitForSeconds(waypoints[targetWaypointIndex].waitTime);
                targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Length;
                targetWaypoint = waypoints[targetWaypointIndex].position;
                yield return StartCoroutine(TurnToFace(waypoints[targetWaypointIndex].position));
            }
            yield return null;
        }
    }

    IEnumerator FollowPathCaution(WaypointInfo[] waypoints) {
        isFollowNormalRunning = true;

        Vector3 targetWaypoint = waypoints[targetWaypointIndex].position;

        while (true) {
            agent.destination = targetWaypoint;
            agent.isStopped = false;
            if (!agent.pathPending && agent.remainingDistance < 0.3f) {
                agent.isStopped = true;
                targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Length;
                targetWaypoint = waypoints[targetWaypointIndex].position;
            }
            yield return null;
        }
    }

    IEnumerator TurnToFace(float targetAngle) {
        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f) {
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
            transform.eulerAngles = Vector3.up * angle;
            yield return null;
        }
    }
    
    IEnumerator TurnToFace(Vector3 lookTarget) {
        Vector3 dirToLookTarget = (lookTarget - transform.position).normalized;
        float targetAngle = 90 - Mathf.Atan2(dirToLookTarget.z, dirToLookTarget.x) * Mathf.Rad2Deg;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f) {
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
            transform.eulerAngles = Vector3.up * angle;
            yield return null;
        }
    }


    IEnumerator LookAround() {
        bool raise = true;
        while (true) {
            if (raise)
                turnViewAngle += 1;
            else
                turnViewAngle -= 1;
            if (turnViewAngle <= -90 || turnViewAngle >= 90)
                raise = !raise;
            yield return new WaitForSeconds(0.01f);
        }
    }

    IEnumerator ResetViewAngle() {
        while (turnViewAngle != 0) {
            if (turnViewAngle < 0)
                turnViewAngle += 1;
            else
                turnViewAngle -= 1;
            if (turnViewAngle <= 0.1f && turnViewAngle >= -0.1f)
                turnViewAngle = 0;
            yield return new WaitForSeconds(0.01f);
        }
        yield break;
    }

    void DrawFielOfView(Mesh viewMesh, float viewDst) {
        int stepCount = Mathf.RoundToInt((viewAngle + currentAnglePenalty) * meshResolution);
        float stepAngleSize = (viewAngle + currentAnglePenalty) / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++) {
            float angle = transform.eulerAngles.y + turnViewAngle - (viewAngle + currentAnglePenalty) / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle, viewDst);

            if (i > 0) {
                bool edgeDstThersholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshhold;
                if (oldViewCast.hit != newViewCast.hit || oldViewCast.hit && newViewCast.hit && edgeDstThersholdExceeded) {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast, viewDst);
                    if (edge.pointA != Vector3.zero) {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero) {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++) {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < vertexCount - 2) {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast, float viewDst) {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++) {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle, viewDst);

            bool edgeDstThersholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshhold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThersholdExceeded) {
                minAngle = angle;
                minPoint = newViewCast.point;
            } else {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    ViewCastInfo ViewCast(float globalAngle, float viewDst) {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir, out hit, viewDst, obstacleMask)) {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        } else {
            return new ViewCastInfo(false, transform.position + dir * viewDst, viewDst, globalAngle);
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal) {
        if (!angleIsGlobal) {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    void OnDrawGizmos() {
        Vector3 startPosition = pathHolder.GetChild(0).position;
        Vector3 previousPosition = startPosition;
        foreach (Transform waypoint in pathHolder) {
            Gizmos.DrawSphere(waypoint.position, .3f);
            Gizmos.DrawLine(previousPosition, waypoint.position);
            Gizmos.DrawRay(waypoint.position, new Vector3(Mathf.Sin(waypoint.GetComponent<Waypoint>().desiredViewAngle * Mathf.Deg2Rad), 0, Mathf.Cos(waypoint.GetComponent<Waypoint>().desiredViewAngle * Mathf.Deg2Rad)));
            previousPosition = waypoint.position;
        } 
        Gizmos.DrawLine(previousPosition, startPosition);

    }

    public struct ViewCastInfo {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle) {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB) {
            pointA = _pointA;
            pointB = _pointB;
        }
    }


    public struct WaypointInfo{
        public Vector3 position;
        public float desiredViewAngle;
        public float waitTime;

        public WaypointInfo(Vector3 _position, float _desiredViewAngle, float _waitTime) {
            position = _position;
            desiredViewAngle = _desiredViewAngle;
            waitTime = _waitTime;
        }

    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result) {
        for (int i = 0; i < 30; i++) {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas)) {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

}
