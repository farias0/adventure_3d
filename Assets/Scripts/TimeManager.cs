using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTime
{
    public int Day { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
}

public class TimeManager : MonoBehaviour
{
    public Light Sun;
    public float SunRadius = 10.0f;
    public float GameSpeed = 60.0f; // 60 means 1 second irl = 1 minute in game

    private int mDay;
    [Range(0, 23)]
    private int mHour;
    [Range(0, 60)]
    private float mMinute;
    private float mCurrentAngle = 90;

    const float SunDegreePerSecond = 360.0f / 1440.0f / 60.0f;


    public GameTime GetTime()
    {
        return new GameTime
        {
            Day = mDay,
            Hour = mHour,
            Minute = (int)mMinute
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mMinute += Time.deltaTime;
        if (mMinute >= 60)
        {
            mMinute = 0;
            mHour++;
            if (mHour >= 24)
            {
                mHour = 0;
                mDay++;
            }
        }


        mCurrentAngle += SunDegreePerSecond * GameSpeed * Time.deltaTime;
        float angleInRadians = mCurrentAngle * Mathf.Deg2Rad;

        float x = Mathf.Cos(angleInRadians) * SunRadius;
        float y = Mathf.Sin(angleInRadians) * SunRadius;

        Sun.transform.position = new Vector3(x, y, Sun.transform.position.z);
        Sun.transform.LookAt(Vector3.zero);
    }
}
