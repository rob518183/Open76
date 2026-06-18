using UnityEngine;

namespace Assets.Scripts.CarSystems
{
    public class RaySusp : MonoBehaviour
    {
        public float SpringLength;
        public bool Grounded;
        public float SpringConstant;
        public float Damping;
        public float WheelRadius;
        private float _lastSpringLength;
        private Rigidbody _rigidbody;
        private Transform _wheelGraphic;
        private TrailRenderer _skidTrail;

        public float TargetAngle;

        // Use this for initialization
        private void Start()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            _wheelGraphic = transform.Find("Mesh");
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            SpringPhysics();

            Vector3 euler = _wheelGraphic.localRotation.eulerAngles;
            euler.y = TargetAngle; // Mathf.Lerp(euler.y, TargetAngle, Time.deltaTime * 2);
            _wheelGraphic.localRotation = Quaternion.Euler(euler);
        }

        public void StartSkid()
        {
            if (_skidTrail != null) return;
            _skidTrail = _wheelGraphic.gameObject.GetComponent<TrailRenderer>();
            if (_skidTrail == null)
            {
                _skidTrail = _wheelGraphic.gameObject.AddComponent<TrailRenderer>();
                _skidTrail.time = 3.0f;
                _skidTrail.startWidth = 0.25f;
                _skidTrail.endWidth = 0.05f;
                _skidTrail.autodestruct = false;
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = Color.black;
                _skidTrail.material = mat;
            }
            _skidTrail.emitting = true;
        }

        public void StopSkid()
        {
            if (_skidTrail == null) return;
            _skidTrail.emitting = false;
        }


        private void SpringPhysics()
        {
            float springNow = SpringLength;
            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit rayHit, SpringLength))
            {
                springNow = rayHit.distance; // * Random.Range(0.7f, 1.0f);  //for rough terrain
                Grounded = true;
            }
            else
            {
                Grounded = false;
            }

            // Mathf.Max(0, springNow * (1.0f - Pressure));

            float displacement = SpringLength - springNow;
            Vector3 force = transform.up * SpringConstant * displacement;

            float springVel = springNow - _lastSpringLength;
            Vector3 wheelVel = springVel * transform.up;
            //Debug.DrawLine(transform.position, transform.position + wheelVel * 10, Color.yellow);
            Vector3 damper = -Damping * wheelVel;
            force += damper;

            _rigidbody.AddForceAtPosition(force, transform.position, ForceMode.Force);
            //Debug.DrawLine(transform.position, transform.position + force, Color.red);
            _lastSpringLength = springNow;

            Vector3 pos = _wheelGraphic.localPosition;
            pos.y = -_lastSpringLength + WheelRadius;
            _wheelGraphic.localPosition = pos;
        }

        public void SetWheelVisibile(bool visible)
        {
            _wheelGraphic.gameObject.SetActive(visible);
        }
    }
}