using UnityEngine;
using System.Collections;

public class OpponentCarController : MonoBehaviour {

    public Sprite[] carSprites;

    private Vector3 _startPos;
    private Vector3 _destinationPos;
    private Quaternion _startRot;
    private Quaternion _destinationRot;
    private float _lastUpdateTime;
    private float _timePerUpdate = 0.16f;

    private Vector3 _lastKnownVel;

	// Use this for initialization
	void Start () {
	    _startPos = transform.position;
        _startRot = transform.rotation;
	}
	
	// Update is called once per frame
	void Update () {
	    float pctDone = (Time.time - _lastUpdateTime) / _timePerUpdate;
 
        if (pctDone <= 1.0) {
            // 2
            transform.position = Vector3.Lerp (_startPos, _destinationPos, pctDone);
            transform.rotation = Quaternion.Slerp (_startRot, _destinationRot, pctDone);
        }  else {
            // Guess where we might be
            transform.position = transform.position + (_lastKnownVel * Time.deltaTime);
        } 
	}

    public void SetCarNumber (int carNum) {
        GetComponent<SpriteRenderer>().sprite = carSprites[carNum-1];
    }

    public void SetCarInformation(float posX, float posY, float velX, float velY, float rotZ) {
        // 1
        _startPos = transform.position;
        _startRot = transform.rotation;
        // 2
        _destinationPos = new Vector3 (posX, posY, 0);
        _destinationRot = Quaternion.Euler (0, 0, rotZ);
        //3
        _lastKnownVel = new Vector3 (velX, velY, 0);
        _lastUpdateTime = Time.time;
    }
}
