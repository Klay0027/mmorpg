﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entities;
using SkillBridge.Message;
using Services;

public class PlayerInputController : MonoBehaviour
{
    public Rigidbody rb; //玩家父物体的刚体
    SkillBridge.Message.CharacterState state; //玩家的状态
    public Character character; //角色
    public float rotateSpeed = 2.5f; //玩家转动速度
    public float turnAngle = 10; //玩家转动的角度
    public int speed; //移动速度

    public EntityController entityController;

    public bool onAir = false;
    private Vector3 lastPos;
    private float lastSync = 0;

    private void Start()
    {
        state = SkillBridge.Message.CharacterState.Idle;
        if (this.character == null)
        {
            DataManager.Instance.Load();
            NCharacterInfo cinfo = new NCharacterInfo();
            cinfo.Id = 1;
            cinfo.Name = "Test";
            cinfo.ConfigId = 1;
            cinfo.Entity = new NEntity();
            cinfo.Entity.Position = new NVector3();
            cinfo.Entity.Direction = new NVector3();
            cinfo.Entity.Direction.X = 0;
            cinfo.Entity.Direction.Y = 100;
            cinfo.Entity.Direction.Z = 0;
            this.character = new Character(cinfo);

            if (entityController != null)
            {
                entityController.entity = this.character;
            }
        }
    }

    private void FixedUpdate()
    {
        if (character == null)
        {
            return;
        }

        if (InputManager.Instance.IsInputMode)
        {
            return;
        }

        float v = Input.GetAxis("Vertical"); //垂直

        if (v > 0.01)
        {
            if (state != SkillBridge.Message.CharacterState.Move)
            {
                state = SkillBridge.Message.CharacterState.Move;
                this.character.MoveForward();
                this.SendEntityEvent(EntityEvent.MoveFwd);
            }
            this.rb.velocity = this.rb.velocity.y * Vector3.up + GameObjectTool.LogicToWorld(character.direction) * (this.character.speed + 15f) / 100f;
        }
        else if (v < -0.01)
        {
            if (state != SkillBridge.Message.CharacterState.Move)
            {
                state = SkillBridge.Message.CharacterState.Move;
                this.character.MoveBack();
                this.SendEntityEvent(EntityEvent.MoveBack);
            }
            this.rb.velocity = this.rb.velocity.y * Vector3.up + GameObjectTool.LogicToWorld(character.direction) * (this.character.speed + 15f) / 100f;
        }
        else
        {
            if (state != SkillBridge.Message.CharacterState.Idle)
            {
                state = SkillBridge.Message.CharacterState.Idle;
                this.rb.velocity = Vector3.zero;
                this.character.Stop();
                this.SendEntityEvent(EntityEvent.Idle);
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            this.SendEntityEvent(EntityEvent.Jump);
        }

        float h = Input.GetAxis("Horizontal"); //水平

        if (h < -0.1 || h > 0.1)
        {
            this.transform.Rotate(0, h * rotateSpeed, 0);
            Vector3 dir = GameObjectTool.LogicToWorld(character.direction);
            Quaternion rot = new Quaternion();
            rot.SetFromToRotation(dir, this.transform.forward);

            if (rot.eulerAngles.y > this.turnAngle && rot.eulerAngles.y < (360 - this.turnAngle))
            {
                character.SetDirection(GameObjectTool.WorldToLogic(this.transform.forward));
                rb.transform.forward = this.transform.forward;
                this.SendEntityEvent(EntityEvent.None);
            }

        }
    }

    private void LateUpdate()
    {
        if (this.character == null)
        {
            return;
        } 

        Vector3 offset = this.rb.transform.position - lastPos;
        this.speed = (int)(offset.magnitude * 500f / Time.deltaTime);
        this.lastPos = this.rb.transform.position;

        if ((GameObjectTool.WorldToLogic(this.rb.transform.position) - this.character.position).magnitude > 50)
        {
            this.character.SetPosition(GameObjectTool.WorldToLogic(this.rb.transform.position));
            this.SendEntityEvent(EntityEvent.None);
        }
        this.transform.position = this.rb.transform.position;
    }

    private void SendEntityEvent(EntityEvent entityEvent)
    {
        if (entityController != null)
        {
            entityController.OnEntityEvent(entityEvent);
        }

        MapService.Instance.SendMapEntitySync(entityEvent, this.character.EntityData);
    }
}
