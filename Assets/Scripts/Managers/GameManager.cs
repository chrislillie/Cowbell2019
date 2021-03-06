﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySpace;
using MySpace.Stats;
using System.Linq;
using System;

//A Manager of game-loop changing functionality between the tycoon-style daytime gameplay and the nighttime gameplay.

public class GameManager : MonoBehaviour
{
    #region Serialized Variables
    public static bool Debug { get { return Ref?.debug ?? false; } }
    [SerializeField] bool debug = false;

    [Header("Guest Arival Time")]
    public int maxGuests = 5; // set up for debug or test, change later.
    public int minuteMaxArival = 60;
    public int hourMinArival = 7;
    private int hourRandomArival;
    private int guestsRandom;
    private int minuteRandom;

    [Header("Guest Stay Time")]
    public int guestMaxStayTimeDays;
    public int guestMinStayTimeDays;

    [Header("===================================================================================================================================================================================")]
    [SerializeField] DebugToolsScript DebugMenu;

    [SerializeField] RoleInfo[] roles;
    //private static Roles _roleInfo;
    private static Dictionary<Enums.ManRole, RoleInfo> roleDic = new Dictionary<Enums.ManRole, RoleInfo>();

    [SerializeField] int startingHoots = 1000;

    [SerializeField] WorkerConstructionData[] workersToMake;

    public static Vector3[] StartPath;
    #endregion

    public static event System.Action<float> OnGameSpeedChanged;

    private static float gameSpeed;
    public static float GameSpeed
    {
        get => gameSpeed;
        set
        {
            if (value < 0)
                Time.timeScale = 0;
            else if (value > 5)
                Time.timeScale = 5;
            else
                Time.timeScale = value;
            gameSpeed = Time.timeScale;
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }
    }
    public static void ResumeGameSpeed()
    {
        GameSpeed = gameSpeed;
    }

    //Might not want to do this for the gamemanager?
    #region Singleton Management

    public static GameManager Ref { get; set; }

    private void Awake()
    {
        if (!Ref)
        {
            Ref = this;

            //We don't want this for the gamemanager
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (Ref != this)
            {
                Destroy(gameObject);
            }
        }
    }
    #endregion

    private void Start()
    {
        //set when guests start ariving
        MySpace.Events.EventManager.AddEventTriggerToGameTime((hourMinArival - 1), 0, 0, RandomGuestArival, true);
        MySpace.Events.EventManager.AddEventTriggerToGameTime(23, 59, 0, InitiateEndOfDay, true);
        DebugMenu.SetPanelActive(false);
        GameSpeed = 1;

        foreach (RoleInfo r in roles)
        {
            roleDic.Add(r.role,
                new RoleInfo()
                {
                    incomeMinimum = Mathf.Round(r.incomeMinimum),
                    incomeMaximum = Mathf.Round(r.incomeMaximum),
                    role = r.role
                });
        }

        //Might need to change this if loading a save
        WalletManager.SetHoots(1000);
        foreach (WorkerConstructionData d in workersToMake)
        {
            ClickManager.Ref.AddNewCleaner(d);
        }

        StartPath = (from PathPosition p in FindObjectsOfType<PathPosition>() orderby p.number ascending select p.transform.position).ToArray();
    }

    public static int GetRandomizedGuestArival()
    {
        return Mathf.RoundToInt(GetApproximatedRandomValue(10, 2, 0));
    }
    private void RandomGuestArival()
    {
        guestsRandom = UnityEngine.Random.Range(1, maxGuests); //Randomize amount of guests
        for (int increaseTime = 0; increaseTime < guestsRandom; increaseTime++) //Run following code for each desired guest daily
        {
            hourRandomArival = GetRandomizedGuestArival(); //Set hour max arival to bell curve random
            minuteRandom = UnityEngine.Random.Range(0, minuteMaxArival); //Randomize minute arival
            MySpace.Events.EventManager.AddEventTriggerToGameTime(hourRandomArival, minuteRandom, 0, CreateBasicGuest);//make guests arive at random time.
            if (Debug)
                UnityEngine.Debug.Log("New arival " + hourRandomArival + ":" + minuteRandom);
        }
    }

    #region Randomness
    /// <summary>
    /// Returns a random number calculated with weight based on the standard deviation off of average given.
    /// </summary>
    /// <param name="avg">The average value. If a random number of 0.5 from 0 to 1 is chosen, this function will return the average.</param>
    /// <param name="standDev">The standard deviation of the number distribution</param>
    /// <param name="accuracy">An increased number of accurace increases the accuracy of the iterative process. An accuracy of 2 will loop 100 times, an accuracy of 4 will loop 1000 times. Any accuracy above 15 will treat the accuracy as 15.</param>
    /// <returns></returns>
    public static float GetApproximatedRandomValue(float avg, float standDev, int accuracy = 4)
    {
        //Make sure we don't overload the computer
        if (accuracy > 15)
            accuracy = 15;
        //To make sure that it's accurate at least to some degree
        else if (accuracy < 1)
            accuracy = 1;

        float r = UnityEngine.Random.Range(0f, 1f);
        float total = standDev * 6, increment = total / Mathf.Pow(10, accuracy);
        float result = 0;

        //f1 doesn't use the iteration for each loop so it doesnt need to be calculated more than once.
        float f1 = 1f / (2.506628274631f/* sqRoot of PI*2 */ * Mathf.Sqrt(standDev));

        for (float i = (total * -0.5f) + avg; i < (total * 0.5f) + avg; i += increment)
        {
            float f2 = Mathf.Pow(Unity.Mathematics.math.E, -(Mathf.Pow(i - avg, 2) / (2 * standDev)));

            result += f1 * f2 * increment;
            if (result >= r)
            {
                return i;
            }
        }

        //if for some reason it fails, just return the average
        return avg;
    }
    #endregion

    private void CreateBasicGuest()
    {
        GuestConstructionData guest = CreateDefaultGuest();
        var sprite = guest.GetRandomizedSprite();
        guest.sprite = sprite;
        guest.sprite = sprite;
        ClickManager.Ref.Button_Book(guest);
        //ClickManager.Ref.AddNewGuest();
    }


    public static event MoodEventFunc MoodCalcEvent;
    public void InitiateEndOfDay()
    {
        float totalMood = 0;
        int num = 0;
        MoodCalcEvent?.Invoke(ref totalMood, ref num);
        if (num == 0)
        {
            num = 1;
            totalMood = MoodBubbleScript.DefaultMoodValue;
        }
        GuiManager.Ref.DailySummaryPanel.Enable("No Name", totalMood / num);
    }

    public static GuestConstructionData CreateDefaultGuest()
    {
        return new GuestConstructionData()
        {
            manFirstName = NameFactory.GetNewFirstName(),
            manLastName = NameFactory.GetNewLastName(),
            manId = System.Guid.NewGuid(),
            manType = Enums.ManTypes.Guest,
            generalStats = new GeneralStat[2]
            {
                new GeneralStat()
                {
                    statType = GeneralStat.StatType.Dirtiness,
                    value = 1
                },
                new GeneralStat()
                {
                    statType = GeneralStat.StatType.Speed,
                    value = 1
                }
            }
        };
    }

    public static RoleInfo GetRoleInfo(Enums.ManRole role)
    {
        return roleDic[role];
    }

    public static int GetRoleSalary(Enums.ManRole role, float loyalty)
    {
        float sMin = roleDic[role].incomeMinimum, sMax = roleDic[role].incomeMaximum;
        if (sMin > sMax)
            UnityEngine.Debug.LogError("The role of '" + role + "' has a higher minimum income than maximum!");
        float t = (sMax - sMin) / 9;

        return Mathf.RoundToInt(sMin + (t * (loyalty - 1)));
    }

    public static int GetRandomizedGuestStayTime(/*input a guest stay multiplier of some kind here maybe?*/)
    {
        return Mathf.Clamp(Mathf.RoundToInt(GetApproximatedRandomValue(3, 2)), Ref.guestMinStayTimeDays, Ref.guestMaxStayTimeDays);
    }

    #region Net Revenue Stuff
    //Can do more stuff with this later to access where the revenue came from or went to using the revenueinfo.

    //Can just use the average profit per day as the return value for any rooms that pay based on guest actions outside of a regular time interval, like casino's or gift shops, etc.

    public delegate void NetRevenueDelegate(List<RevenueInfo> list);
    public static NetRevenueDelegate NetRevenueCalculationEvent;

    public static int CalculateNetRevenue()
    {
        return Mathf.RoundToInt(CalculateHardNetRevenue());
    }

    public static float CalculateHardNetRevenue()
    {
        List<RevenueInfo> list = new List<RevenueInfo>();
        NetRevenueCalculationEvent?.Invoke(list);
        float f = 0;
        foreach (RevenueInfo i in list)
        {
            f += i.effect;
        }
        return f;
    }

    public static List<RevenueInfo> GetNetRevenueInfo()
    {
        List<RevenueInfo> list = new List<RevenueInfo>();
        NetRevenueCalculationEvent?.Invoke(list);
        return list;
    }
    #endregion
}

namespace MySpace
{
    public delegate void MoodEventFunc(ref float value, ref int num);

    [Serializable]
    public struct RoleInfo
    {
        public Enums.ManRole role;

        [Tooltip("Paid Daily")]
        public float incomeMinimum, incomeMaximum;
        public CharacterSwaper.CharLabel roleModel;
    }

    public struct RevenueInfo
    {
        public enum RevenueType : byte { Worker, Guest, Room }

        /// <summary>
        /// The increase or decrease in value that this object has on the revnue of each given day.
        /// </summary>
        public float effect;
        public RevenueType revenueType;
        public Guid objectId;

        /// <summary>
        /// If the revenue is estimated or simply a hard known value. Estimated values will come from non-daily sources, like a bar or casino that relies on guest usage.
        /// </summary>
        public bool estimated;

        public RevenueInfo(float value, RevenueType type, Guid id, bool estimated = false)
        {
            effect = value;
            revenueType = type;
            objectId = id;
            this.estimated = estimated;
        }
    }

    public static class Extensions
    {
        public static SpecialtyStat GetSpecialtyStat(this SpecialtyStat[] ar, SpecialtyStat.StatType type)
        {
            foreach (SpecialtyStat s in ar)
            {
                if (type == s.statType)
                    return s;
            }
            Debug.LogWarning("Specialty Stat type '" + type + "', was not found in the given array!!");
            return null;
        }

        public static GeneralStat GetGeneralStat(this GeneralStat[] ar, GeneralStat.StatType type)
        {
            foreach (GeneralStat s in ar)
            {
                if (type == s.statType)
                    return s;
            }
            Debug.LogWarning("General Stat type '" + type + "', was not found in the given array!!");
            return null;
        }

        public static void Add(this System.Action action, System.Action addition)
        {
            action -= addition;
            action += addition;
        }

        public static void Remove(this System.Action action, System.Action addition)
        {
            action -= addition;
        }

        public static void Add(this System.Action<ManScript> action, System.Action<ManScript> addition)
        {
            action -= addition;
            action += addition;
        }

        public static void Remove(this System.Action<ManScript> action, System.Action<ManScript> addition)
        {
            action -= addition;
        }
    }

    #region Containers
    public struct Container<T1, T2>
    {
        public T1 object1;
        public T2 object2;
    }

    public struct Container<T1, T2, T3>
    {
        public T1 object1;
        public T2 object2;
        public T3 object3;
    }

    public struct Container<T1, T2, T3, T4>
    {
        public T1 object1;
        public T2 object2;
        public T3 object3;
        public T4 object4;
    }
    #endregion
}

