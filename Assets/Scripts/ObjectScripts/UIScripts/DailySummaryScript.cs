﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MySpace;

public class DailySummaryScript : MonoBehaviour
{
    public TextMeshProUGUI Title, HappinessText, BucksInText, BucksOutText, TotalText;

    public GameObject HappyFace, AlmostHappyFace, NotHappyFace, LyeydownFace, BotheyesdownFace, SadFace, ScaredFace, SickFace, MonsterFace;


    private void Start()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hootelName">Name of the current hootel to be displayed.</param>
    /// <param name="guestHappiness">Should be a value from 0 to 1, 0 being 0% happiness and 1 being 100% happiness.</param>
    public void Enable(string hootelName, float guestHappiness)
    {
        gameObject.SetActive(true);
        Time.timeScale = 0;
        #region Revenue Calc Stuff
        List<RevenueInfo> info = GameManager.GetNetRevenueInfo();
        float fOut = 0, fIn = 0;

        foreach(RevenueInfo i in info)
        {
            if (i.effect > 0)
            {
                fIn += i.effect;
            }
            else
            {
                fOut += i.effect;
            }
        }

        fOut = Mathf.Round(fOut);
        fIn = Mathf.Round(fIn);
        #endregion

        Title.text = "Daily Summary: " + hootelName;
        HappinessText.text = Mathf.Round(guestHappiness * 100) + "%";
        BucksInText.text =fIn.ToString();
        BucksOutText.text = fOut.ToString();
        TotalText.text = (fOut + fIn).ToString();

        //currently not using the sick face, monster face, or lyeydownface
        SetFace(guestHappiness);
    }


    public void ProceedToNextDayClick()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public void FinancesClick()
    {
        //This shouldn't do anything for now, This will be implemented post POC.
        print("Finances Button Clicked!");
    }

    private void SetFace(float v)
    {
        HappyFace.SetActive(false);
        AlmostHappyFace.SetActive(false);
        NotHappyFace.SetActive(false);
        LyeydownFace.SetActive(false);
        BotheyesdownFace.SetActive(false);
        SadFace.SetActive(false);
        ScaredFace.SetActive(false);
        SickFace.SetActive(false);
        MonsterFace.SetActive(false);

        if (v >= 0.9f)
        {
            HappyFace.SetActive(true);
        }
        else if (v >= 0.75f)
        {
            AlmostHappyFace.SetActive(true);
        }
        else if (v >= 0.60f)
        {
            NotHappyFace.SetActive(true);
        }
        else if (v >= 0.5f)
        {
            BotheyesdownFace.SetActive(true);
        }
        else if (v >= 0.3f)
        {
            SadFace.SetActive(true);
        }
        else
        {
            ScaredFace.SetActive(true);
        }
    }
}