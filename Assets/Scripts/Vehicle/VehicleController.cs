using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        public List<AxleInfo> axleInfos; // the information about each individual axle
        public float maxMotorTorque; // maximum torque the motor can apply to wheel
        public float maxSteeringAngle; // maximum steer angle the wheel can have
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Camera vehicleCamera;

        [Header("Vehicle")]
        public VehicleInputs vehicleInputs;
        public bool handbrake_input = true;
        
        public void FixedUpdate()
        {
            float motor = maxMotorTorque * Input.GetAxis("Vertical");
            float steering = maxSteeringAngle * Input.GetAxis("Horizontal");
                
            foreach (AxleInfo axleInfo in axleInfos) {
                if (axleInfo.steering) {
                    axleInfo.leftWheel.steerAngle = steering;
                    axleInfo.rightWheel.steerAngle = steering;
                }
                if (axleInfo.motor) {
                    axleInfo.leftWheel.motorTorque = motor;
                    axleInfo.rightWheel.motorTorque = motor;
                }
                ApplyLocalPositionToVisuals(axleInfo.leftWheel);
                ApplyLocalPositionToVisuals(axleInfo.rightWheel);
            }
        }

        private void Update() {
            RotateCamera();
        }

        private void HandleVehicleInputs() {
            vehicleInputs = new VehicleInputs();
            if (vehicleInputs != null) {
                vehicleInputs.VehicleActions.Brake.performed += i => {
                    Handbrake(handbrake_input);
                    handbrake_input = !handbrake_input;
                };
            }
            vehicleInputs.Enable();
        }

        public void ApplyLocalPositionToVisuals(WheelCollider collider)
        {
            if (collider.transform.childCount == 0) {
                return;
            }
        
            Transform visualWheel = collider.transform.GetChild(0);
        
            Vector3 position;
            Quaternion rotation;
            collider.GetWorldPose(out position, out rotation);
        
            visualWheel.transform.position = position;
            visualWheel.transform.rotation = rotation;
        }

        private void RotateCamera ()
        {
            vehicleCamera.transform.RotateAround(transform.position, Vector3.up, Input.GetAxis("Mouse X"));
        }

        public void Handbrake (bool isBraking) {
            if (isBraking){
                foreach (AxleInfo axleInfo in axleInfos) {
                    if (axleInfo.motor) {
                        axleInfo.leftWheel.brakeTorque = 1000f;
                        axleInfo.rightWheel.brakeTorque = 1000f;
                    }
                }
            }
            else {
                foreach (AxleInfo axleInfo in axleInfos) {
                    if (axleInfo.motor) {
                        axleInfo.leftWheel.brakeTorque = 0f;
                        axleInfo.rightWheel.brakeTorque = 0f;
                    }
                }
            }
        }

        private void Enter () {
            playerController.gameObject.SetActive(false);
            vehicleCamera.gameObject.SetActive(true);
        }

        public void EnterDriverSeat () {
            Enter();
            this.enabled = true;
        }

        private void OnEnable() {
            HandleVehicleInputs();
        }

        private void OnDisable() {
            vehicleInputs.Disable();
        }
    }

    [System.Serializable]
    public class AxleInfo {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor; // is this wheel attached to motor?
        public bool steering; // does this wheel apply steer angle?
    }
}
