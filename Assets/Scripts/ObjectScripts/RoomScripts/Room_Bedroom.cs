﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySpace;
using System;

public class Room_Bedroom : RoomScript
{
    public const float cleanTickTime = 1;

    #region Serialized Variables
    [SerializeField] Shader stankShade;
    [SerializeField] Material stankMatSet;
    [SerializeField] private Animator doorAnim;
    [SerializeField] Transform DoorPos;

    [Tooltip("The cost of this room to stay in for guests")]
    public int RentCost = 50;

    #endregion

    public override Enums.ManRole RoomRole => Enums.ManRole.Guest;

    #region Private Variables
    float c = -1;
    private float clipLength
    {
        get
        {
            if (c < 0)
            {
                c = doorAnim.runtimeAnimatorController.animationClips[0].length;
            }
            return c;
        }
    }
    private bool GetDoorIsOpen
    {
        get
        {
            if (doorAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= clipLength && !doorAnim.IsInTransition(0))
            {
                return doorAnim.GetBool("SingleDoorOpen");
            }
            else
                return false;
        }
    }
    private bool GetDoorIsClosed
    {
        get
        {
            if (doorAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= clipLength && !doorAnim.IsInTransition(0))
            {
                return !doorAnim.GetBool("SingleDoorOpen");
            }
            else
                return false;
        }
    }
    public bool CheckDoor(bool closed)
    {
        if (closed)
            return GetDoorIsClosed;
        else
            return GetDoorIsOpen;
    }

    ParticleSystem stankParts;
    Material stankMat;
    private float fElapsedTime = 0;

    // Room Attributes
    private float cleanliness; //For debugging to the inspector
    public float Cleanliness { get { return cleanliness; } set { cleanliness = value; } }

    public const float cleanlinessThreshhold = 0.4f;

    
    Room_Hallway parent;
    #endregion

    protected override void Start()
    {
        //We are calling start in Self_Initialize, so there's no need to have it called again here.
        //base.Start();
        Debug.Assert(DoorPos != null);
    }

    protected override void Update()
    {
        base.Update();
        if (delay < 1)
            delay += Time.deltaTime;

        fElapsedTime += Time.deltaTime;
        if (RoomIsActive && fElapsedTime > cleanTickTime)
        {
            int numMen = CountMen();
            fElapsedTime = 0.0f;
            bool bStank = false; // If true deteriorates room
            float dirtyFactor = 0, mult = (cleanTickTime / .01667f) * 0.01f;
            //Check to see if Room is occupied by Guest and decrease cleanliness. 
            //If room is occupied by Cleaners, increase cleanliness.
            //Make it so Cleaners cannot occupy the rooms at the same time as guests?

            for (int i = 0; i < numMen; i++)
            {
                Guid id = RoomData.ManSlotsAssignments[i];
                if (id == Guid.Empty)
                    continue;

                if (!(manManRef.GetManData(id).ManScript.State == Enums.ManStates.None))
                    continue;

                if (manManRef.IsManTypeOf<ManScript_Guest>(id))
                {
                    bStank = true;
                    dirtyFactor += manManRef.GetManData(id).ManScript.GetGeneralStatValue(MySpace.Stats.GeneralStat.StatType.Dirtiness);
                }
            }

            if (bStank)
            {
                Cleanliness -= RoomManager.Ref.DirtinessSpeedRatio * dirtyFactor * cleanTickTime * Time.deltaTime * mult;
                //print("dirty: " + RoomManager.Ref.CleanSpeedRatio * dirtyFactor * cleanTickTime * Time.deltaTime);
                if (Cleanliness < 0)
                    Cleanliness = 0;
            }

            SetShaderValue();
        }
    }

    public void SelfInitialize(GridIndex leftMostIndex, Room_Hallway parent)
    {
        this.parent = parent;

        //HAD TO DO ALL OF THE CREATEROOM INITIALIZATION HERE BECAUSE IT CANNOT BE INSTANTIATED IN THE WRONG POSITION THROUGH CREATEROOM
        RoomDefData RoomDefData = new RoomDefData("BedroomBase", gameObject, Enums.RoomSizes.Size2, Enums.RoomTypes.Bedroom, Enums.RoomCategories.Miscellaneous, 2, CreateNewArray(2), "The room in which all Hootel guests stay. These rooms become dirtied by guests over time and need to be cleaned by cleaners.", 0, Enums.RoomOverUnder.Over, false, null);

        RoomData = new RoomInstanceData();
        RoomData.RoomId = Guid.NewGuid();
        RoomData.RoomName = RoomDefData.RoomName;
        RoomData.RoomSize = RoomDefData.RoomSize;
        RoomData.RoomCategory = RoomDefData.RoomCategory;
        RoomData.RoomType = RoomDefData.RoomType;
        RoomData.RoomOverUnder = RoomDefData.RoomOverUnder;
        RoomData.ManSlotCount = RoomDefData.ManSlotCount;
        RoomData.ManSlotsPositions = new Vector3[RoomDefData.ManSlotCount]; // Data will be set by object script on Start()
        RoomData.ManSlotsRotations = new Quaternion[RoomDefData.ManSlotCount]; // Data will be set by object script on Start()
        RoomData.ManSlotsAssignments = new Guid[RoomDefData.ManSlotCount];
        RoomData.OwnerSlotsAssignments = new Guid[RoomDefData.ManSlotCount];
        RoomData.RoomScript = this;
        RoomData.RoomDescription = RoomDefData.RoomDescription;

        for (int i = 0; i < RoomData.ManSlotCount; i++) RoomData.ManSlotsAssignments[i] = Guid.Empty;
        RoomData.ManWorkingStates = RoomDefData.ManWorkingStates;
        RoomData.CoveredIndizes = GridManager.Ref.GetOccupiedindizes(RoomDefData.RoomSize, leftMostIndex);

        for (int i = 0; i < RoomData.ManSlotCount; i++) RoomData.OwnerSlotsAssignments[i] = Guid.Empty;

        GridManager.Ref.RegisterAtGrid(RoomData.RoomSize, RoomData.RoomId, leftMostIndex, false);
        RoomManager.Ref.AddRoom(RoomData.RoomId, new RoomRef(gameObject, this));

        #region Manual assignment of grid index movement directions
        GridManager.Ref.AddMovementDirectionToGridIndex(RoomData.CoveredIndizes[1], Enums.MoveDirections.Front);
        GridManager.Ref.AddMovementDirectionToGridIndex(RoomData.CoveredIndizes[1], Enums.MoveDirections.Left);
        GridManager.Ref.AddMovementDirectionToGridIndex(RoomData.CoveredIndizes[1], Enums.MoveDirections.D_FrontLeft);
        GridManager.Ref.AddMovementDirectionToGridIndex(RoomData.CoveredIndizes[0], Enums.MoveDirections.Right);
        GridManager.Ref.AddMovementDirectionToGridIndex(RoomData.CoveredIndizes[0], Enums.MoveDirections.Front);
        GridManager.Ref.AddMovementDirectionToGridIndex(RoomData.CoveredIndizes[0], Enums.MoveDirections.D_FrontRight);
        #endregion

        Cleanliness = 1;
        stankParts = GetComponentInChildren<ParticleSystem>();
        
        stankParts.GetComponentInChildren<Renderer>().material = Instantiate(stankMatSet);
        stankMat = stankParts.GetComponentInChildren<Renderer>().material;
        stankMat.shader = Instantiate(stankShade);

        //Call start now so that it happens before waiting for the next frame.
        base.Start();
        SetShaderValue();
        //doorAnim = MeshBothOpen.GetComponentInChildren<Animator>();
        SetupGridEvents();

        Enums.ManStates[] CreateNewArray(int length)
        {
            var ar = new Enums.ManStates[length];
            for (int i = 0; i < length; i++)
            {
                ar[i] = Enums.ManStates.Idle;
            }
            return ar;
        }
    }

    private void SetShaderValue()
    {
        //Cleanliness below 80 % should spawn the particles
        if (Cleanliness > 0.8f)
        {
            if (stankParts.gameObject.activeInHierarchy)
                stankParts.gameObject.SetActive(false);
            return;
        }
        else if (!stankParts.gameObject.activeInHierarchy)
        {
            stankParts.gameObject.SetActive(true);
        }

        //The 1.25 multiplier is to offset the 80% activation value. The 0.4 multiplier is to make it so that the particle shader float is never above 0.4
        stankMat.SetFloat("trans", (1 - (Cleanliness * 1.25f)) * 0.4f);
        //print(stankMat.GetFloat("trans"));
    }

    public void CleanRoom(float cleanFactor)
    {
        Cleanliness += RoomManager.Ref.CleanSpeedRatio * cleanFactor * cleanTickTime * Time.deltaTime * 0.01f;
        if (Cleanliness > 1)
            Cleanliness = 1;
    }

    private void SetupGridEvents()
    {
        GridIndex start, end;
        start = RoomData.CoveredIndizes[0].GetFront();
        end = RoomData.CoveredIndizes[0];
        GridManager.AddEventToGrid(new IndexPair(start, end), new IndexEvent()
        {
            sourceId = RoomData.RoomId,
            start = start,
            end = end,
            //OnIndexStart = OpenDoor,
            OverrideMovementUpdate = OverRideUpdateForDoor,
            //OnIndexEnd = CloseDoor
        });


        start = RoomData.CoveredIndizes[1].GetFront();
        end = RoomData.CoveredIndizes[1];
        GridManager.AddEventToGrid(new IndexPair(start, end), new IndexEvent()
        {
            sourceId = RoomData.RoomId,
            start = start,
            end = end,
            //OnIndexStart = OpenDoor,
            OverrideMovementUpdate = OverRideUpdateForDoor,
            //OnIndexEnd = CloseDoor
        });


        start = RoomData.CoveredIndizes[0];
        end = RoomData.CoveredIndizes[0].GetFront();
        GridManager.AddEventToGrid(new IndexPair(start, end), new IndexEvent()
        {
            sourceId = RoomData.RoomId,
            start = start,
            end = end,
            //OnIndexStart = OpenDoor,
            OverrideMovementUpdate = OverRideUpdateForDoor,
            //OnIndexEnd = CloseDoor
        });


        start = RoomData.CoveredIndizes[1];
        end = RoomData.CoveredIndizes[1].GetFront();
        GridManager.AddEventToGrid(new IndexPair(start, end), new IndexEvent()
        {
            sourceId = RoomData.RoomId,
            start = start,
            end = end,
            //OnIndexStart = OpenDoor,
            OverrideMovementUpdate = OverRideUpdateForDoor,
            //OnIndexEnd = CloseDoor
        });


        //DIAGONALS
        start = RoomData.CoveredIndizes[1];
        end = RoomData.CoveredIndizes[1].GetDiag_FrontLeft();
        GridManager.AddEventToGrid(new IndexPair(start, end), new IndexEvent()
        {
            sourceId = RoomData.RoomId,
            start = start,
            end = end,
            OverrideMovementUpdate = OverRideUpdateForDoor
        });

        start = RoomData.CoveredIndizes[0];
        end = RoomData.CoveredIndizes[0].GetDiag_FrontRight();
        GridManager.AddEventToGrid(new IndexPair(start, end), new IndexEvent()
        {
            sourceId = RoomData.RoomId,
            start = start,
            end = end,
            OverrideMovementUpdate = OverRideUpdateForDoor
        });
    }

    
    

    float delay = 0;
    public void OpenDoor(ManScript man)
    {
        OpenDoor();
    }

    public void OpenDoor()
    {
        doorAnim.SetBool("SingleDoorOpen", true);
        delay = 0;
    }

    public void CloseDoor(ManScript man)
    {
        CloseDoor();
    }

    public void CloseDoor()
    {
        if (waitingMan == Guid.Empty)
        {
            doorAnim.SetBool("SingleDoorOpen", false);
        }
    }

    Guid manIn, waitingMan;
    public bool OverRideUpdateForDoor(ManScript man, Vector3 target)
    {
        if (manIn != Guid.Empty && manIn != man.ManData.ManId)
        {
            if (waitingMan == Guid.Empty)
                waitingMan = man.ManData.ManId;
            //if (man.State != Enums.ManStates.Waiting)
            man.SetAnimation(Enums.ManStates.Waiting, 0);
            return false;
        }
        else if (delay < 0.2f && !CheckDoor(false))
        {
            //do nothing, wait for door to be open.
            //if (man.State != Enums.ManStates.Waiting)
            man.SetAnimation(Enums.ManStates.Waiting, 0);
            return false;
        }
        else if (man.transform.position.z == target.z)
        {
            //if (man.State != Enums.ManStates.Running)
            man.SetAnimation(Enums.ManStates.Running, -1, target);
            float Travel = (Constants.ManRunSpeed + (man.GetGeneralStatValue(MySpace.Stats.GeneralStat.StatType.Speed) * 0.1f)) * Time.deltaTime;
            if (Travel > Vector3.Distance(man.transform.position, target))
            {
                man.transform.position = target;
                return true;
            }
            else
            {
                Vector3 dir = (target - man.transform.position);
                dir.Normalize();
                man.transform.position += (dir * Travel);
                return false;
            }
        }
        else if (man.transform.position.x == DoorPos.transform.position.x)
        {
            target = new Vector3(DoorPos.transform.position.x, target.y, target.z);
            //if (man.State != Enums.ManStates.Running)
            man.SetAnimation(Enums.ManStates.Running, -1, target);
            float Travel = (Constants.ManRunSpeed + (man.GetGeneralStatValue(MySpace.Stats.GeneralStat.StatType.Speed) * 0.1f)) * Time.deltaTime;

            if (Travel > Vector3.Distance(man.transform.position, target))
            {
                man.transform.position = target;
                CloseDoor();
                if (waitingMan != Guid.Empty)
                {
                    manIn = waitingMan;
                    waitingMan = Guid.Empty;
                }
                else
                    manIn = Guid.Empty;
                return false;
            }
            else
            {
                Vector3 dir = (target - man.transform.position);
                dir.Normalize();
                man.transform.position += (dir * Travel);
                return false;
            }
        }
        else
        {
            target = new Vector3(DoorPos.transform.position.x, man.transform.position.y, man.transform.position.z);
            //if (man.State != Enums.ManStates.Running)
            man.SetAnimation(Enums.ManStates.Running, -1, target);
            float Travel = (Constants.ManRunSpeed + (man.GetGeneralStatValue(MySpace.Stats.GeneralStat.StatType.Speed) * 0.1f)) * Time.deltaTime;

            if (Travel > Vector3.Distance(man.transform.position, target))
            {
                man.transform.position = target;
                OpenDoor();
                manIn = man.ManData.ManId;
                return false;
            }
            else
            {
                Vector3 dir = (target - man.transform.position);
                dir.Normalize();
                man.transform.position += (dir * Travel);
                return false;
            }
        }
    }
    
}
