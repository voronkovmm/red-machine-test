using Camera;
using Events;
using Player.ActionHandlers;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static Events.EventModels.Game;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private EnumMovementType movementType;
    [SerializeField] private EnumFunctionType functionType;

    [Space(10)]
    [SerializeField] private float duration = 3;
    [SerializeField] private Vector2 borders = new(5, 5);

    private bool isMovement;
    private bool isNodeTapped;
    private Vector3 endClickPoint;
    private Vector3 startClickPoint;
    private Coroutine coroutineMove;
    private Transform cameraTransform;

    public enum EnumMovementType { RELEASE, DRAG }
    public enum EnumFunctionType { LERP, SMOOTHSTEP }

    private float dragDuration => duration * 0.03f;

    private void Start()
    {
        this.cameraTransform = CameraHolder.Instance.MainCamera.transform;

        ClickHandler clickHandler = ClickHandler.Instance;
        clickHandler.DragStartEvent += OnDragStart;
        clickHandler.DragEndEvent   += OnDragEnd;
        clickHandler.DragEvent      += OnDrag;

        EventsController.Subscribe<NodeTapped>(this, OnNodeTapped);
    }

    private void OnNodeTapped(NodeTapped tapped) => isNodeTapped = true;

    private void OnDragEnd(Vector3 vector)
    {
        endClickPoint = vector;

        if (movementType != EnumMovementType.RELEASE || !isMovement)
            return;

        if (coroutineMove != null)
            StopCoroutine(coroutineMove);

        coroutineMove = StartCoroutine(MoveReleaseRoutine());
    }
    private void OnDragStart(Vector3 vector)
    {
        isMovement = !EventSystem.current.IsPointerOverGameObject() && !isNodeTapped;
        isNodeTapped = false;

        startClickPoint = vector;
    }
    private void OnDrag(Vector3 vector)
    {
        endClickPoint = vector;

        if (movementType != EnumMovementType.DRAG || coroutineMove != null || !isMovement)
            return;

        coroutineMove = StartCoroutine(MoveDragRoutine());
    }

    private IEnumerator MoveReleaseRoutine()
    {
        Vector3 cameraPos = cameraTransform.position;

        float distance = Vector3.Distance(endClickPoint, startClickPoint);
        Vector3 dirNormalInvert = (endClickPoint - startClickPoint).normalized * -1;

        Vector3 targetPosition = cameraPos + dirNormalInvert * distance;
        targetPosition.x = Mathf.Clamp(targetPosition.x, -borders.x, borders.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -borders.y, borders.y);
        targetPosition.z = cameraPos.z;

        float elapsedTime = 0.0f;

        while ((elapsedTime += Time.deltaTime) < duration)
        {
            float interpolation = elapsedTime / duration;

            if (functionType == EnumFunctionType.SMOOTHSTEP)
            {
                Vector3 pos = cameraTransform.position;
                float smoothedX = Mathf.SmoothStep(pos.x, targetPosition.x, interpolation);
                float smoothedY = Mathf.SmoothStep(pos.y, targetPosition.y, interpolation);
                cameraTransform.position = new(smoothedX, smoothedY, targetPosition.z);
            }
            else if (functionType == EnumFunctionType.LERP)
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, interpolation);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = targetPosition;

        coroutineMove = null;
    }
    private IEnumerator MoveDragRoutine()
    {

        Vector3 velocity = Vector3.zero;
        float z = cameraTransform.position.z;

        while (Input.GetMouseButton(0))
        {
            Vector3 cameraPos = cameraTransform.position;
            Vector3 dirNormalInvert = (endClickPoint - startClickPoint).normalized * -1;
            float distance = Vector3.Distance(endClickPoint, startClickPoint);

            Vector3 targetPosition = cameraPos + dirNormalInvert * distance;
            targetPosition.x = Mathf.Clamp(targetPosition.x, -borders.x, borders.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, -borders.y, borders.y);
            targetPosition.z = z;

            cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, targetPosition, ref velocity, dragDuration);

            yield return null;
        }

        coroutineMove = null;
    }
}