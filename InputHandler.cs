using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Content
{
    public GameObject GameObject;
    public string Tag;
    public string Name;
}

public class InputHandler : MonoBehaviour
{

    private float TempPos = 0f;

    public float MaxAngleZoom = 89;
    public float MaxDistanceZoom = 100;

    private bool Centering = false;

    private Vector3 CurrentVelocity;

    private Vector3 CamDirection;
    private float CamVelocity;
    private Vector3 CamEasingTarget;

    private bool HoldTime = false;

    private List<Content> MarkedObjects = new List<Content>();
    private GameObject MarkedObject = null;

    private Vector3 Center;

    public Camera Cam;
    private Vector3 Target;
    public float RotationSpeed = 8f;
    public float MoveSpeed = 2.5f;

    private float TapTime = 0.1f;
    public float ZoomSpeed = 4f;

    private float Distance;

    private float RotationY = 0f;
    private float RotationX = 0f;

    private float PositionX = 0f;
    private float PositionZ = 0f;

    private GameObject HitObject = null;
    private bool Tap = false;

    private Quaternion ToRotation;
    private Vector3 NegDistance;
    private Vector3 ToPosition;

    public Material DefaultMaterial;
    public Material MarkedMaterial;

    private Vector3 velocity = new Vector3(0f, 0f, 0f);
    private float smoothTime = 0.2f;

    void Start()
    {
        Cam = GetComponentInChildren<Camera>();
        Vector3 Angles = transform.eulerAngles;
        RotationY = (RotationY == 0) ? Angles.y : RotationY;
        RotationX = Angles.x;

        Distance = (Cam.transform.position - Target).magnitude;
        Distance = ClampAngle(Distance, 10, MaxDistanceZoom);
    }

    void Update()
    {
        if (Centering)
        {
            Cam.transform.position = Vector3.SmoothDamp(Cam.transform.position, ToPosition, ref velocity, smoothTime);

            float Differ = (Cam.transform.position - ToPosition).magnitude;
            if (Differ < 0.1)
                Centering = false;
        }

        else
        {
            RaycastHit hit;
            var cameraCenter = Cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, Cam.nearClipPlane));
            if (Physics.Raycast(cameraCenter, this.transform.forward, out hit, 1000))
            {
                Target = hit.point;
                Distance = (Cam.transform.position - Target).magnitude;
            }

            if (Input.touchCount == 2)
            {
                Touch TouchZero = Input.GetTouch(0);
                Touch TouchOne = Input.GetTouch(1);
                CurrentVelocity = new Vector3(0f, 0f, 0f);
                Cam.GetComponent<Rigidbody>().velocity = CurrentVelocity;

                if (TouchZero.phase == TouchPhase.Moved && TouchOne.phase == TouchPhase.Moved)
                {
                    Rotate(TouchZero, TouchOne);
                    Zoom(TouchZero, TouchOne);
                }

                if (TouchZero.phase == TouchPhase.Stationary && TouchOne.phase == TouchPhase.Stationary)
                {
                    StartCoroutine(HoldTap(TouchZero, TouchOne));
                }
            }

            if (Input.touchCount == 1)
            {
                Touch Touch = Input.GetTouch(0);

                if (Touch.phase == TouchPhase.Began)
                {
                    Cam.GetComponent<Rigidbody>().velocity = new Vector3(0f, 0f, 0f);
                    Tap = true;
                    StartCoroutine(TapID(Touch));
                    StartCoroutine(DoubleTapID(Touch));
                    StartCoroutine(HoldID());

                    TempPos = Touch.position.magnitude;
                }

                if (Touch.phase == TouchPhase.Moved)
                {
                    Move(Touch);
                    Tap = false;
                    HoldTime = false;
                    CurrentVelocity = Cam.velocity;
                }

                if (Touch.phase == TouchPhase.Ended)
                {
                    Tap = false;
                    if (Mathf.Abs(TempPos - Touch.position.magnitude) > 50)
                        Cam.GetComponent<Rigidbody>().AddForce(CurrentVelocity * 1000);
                    if (HoldTime)
                        Hold(Touch);
                }
            }
        }
    }

    void Zoom(Touch TouchZero, Touch TouchOne)
    {
        Vector2 TouchZeroPrev = TouchZero.position - TouchZero.deltaPosition;
        Vector2 TouchOnePrev = TouchOne.position - TouchOne.deltaPosition;

        float Multiplier = (MaxAngleZoom - 10) / (MaxDistanceZoom - 10);

        float TouchPrev = (TouchZeroPrev - TouchOnePrev).magnitude;
        float TouchNow = (TouchZero.position - TouchOne.position).magnitude;

        float DeltaPosition = TouchPrev - TouchNow;
        Distance += DeltaPosition * ZoomSpeed * Time.deltaTime;
        Distance = ClampAngle(Distance, 10, MaxDistanceZoom);

        RotationX += DeltaPosition * ZoomSpeed * Time.deltaTime * Multiplier;
        RotationX = ClampAngle(RotationX, 10, MaxAngleZoom);

        ToRotation = Quaternion.Euler(RotationX, RotationY, 0);
        NegDistance = new Vector3(0.0f, 0.0f, -Distance);

        ToPosition = ToRotation * NegDistance + Target;

        ToRotation = Quaternion.Euler(RotationX, RotationY, 0);
        Cam.transform.rotation = ToRotation;
        Cam.transform.position = ToPosition;
    }

    IEnumerator TapID(Touch Touch)
    {
        yield return new WaitForSeconds(TapTime);
        if (!Tap && Touch.tapCount == 1 && Input.touchCount == 0)
        {
            Ray Raycast = Camera.main.ScreenPointToRay(Touch.position);
            RaycastHit HitCollider;

            if (Physics.Raycast(Raycast, out HitCollider))
            {
                HitObject = HitCollider.transform.gameObject;

                if (HitObject.CompareTag("Markable"))
                {
                    if (MarkedObject != null && MarkedObject != HitObject)
                    {
                        for (int i = 0; i < MarkedObjects.Count; i++)
                            ColorDefault(MarkedObjects[i].GameObject);
                        MarkedObjects.Clear();
                        MarkingObject(HitObject);

                        ColorMarked(HitObject);
                        MarkedObject = HitObject;
                    }

                    else if (MarkedObject == HitObject)
                    {
                        for (int i = 0; i < MarkedObjects.Count; i++)
                            ColorDefault(MarkedObjects[i].GameObject);
                        MarkedObjects.Clear();
                        MarkedObject = null;
                    }

                    else if (MarkedObject == null)
                    {
                        MarkingObject(HitObject);
                        ColorMarked(HitObject);
                        MarkedObject = HitObject;
                    }
                }

                if (HitObject.CompareTag("Ground"))
                {
                    MarkedObjects.Clear();

                    ColorDefault(MarkedObject);
                    MarkedObject = null;
                }
            }
        }
    }

    void ColorDefault(GameObject Object)
    {
        Object.GetComponent<Renderer>().material = DefaultMaterial;
    }

    void ColorMarked(GameObject Object)
    {
        Object.GetComponent<Renderer>().material = MarkedMaterial;
    }

    IEnumerator DoubleTapID(Touch Touch)
    {
        yield return new WaitForSeconds(TapTime * 2);
        if (!Tap && Touch.tapCount == 2 && Input.touchCount == 0)
        {
            Ray Raycast = Camera.main.ScreenPointToRay(Touch.position);
            RaycastHit HitCollider;

            if (Physics.Raycast(Raycast, out HitCollider))
            {
                HitObject = HitCollider.transform.gameObject;
                if (HitObject.CompareTag("Markable"))
                {
                    ToPosition = ToRotation * NegDistance + HitObject.transform.position;
                    Centering = true;
                }
            }
        }
    }

    void Move(Touch Touch)
    {
        Vector3 VectorForward = new Vector3(Cam.transform.forward.x, 0f, Cam.transform.forward.z).normalized;
        Vector3 VectorUpward = new Vector3(0f, 1f, 0f);

        Vector3 VectorSideways = Vector3.Cross(VectorForward, VectorUpward).normalized;

        PositionX = Touch.deltaPosition.x * Time.deltaTime * MoveSpeed;
        PositionZ = Touch.deltaPosition.y * Time.deltaTime * MoveSpeed;

        Vector3 ConY = VectorForward * PositionZ;
        Vector3 ConX = VectorSideways * PositionX;

        Cam.transform.position -= ConY - ConX;
    }

    void Rotate(Touch TouchZero, Touch TouchOne)
    {
        Touch TouchLeft;
        Touch TouchRight;
        float Mediana;

        if (TouchZero.position.x <= TouchOne.position.x)
        {
            TouchLeft = TouchZero;
            TouchRight = TouchOne;
        }
        else
        {
            TouchLeft = TouchOne;
            TouchRight = TouchZero;
        }

        float DeltaTouchLeft = TouchLeft.deltaPosition.y;
        float DeltaTouchRight = TouchRight.deltaPosition.y;

        if (DeltaTouchLeft < 0 && DeltaTouchRight > 0)
        {
            Mediana = (DeltaTouchRight - DeltaTouchLeft) / 2;
            RotationY += Mediana * Time.deltaTime * RotationSpeed;
        }

        if (DeltaTouchRight < 0 && DeltaTouchLeft > 0)
        {
            Mediana = (DeltaTouchLeft - DeltaTouchRight) / 2;
            RotationY -= Mediana * Time.deltaTime * RotationSpeed;
        }

        ToRotation = Quaternion.Euler(RotationX, RotationY, 0);
        NegDistance = new Vector3(0.0f, 0.0f, -Distance);

        ToPosition = ToRotation * NegDistance + Target;

        Cam.transform.rotation = ToRotation;
        Cam.transform.position = ToPosition;
    }

    IEnumerator HoldID()
    {
        yield return new WaitForSeconds(0.3f);
        Touch Touch = Input.GetTouch(0);
        if (Touch.phase == TouchPhase.Stationary && Input.touchCount == 1 && Touch.tapCount == 1)
            HoldTime = true;
    }

    void Hold(Touch Touch)
    {
        Ray Raycast = Camera.main.ScreenPointToRay(Touch.position);
        RaycastHit HitCollider;
        HoldTime = false;

        if (Physics.Raycast(Raycast, out HitCollider))
        {
            HitObject = HitCollider.transform.gameObject;
            if (HitObject != null && HitObject.CompareTag("Markable"))
                StartCoroutine(ShowInfo(HitObject));
        }
    }

    IEnumerator ShowInfo(GameObject HitObject)
    {
        GameObject ObjectInfo = HitObject.transform.Find("Info").gameObject;
        ObjectInfo.transform.rotation = Cam.transform.rotation;
        ObjectInfo.SetActive(true);

        yield return new WaitForSeconds(3f);
        HitObject.transform.Find("Info").gameObject.SetActive(false);
    }

    public static float ClampAngle(float Angle, float Min, float Max)
    {
        if (Angle < -360F)
            Angle += 360F;

        if (Angle > 360F)
            Angle -= 360F;

        return Mathf.Clamp(Angle, Min, Max);
    }

    IEnumerator HoldTap(Touch TouchZero, Touch TouchOne)
    {
        Ray Raycast = Camera.main.ScreenPointToRay(TouchOne.position);
        RaycastHit HitCollider;
        yield return new WaitForSeconds(TapTime);

        if (Input.touchCount == 1)
        {
            if (Physics.Raycast(Raycast, out HitCollider))
            {
                HitObject = HitCollider.transform.gameObject;

                if (HitObject.CompareTag("Markable"))
                {
                    if (IsMarked(HitObject))
                    {
                        RemoveObject(HitObject);
                        ColorDefault(HitObject);
                    }

                    else
                    {
                        MarkingObject(HitObject);
                        ColorMarked(HitObject);
                    }
                }
            }
        }
    }

    bool IsMarked(GameObject HitObject)
    {
        bool Found = false;
        for (int i = 0; i < MarkedObjects.Count; i++)
            if (HitObject == MarkedObjects[i].GameObject)
                Found = true;

        if (Found)
            return true;
        else
            return false;
    }

    void RemoveObject(GameObject HitObject)
    {
        for (int i = 0; i < MarkedObjects.Count; i++)
            if (HitObject == MarkedObjects[i].GameObject)
                MarkedObjects.Remove(MarkedObjects[i]);
    }

    void MarkingObject(GameObject HitObject)
    {
        Content Object = new Content
        {
            GameObject = HitObject,
            Name = HitObject.name,
            Tag = HitObject.tag
        };
        MarkedObjects.Add(Object);
    }
}
