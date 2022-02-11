using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = System.Random;

public class Rotater : MonoBehaviour
{
    public List<Price> prices = new List<Price>();
    
    public GameObject wheel;
    public GameObject gluecksrad;
    public GameObject middlePin;
    public GameObject baiverLogo;
    public GameObject nameText;
    public GameObject textText;
    
    public float defaultDeacceleartion = -40f;
    
    
    private State _state = State.WaitingForSpin;
    
    private Quaternion _lookRotation;

    private Queue<float> _lastVelocitys = new Queue<float>();

    private Vector3 _direction;
    private Vector3 _rotationLast;
    private Vector3 _rotationDelta;
    
    private float _neededAcceleration;
    private float _time;
    private float _distanceRemaining;
    private float _angularVelocity;

    private bool _endScreenAnimFinished = false;


    private enum State
    {
        WaitingForSpin,
        SpinInControl,
        FreeSpinning,
        EndScreen,
    }
    
    private void Start()
    {
        _lastVelocitys.Enqueue(transform.rotation.eulerAngles.z);
        nameText.SetActive(false);
        textText.SetActive(false);
    }

    private void Update()
    {
        switch (_state)
        {
            case State.WaitingForSpin:
                WaitingForSpin();
                break;
            case State.SpinInControl:
                SpinInControl();
                break;
            case State.FreeSpinning:
                FreeSpinning();
                break;
            case State.EndScreen:
                EndScreen();
                break;
        }
    }

    private void WaitingForSpin()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetRotationToMousePoint();
            wheel.transform.parent = gameObject.transform;
            _state = State.SpinInControl;
        }
    }

    private void SpinInControl()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _angularVelocity = GetAngularVelocity(_lastVelocitys);
            _distanceRemaining = CalculateDistance();
            _state = State.FreeSpinning;
            return;
        }
        
        _lastVelocitys.Enqueue(transform.rotation.eulerAngles.z);
        if (_lastVelocitys.Count > 5)
        {
            _lastVelocitys.Dequeue();
        }

        SetRotationToMousePoint();
    }

    private float CalculateDistance()
    {
        var direction = Mathf.Sign(_angularVelocity);
        _angularVelocity = Mathf.Clamp( Mathf.Abs(_angularVelocity), 360, 1500) * direction;
            
        var defaultDistance = -(_angularVelocity * _angularVelocity) / (2 * defaultDeacceleartion);
        var defaultFullCircle = defaultDistance - defaultDistance % 360;
        defaultFullCircle *= direction;
        var targetAngle = ChoosePriceAngle() - wheel.transform.rotation.eulerAngles.z;
        if (targetAngle <= 0)
            targetAngle += 360;
            
        return targetAngle + defaultFullCircle;
    }

    private void CalculateAcceleration()
    {
        _neededAcceleration = -(_angularVelocity * _angularVelocity) / (2 * _distanceRemaining);
    }

    private void FreeSpinning()
    {
        if(IsFinishedFreeSpinning())
        {
            _state = State.EndScreen;
        
            wheel.transform.parent = gluecksrad.transform;
            
            var price = GetPriceFromRotation();

            StartCoroutine(StartWinAnimation(price));

            _lastVelocitys = new Queue<float>();

            return;
        }
        CalculateAcceleration();
        _angularVelocity += _neededAcceleration * Time.deltaTime;
        _distanceRemaining -= _angularVelocity * Time.deltaTime;
        transform.Rotate(new Vector3(0,0,_angularVelocity * Time.deltaTime));
    }

    private Price GetPriceFromRotation()
    {
        var rotation = wheel.transform.rotation;
        var t = rotation.eulerAngles.z - rotation.eulerAngles.z % 30;

        var position = Convert.ToInt32(t) / 30;
        var price = prices[position];
        return price;
    }

    private bool IsFinishedFreeSpinning()
    {
        return Mathf.Abs(_angularVelocity) <= Mathf.Abs(_neededAcceleration * Time.deltaTime);
    }

    private float ChoosePriceAngle()
    {
        var rand = new Random();
        var number = rand.Next(0, 101);
        Price price = null;
        foreach (var p in prices)
        {
            if (number >= p.fromProbability && number <= p.toProbability)
            {
                price = p;
                break;
            }
        }
        Debug.Log($"Chosen Price: {price.name}");

        var indexes = GetIndexesOfPrice(price);
        var randomIndex = rand.Next(0, indexes.Count);
        var index = indexes[randomIndex];
        return (index * 30) + rand.Next(5, 25);
    }

    private List<int> GetIndexesOfPrice(Price price)
    {
        var indexes = new List<int>();
        
        for (int i = 0; i < prices.Count; i++)
        {
            if (price.Equals(prices[i]))
            {
                indexes.Add(i);
            }
        }

        return indexes;
    }

    private void EndScreen()
    {
        if (Input.GetMouseButtonDown(0) && _endScreenAnimFinished)
        {
            _endScreenAnimFinished = false;
            _state = State.WaitingForSpin;
            textText.SetActive(false);
            nameText.SetActive(false);
            middlePin.GetComponent<Animator>().Play("whiteCircleBackwards");
            baiverLogo.GetComponent<Animator>().Play("BaiverLogoBackwards");
        }
    }
    
    IEnumerator StartWinAnimation(Price price)
    {
        yield return new WaitForSeconds(0.5f);
        middlePin.GetComponent<Animator>().Play("whiteCircle");
        baiverLogo.GetComponent<Animator>().Play("BaiverLogo");
        StartCoroutine(DisplayText(price));
    }

    IEnumerator DisplayText(Price price)
    {
        yield return new WaitForSeconds(0.4f);
        nameText.SetActive(true);
        textText.SetActive(true);
        
        nameText.GetComponent<TextMeshProUGUI>().text = price.name;
        nameText.GetComponent<TextMeshProUGUI>().color = price.hauptGewinn ? new Color32(218,165,32, 255) : new Color32(75, 75, 75, 255);
        textText.GetComponent<TextMeshProUGUI>().text = price.text;
        _endScreenAnimFinished = true;
    }
    
    void SetRotationToMousePoint()
    {
        Vector2 pos;
        
        if (Input.GetMouseButton(0))
        {
            if (Input.touchCount > 0)
            {
                pos = Input.GetTouch(0).position;
            }
            else
            {
                pos = Input.mousePosition;
            }
            
            
            Vector2 mouseScreenPosition = Camera.main.ScreenToWorldPoint(pos);

            var transform1 = transform;
            Vector2 direction = (mouseScreenPosition - (Vector2) transform1.position).normalized;

            transform1.up = direction;
        }
    }
    
    private float GetAngularVelocity(Queue<float> velocitys)
    {
        var direction = 0;

        float l = 0;
        foreach (var velocity in velocitys)
        {
            
            if (l != 0)
            {
                direction += Math.Sign(velocity - l);
            }

            l = velocity;
        }

        direction = Math.Sign(direction);

        var lastVelocity = velocitys.First();
        var currentVelocity = velocitys.Last();
        
        
        if (direction == 1)
        {
            if (lastVelocity > currentVelocity)
            {
                currentVelocity += 360;
            }
        }
        else
        {
            if (currentVelocity > lastVelocity)
            {
                currentVelocity -= 360;
            }
        }

        var finalVelo = (currentVelocity - lastVelocity) / velocitys.Count;

        return (1 / Time.deltaTime) * finalVelo;
    }
}