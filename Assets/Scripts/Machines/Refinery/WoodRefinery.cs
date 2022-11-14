using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refiner 
{
    public class WoodRefinery : MonoBehaviour
    {
        private ManageRefiner manageRefiner;
        private Transform instanciatePos;
        [SerializeField] private GameObject rawWood;

        private void Awake() {
            manageRefiner = GetComponent<ManageRefiner>();
            instanciatePos = GetComponent<BoxCollider>().transform;
        }

        private void OnTriggerEnter(Collider other) {
            if (other.gameObject.tag == "Wood") {
                Wood wood = other.gameObject.GetComponent<Wood>();
                if (wood == null) {
                    return;
                }
                if (wood.isRefined) {
                    return;
                }
                float mass = other.GetComponent<Rigidbody>().mass;
                float lenght = other.GetComponent<Collider>().bounds.size.x;
                float width = other.GetComponent<Collider>().bounds.size.z;
                float height = other.GetComponent<Collider>().bounds.size.y;
                float volume = lenght * width * height;
                float density = mass / volume;
                float woodValue = density * 10;
                Debug.Log("Wood value: " + woodValue);
                Destroy(other.gameObject);
                RefinerWood(new float[] {woodValue, mass, volume, density, lenght, width, height});
            }
        }

        private void RefinerWood(float[] woodData) {
            float sizeX = float.Parse(manageRefiner.screenTextX.text);
            float sizeY = float.Parse(manageRefiner.screenTextY.text);

            float woodValue = woodData[0];
            float woodMass = woodData[1];
            float woodVolume = woodData[2];
            float woodDensity = woodData[3];
            float woodLenght = woodData[4];
            float woodWidth = woodData[5];
            float woodHeight = woodData[6];

            Wood newWood = Instantiate(rawWood, instanciatePos.position, Quaternion.identity).GetComponent<Wood>();
            newWood.isRefined = true;
        }
    }
}
