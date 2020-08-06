using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFloat : MonoBehaviour
{
    public float AirDrag = 1;
    public float WaterDrag = 10;
    public bool AttachToSurface = false;
    public Transform[] FloatPoints;

    private Rigidbody _rigidbody;
    private Waves _waves;

    // water line
    private float _waterLine;
    private Vector3[] _waterLinePoints;

    // help vectors
    private Vector3 _centerOffset;
    private Vector3 _smoothVectorRotation;
    private Vector3 _targetUp;

    private Vector3 Center => transform.position + _centerOffset;

    void Awake()
    {
        _waves = FindObjectOfType<Waves>();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;

        _waterLinePoints = new Vector3[FloatPoints.Length];
        for(int i = 0; i < FloatPoints.Length; i++)
            _waterLinePoints[i] = FloatPoints[i].position;
        _centerOffset = PhysicsHelper.GetCenter(_waterLinePoints) - transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        var newWaterLine = 0f;
        var pointUnderWater = false;

        // set heights
        for(int i = 0; i<FloatPoints.Length; i++)
        {
            _waterLinePoints[i] = FloatPoints[i].position;
            _waterLinePoints[i].y = _waves.GetHeight(FloatPoints[i].position);

            newWaterLine += _waterLinePoints[i].y /FloatPoints.Length;
            
            if(_waterLinePoints[i].y > FloatPoints[i].position.y)
                pointUnderWater = true;
        }

        var waterLineDelta = newWaterLine - _waterLine;
        _waterLine = newWaterLine;

        // gravity
        var gravity = Physics.gravity;
        _rigidbody.drag = AirDrag;
        if(_waterLine > Center.y)
        {
            // 'underwater'
            _rigidbody.drag = WaterDrag;
            if(AttachToSurface)
            {
                // attach to the surface
                _rigidbody.position = new Vector3(_rigidbody.position.x, _waterLine - _centerOffset.y, _rigidbody.position.z);
            }
            else
            {
                // apply bounacy
                gravity = -gravity;
                transform.Translate(Vector3.up * waterLineDelta * 0.9f);    // speed up bouancy a little
            }
        }
        _rigidbody.AddForce(gravity * Mathf.Clamp(Mathf.Abs(_waterLine - Center.y), 0, 1));  // scale gravity to have less 'effect' when hovering around water line

        // calculate up vector 
        _targetUp = PhysicsHelper.GetNormal(_waterLinePoints);

        // rotation
        if(pointUnderWater)
        {
            //attach to water surface
            _targetUp = Vector3.SmoothDamp(transform.up, _targetUp, ref _smoothVectorRotation, 10f * Time.deltaTime);
            _rigidbody.rotation = Quaternion.FromToRotation(transform.up, _targetUp) * _rigidbody.rotation;
        }

    }

    void OnDrawGizmos()
    {
        if(FloatPoints == null)
            return;

        // float
        for(var i = 0; i < FloatPoints.Length; i++)
        {
            if(FloatPoints[i] == null)
                continue;

            // cube at prediceded water height
            if(_waves != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(_waterLinePoints[i], Vector3.one * 0.1f);
            }

            // sphere at float point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(FloatPoints[i].position, 0.05f);
        }

        // center / waterline
        if(Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(new Vector3(Center.x, _waterLine, Center.z), Vector3.one * 0.3f);
        }

    }
}
