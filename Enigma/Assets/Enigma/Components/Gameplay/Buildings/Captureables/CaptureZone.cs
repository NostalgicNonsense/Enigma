﻿using System.Collections.Generic;
using Enigma.Components.Gameplay.TeamSettings.Enums;
using UnityEngine;

namespace Enigma.Components.Gameplay.Buildings.Captureables
{
    public class CaptureZone : MonoBehaviour
    {
        private Team team;

        public Color colorDefault;
        

        [SerializeField]
        private float progressMax = 100;
        [SerializeField]
        private float progressSpeed = 10f;
        private float progress = 0; //team1: -100, team2: 100


        private List<Collider> ListTeam1;
        private List<Collider> ListTeam2;

        private CaptureFlag captureFlag;
        private BuildingResourceGive buildingResource;

        private TextMesh textMesh;

        void Start()
        {
            team = GetComponentInParent<Team>();
            buildingResource = GetComponentInParent<BuildingResourceGive>();
            captureFlag = GetComponentInChildren<CaptureFlag>();
            textMesh = GetComponentInChildren<TextMesh>();

            ListTeam1 = new List<Collider>();
            ListTeam2 = new List<Collider>();
            Debug.Log("max: " + progressMax);
        }

        void Update()
        {
            CheckCapture();
        }

        private void CheckCapture()
        {
            if (ListTeam1.Count > 0 && team.TeamName != TeamName.Team1)
            {
                if (ListTeam1.Count > ListTeam2.Count)
                {
                    StartCapturing(TeamName.Team1);
                }
            }

            if (ListTeam2.Count > 0 && team.TeamName != TeamName.Team2)
            {
                if (ListTeam2.Count > ListTeam1.Count)
                {
                    StartCapturing(TeamName.Team2);
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var teamOther = other.GetComponentInParent<Team>();
            if (teamOther != null)
            {

                var otherName = teamOther.TeamName;
                if (otherName == TeamName.Team1 && ListTeam1.Contains(other) == false)
                {
                    ListTeam1.Add(other);
                }

                if (otherName == TeamName.Team2 && ListTeam2.Contains(other) == false)
                {
                    ListTeam2.Add(other);
                }

                if (team.TeamName != otherName)
                {
                    //Check capture
                }
            }
        }

        void OnTriggerExit(Collider other)
        {

            if (ListTeam1.Contains(other))
                ListTeam1.Remove(other);
            else if (ListTeam2.Contains(other))
                ListTeam1.Remove(other);
        }

        private void StartCapturing(TeamName teamName)
        {

            if (teamName == TeamName.Team1 && progress > -progressMax)
            {
                progress -= progressSpeed * ListTeam1.Count * Time.deltaTime;

                if (progress <= -progressMax)
                {
                    progress = -progressMax;
                    SetTeam(teamName);
                }
                textMesh.text = "TeamOfTurret 1: " + (progress * -1).ToString("F0") + " / " + progressMax;
            }

            if (teamName == TeamName.Team2 && progress < progressMax)
            {
                progress += progressSpeed * ListTeam2.Count * Time.deltaTime;


                if (progress >= progressMax)
                {
                    progress = progressMax;
                    SetTeam(teamName);
                }
                textMesh.text = "TeamOfTurret 2: " + progress.ToString("F0") + " / " + progressMax;
            }
        }

        private void SetTeam(TeamName teamName)
        {
            team.TeamName = teamName;

            if (buildingResource != null)
            {
                Debug.Log("captured");

                buildingResource.SetTeam(teamName);
                captureFlag.SetTeam(teamName);
            }
        }
    }
}
