using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Camera;
using Assets.Scripts.CarSystems;
using Assets.Scripts.CarSystems.Ui;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Entities
{
    public enum DamageType
    {
        Projectile,
        Force
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CarPhysics))]
    public class Car : WorldEntity
    {
        public static bool FireWeapons;

        // Constants
        private const int VehicleStartHealth = 550;
        private const int CoreStartHealth = 250;
        private const int TireStartHealth = 100;

        // Components & References
        private Transform _transform;
        private Rigidbody _rigidBody;
        private CarPhysics _carPhysics;
        private CameraController _camera;
        private AudioSource _engineStartSound;
        private AudioSource _engineLoopSound;
        private Transform _thirdPersonChassis;

        // Controllers
        public WeaponsController WeaponsController;
        public SpecialsController SpecialsController;
        public SystemsPanel SystemsPanel;
        public GearPanel GearPanel;
        public RadarPanel RadarPanel;
        public CarAI AI { get; private set; }
        private CompassPanel _compassPanel;

        // State Data
        private int _vehicleHealthGroups;
        private int _currentVehicleHealthGroup;
        private bool _engineStarting;
        private float _engineStartTimer;
        
        // Health Data
        private int[] _vehicleHitPoints;
        private int[] _vehicleStartHitPoints;
        private Dictionary<int, List<Transform>> _visualDamageParts; // Cached visual parts

        // Properties
        public bool EngineRunning { get; private set; }
        public bool Arrived { get; set; }
        public Vdf Vdf { get; private set; }
        public Vcf Vcf { get; private set; }
        public bool Attacked { get; private set; }
        public int TeamId { get; set; }
        public bool IsPlayer { get; set; }
        public int Skill1 { get; set; }
        public int Skill2 { get; set; }
        public int Aggressiveness { get; set; }

        public override bool Alive
        {
            get { return GetComponentHealth(SystemType.Vehicle) > 0; }
        }

        private void Awake()
        {
            _transform = transform;
            _rigidBody = GetComponent<Rigidbody>();
            _carPhysics = GetComponent<CarPhysics>();
            
            // Safety check for Camera Manager
            if (CameraManager.Instance.MainCamera != null)
            {
                _camera = CameraManager.Instance.MainCamera.GetComponent<CameraController>();
            }

            AI = new CarAI(this);
            EngineRunning = true;
            _currentVehicleHealthGroup = 1;
            _visualDamageParts = new Dictionary<int, List<Transform>>();
        }

        private void Start()
        {
            EntityManager.Instance.RegisterCar(this);
            UpdateEngineSounds();

            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
            WeaponsController = new WeaponsController(this, Vcf, firstPersonTransform);
            SpecialsController = new SpecialsController(Vcf, firstPersonTransform);
        }

        public void Configure(Vdf vdf, Vcf vcf)
        {
            Vdf = vdf;
            Vcf = vcf;

            _vehicleHealthGroups = Vdf.PartsThirdPerson.Count;

            // Initialize Health
            InitializeHealthData();

            // Cache Visuals
            CacheDamageVisuals();
        }

        private void InitializeHealthData()
        {
            int systemCount = (int)SystemType.TotalSystems;
            _vehicleHitPoints = new int[systemCount];
            _vehicleStartHitPoints = new int[systemCount];

            // Core
            _vehicleStartHitPoints[(int)SystemType.Vehicle] = VehicleStartHealth;
            _vehicleStartHitPoints[(int)SystemType.Suspension] = CoreStartHealth;
            _vehicleStartHitPoints[(int)SystemType.Brakes] = CoreStartHealth;
            _vehicleStartHitPoints[(int)SystemType.Engine] = CoreStartHealth;

            // Armor
            _vehicleStartHitPoints[(int)SystemType.FrontArmor] = (int)Vcf.ArmorFront;
            _vehicleStartHitPoints[(int)SystemType.RightArmor] = (int)Vcf.ArmorRight;
            _vehicleStartHitPoints[(int)SystemType.BackArmor] = (int)Vcf.ArmorRear;
            _vehicleStartHitPoints[(int)SystemType.LeftArmor] = (int)Vcf.ArmorLeft;

            // Chassis
            _vehicleStartHitPoints[(int)SystemType.FrontChassis] = (int)Vcf.ChassisFront;
            _vehicleStartHitPoints[(int)SystemType.RightChassis] = (int)Vcf.ChassisRight;
            _vehicleStartHitPoints[(int)SystemType.BackChassis] = (int)Vcf.ChassisRear;
            _vehicleStartHitPoints[(int)SystemType.LeftChassis] = (int)Vcf.ChassisLeft;

            // Tires
            _vehicleStartHitPoints[(int)SystemType.TireFL] = TireStartHealth;
            _vehicleStartHitPoints[(int)SystemType.TireFR] = TireStartHealth;
            _vehicleStartHitPoints[(int)SystemType.TireBL] = TireStartHealth;
            _vehicleStartHitPoints[(int)SystemType.TireBR] = TireStartHealth;

            // Reset current HP to max
            Array.Copy(_vehicleStartHitPoints, _vehicleHitPoints, systemCount);
        }

        private void CacheDamageVisuals()
        {
            _thirdPersonChassis = transform.Find("Chassis/ThirdPerson");
            if (_thirdPersonChassis == null) return;

            // We expect child objects named "Health 0", "Health 1", etc.
            // We group them here so we don't have to .Find() them during combat
            for (int i = 0; i < _vehicleHealthGroups; ++i)
            {
                var part = _thirdPersonChassis.Find("Health " + i);
                if (part != null)
                {
                    // If you have multiple parts per group, logic goes here. 
                    // Assuming 1:1 mapping based on original code.
                    if (!_visualDamageParts.ContainsKey(i))
                        _visualDamageParts[i] = new List<Transform>();
                    
                    _visualDamageParts[i].Add(part);
                }
            }
        }

        public void InitPanels()
        {
            Transform firstPersonTransform = _transform.Find("Chassis/FirstPerson");
            if (firstPersonTransform == null) return;

            SystemsPanel = new SystemsPanel(firstPersonTransform);
            GearPanel = new GearPanel(firstPersonTransform);
            _compassPanel = new CompassPanel(firstPersonTransform);
            RadarPanel = new RadarPanel(this, firstPersonTransform);
        }

        private void Update()
        {
            if (!Alive) return;

            HandleEngineAudio();
            HandleUIUpdates();
            HandleAIAndWeapons();
        }

        private void HandleEngineAudio()
        {
            if (EngineRunning && _engineLoopSound != null)
            {
                // Replaced 'while' loop with math for safety and performance
                const float firstGearTopSpeed = 40f;
                const float gearRatioAdjustment = 1.5f;
                const float minPitch = 0.6f;
                const float maxPitch = 1.2f;

                // Unity 6 uses linearVelocity, older uses velocity. 
                // Using velocity for LTS compatibility.
                float velocity = _rigidBody.linearVelocity.magnitude; 
                
                float currentGearMax = firstGearTopSpeed;
                
                // Simulate gear shifting mathematically
                // If velocity > gearMax, we are in higher gears
                while (velocity > currentGearMax && currentGearMax < 200f) // Cap at 200 to prevent infinite
                {
                    currentGearMax *= gearRatioAdjustment;
                }

                // Calculate ratio within current gear
                float gearStartSpeed = currentGearMax / gearRatioAdjustment;
                float ratio = (velocity - gearStartSpeed) / (currentGearMax - gearStartSpeed);
                // Clamp ratio for safety (0 to 1)
                ratio = Mathf.Clamp01(Mathf.Max(0, (velocity - (currentGearMax/gearRatioAdjustment)) / (currentGearMax - (currentGearMax/gearRatioAdjustment))));
                
                // Simplified pitch logic
                float enginePitch = Mathf.Lerp(minPitch, maxPitch, (velocity / currentGearMax));
                _engineLoopSound.pitch = enginePitch;
            }

            if (_engineStarting)
            {
                _engineStartTimer += Time.deltaTime;
                if (_engineStartTimer > _engineStartSound.clip.length - 0.5f)
                {
                    _engineLoopSound.Play();
                    EngineRunning = true;
                    _engineStarting = false;
                    _engineStartTimer = 0f;
                }
            }
        }

        private void HandleUIUpdates()
        {
            if (_camera != null && _camera.FirstPerson)
            {
                if (_compassPanel != null)
                    _compassPanel.UpdateCompassHeading(_transform.eulerAngles.y);
            }

            if (RadarPanel != null)
                RadarPanel.Update();
        }

        private void HandleAIAndWeapons()
        {
            bool mainCameraActive = CameraManager.Instance.IsMainCameraActive;

            if (!IsPlayer || !mainCameraActive)
            {
                AI.Navigate();
            }

            if (!IsPlayer && FireWeapons && WeaponsController != null)
            {
                WeaponsController.Fire(0);
            }
        }

        // --- Health & Damage System ---

        public override void ApplyDamage(DamageType damageType, Vector3 normal, int damageAmount)
        {
            // Determine quadrant based on angle
            float angle = Quaternion.FromToRotation(Vector3.up, normal).eulerAngles.z;
            SystemType targetSystem = GetSystemFromAngle(angle, damageType);

            int currentHealth = GetComponentHealth(targetSystem);
            SetComponentHealth(targetSystem, currentHealth - damageAmount);
        }

        private SystemType GetSystemFromAngle(float angle, DamageType type)
        {
            // Normalize angle to 0-360
            if (angle < 0) angle += 360f;

            // Quadrant Logic
            // Front: 315 - 45
            // Right: 45 - 135
            // Back:  135 - 225
            // Left:  225 - 315

            bool isRight = angle >= 45f && angle < 135f;
            bool isBack = angle >= 135f && angle < 225f;
            bool isLeft = angle >= 225f && angle < 315f;
            
            if (type == DamageType.Force)
            {
                if (isRight) return SystemType.RightChassis;
                if (isBack) return SystemType.BackChassis;
                if (isLeft) return SystemType.LeftChassis;
                return SystemType.FrontChassis;
            }
            else // Projectile
            {
                if (isRight) return SystemType.RightArmor;
                if (isBack) return SystemType.BackArmor;
                if (isLeft) return SystemType.LeftArmor;
                return SystemType.FrontArmor;
            }
        }

        private int GetComponentHealth(SystemType healthType)
        {
            return _vehicleHitPoints[(int)healthType];
        }

        private void SetComponentHealth(SystemType system, int value)
        {
            if (!Alive) return;

            int sysIndex = (int)system;
            _vehicleHitPoints[sysIndex] = value;

            if (system == SystemType.Vehicle)
            {
                UpdateVisualHealthGroup(GetHealthGroup(system));
                if (value <= 0) Explode();
            }
            else
            {
                // Pass through damage to core if component destroyed
                if (_vehicleHitPoints[sysIndex] < 0)
                {
                    int overflowDamage = _vehicleHitPoints[sysIndex]; // This is negative
                    _vehicleHitPoints[sysIndex] = 0;

                    SystemType coreComponent = GetCoreComponent();
                    SetComponentHealth(coreComponent, GetComponentHealth(coreComponent) + overflowDamage);
                }
            }

            // Update UI
            if (SystemsPanel != null && system != SystemType.Vehicle)
            {
                SystemsPanel.SetSystemHealthGroup(system, GetHealthGroup(system), true);
            }
        }

        private void UpdateVisualHealthGroup(int healthGroupIndex)
        {
            if (_currentVehicleHealthGroup == healthGroupIndex) return;

            _currentVehicleHealthGroup = healthGroupIndex;

            // Optimized: Use cached dictionary instead of .Find()
            foreach (var kvp in _visualDamageParts)
            {
                int groupIndex = kvp.Key;
                bool isActive = (groupIndex == healthGroupIndex);
                
                foreach(var part in kvp.Value)
                {
                    if(part.gameObject.activeSelf != isActive)
                        part.gameObject.SetActive(isActive);
                }
            }
        }

        private int GetHealthGroup(SystemType system)
        {
            int sysIndex = (int)system;
            int startHealth = _vehicleStartHitPoints[sysIndex];
            int currentHealth = _vehicleHitPoints[sysIndex];

            // If dead, return the last group (most damaged)
            int healthGroupCount = (system == SystemType.Vehicle) ? _vehicleHealthGroups : 5;
            
            if (currentHealth <= 0) return healthGroupCount - 1;

            // Calculate percentage
            float pct = (float)currentHealth / startHealth;
            // Map percentage to group index (inverted: 100% is group 0, 0% is group MAX)
            int group = Mathf.CeilToInt(pct * (healthGroupCount - 1));
            
            return (healthGroupCount - 1) - group;
        }

        private SystemType GetCoreComponent()
        {
            SystemType[] coreSystems = {
                SystemType.Vehicle, SystemType.Brakes, SystemType.Engine, SystemType.Suspension
            };

            // Filter for only alive systems to avoid infinite loops
            var aliveSystems = coreSystems.Where(s => GetComponentHealth(s) > 0).ToList();

            if (aliveSystems.Count > 0)
            {
                return aliveSystems[Random.Range(0, aliveSystems.Count)];
            }

            return SystemType.Vehicle; // Fallback
        }

        // --- Core Mechanics ---

        public void ToggleEngine()
        {
            if (_engineStartSound == null || _engineStartSound.isPlaying) return;

            if (EngineRunning)
            {
                _engineLoopSound.Stop();
                EngineRunning = false;
            }
            else
            {
                _engineStartSound.Play();
                _engineStarting = true;
                _engineStartTimer = 0f;
            }
        }

        private void Explode()
        {
            _rigidBody.AddForce(Vector3.up * _rigidBody.mass * 5f, ForceMode.Impulse);

            CarInput carInput = GetComponent<CarInput>();
            if (carInput != null) Destroy(carInput);

            AudioSource explosion = CacheManager.Instance.GetAudioSource(gameObject, "xcar");
            if (explosion != null)
            {
                explosion.volume = 0.9f;
                explosion.Play();
            }

            EngineRunning = false;
            if(_engineLoopSound) Destroy(_engineLoopSound);
            if(_engineStartSound) Destroy(_engineStartSound);

            // Cleanup Components
            if(_carPhysics) Destroy(_carPhysics);
            _carPhysics = null;
            AI = null;
            WeaponsController = null;
            SpecialsController = null;
            SystemsPanel = null;
            RadarPanel = null;
            
            // Visual Detachment (Using Find here is acceptable as it happens once on death)
            DetachPart("FrontLeft");
            DetachPart("FrontRight");
            DetachPart("BackLeft");
            DetachPart("BackRight");
        }

        private void DetachPart(string partName)
        {
            Transform t = transform.Find(partName);
            if (t != null) Destroy(t.gameObject);
        }

        public void Kill()
        {
            SetComponentHealth(SystemType.Vehicle, 0);
        }

        public void Sit()
        {
            if(_carPhysics != null) _carPhysics.Brake = 1.0f;
            if(AI != null) AI.Sit();
        }

        // --- AI Delegates ---

        public void SetSpeed(int targetSpeed)
        {
            _rigidBody.linearVelocity = _transform.forward * targetSpeed;
        }

        public bool AtFollowTarget() => AI != null && AI.AtFollowTarget();
        public void SetFollowTarget(Car targetCar, int xOffset, int targetSpeed) => AI?.SetFollowTarget(targetCar, xOffset, targetSpeed);
        public void SetTargetPath(FSMPath path, int targetSpeed) => AI?.SetTargetPath(path, targetSpeed);
        public bool IsWithinNav(FSMPath path, int distance) => AI != null && AI.IsWithinNav(path, distance);

        private void OnDrawGizmos()
        {
            if (AI != null) AI.DrawGizmos();
        }

        private void OnDestroy()
        {
            if(EntityManager.Instance != null)
                EntityManager.Instance.UnregisterCar(this);
        }

        private void UpdateEngineSounds()
        {
            string startName = "";
            string loopName = "";

            switch (Vdf.VehicleSize)
            {
                case 1: loopName = "eishp"; startName = "esshp" + _currentVehicleHealthGroup; break;
                case 2: loopName = "eihp"; startName = "eshp" + _currentVehicleHealthGroup; break;
                case 3: loopName = "einp1"; startName = "esnp" + _currentVehicleHealthGroup; break;
                case 4: loopName = "eisv"; startName = "essv"; break; // Note: Van didn't have health group in original?
                case 5: loopName = "eimarx"; startName = "esmarx"; break;
                case 6: loopName = "eitank"; startName = "estank"; break;
                default: Debug.LogWarning($"Unknown vehicle size {Vdf.VehicleSize}"); return;
            }

            startName += ".gpw";
            loopName += ".gpw";

            ReplaceAudioSource(ref _engineStartSound, startName, false);
            ReplaceAudioSource(ref _engineLoopSound, loopName, true);
        }

        private void ReplaceAudioSource(ref AudioSource source, string name, bool loop)
        {
            if (source != null && source.clip.name == name) return; // Already correct

            if (source != null) Destroy(source);
            
            source = CacheManager.Instance.GetAudioSource(gameObject, name);
            if (source != null)
            {
                source.volume = 0.6f;
                source.loop = loop;
                if (loop && EngineRunning) source.Play();
            }
        }
    }
}