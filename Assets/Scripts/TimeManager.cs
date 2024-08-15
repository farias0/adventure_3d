using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public int InitialDay = 1;
    public int InitialHour = 12;
    public int InitialMinute = 0;
    [Range(0, 1440)] 
    public int TimeUpdater; // Control the game time from the inspector
    public int DayUpdater; // Control the game day from the inspector

    private int mDay;
    [Range(0, 23)]
    private int mHour;
    [Range(0, 60)]
    private float mMinute;


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
        mDay = InitialDay;
        mHour = InitialHour;
        mMinute = InitialMinute;

        TimeUpdater = mHour * 60 + (int)mMinute;
        DayUpdater = mDay;
    }

    // Update is called once per frame
    void Update()
    {
        int timeForUpdater = mHour * 60 + (int)mMinute;
        int dayForUpdater = mDay;


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


        float angleInRadians = CalculateSunAngle(mHour, mMinute) * Mathf.Deg2Rad;

        float x = Mathf.Cos(angleInRadians) * SunRadius;
        float y = Mathf.Sin(angleInRadians) * SunRadius;

        Sun.transform.position = new Vector3(x, y, Sun.transform.position.z);
        Sun.transform.LookAt(Vector3.zero);


        InventoryController.Instance.UpdateDateTime(GetTime());


        // TimeUpdater
        if (TimeUpdater != timeForUpdater)
        {
            mHour = TimeUpdater / 60;
            mMinute = TimeUpdater % 60;
        }
        else
            TimeUpdater = mHour * 60 + (int)mMinute;
        // DayUpdater
        if (DayUpdater != dayForUpdater)
            mDay = DayUpdater;
        else
            DayUpdater = mDay;
    }

    private float CalculateSunAngle(int hour, float minute)
    {
        float hourAngle = hour * 15f;
        float minuteAngle = minute * 0.25f;
        return hourAngle + minuteAngle + 270;
    }
}
