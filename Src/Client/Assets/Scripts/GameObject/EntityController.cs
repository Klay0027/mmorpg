﻿using UnityEngine;
using Entities;
using SkillBridge.Message;
using Managers;

public class EntityController : MonoBehaviour, IEntityNotify
{
    public Animator anim;
    public Rigidbody rb;
    private AnimatorStateInfo currentBaseState;

    public Entity entity;

    public UnityEngine.Vector3 position;
    public UnityEngine.Vector3 direction;
    private Quaternion rotation;

    public UnityEngine.Vector3 lastPosition;
    private Quaternion lastRotation;

    public float speed;
    public float animSpeed = 2f;
    public float jumpPower = 3.5f;

    public bool isPlayer = false;

    private void Start()
    {
        if (entity != null)
        {
            EntityManager.Instance.RegisterEntityChangeNotify(entity.entityId, this);
            this.UpdateTransform();
        }

        if (!this.isPlayer)
        {
            rb.useGravity = false;
        }
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            Debug.LogFormat("{0} OnDestroy :ID:{1} POS:{2} DIR:{3} SPD:{4} ", this.name, entity.entityId, entity.position, entity.direction, entity.speed);
        }

        if (UIWorldElementManager.Instance != null)
        {
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
    }

    private void FixedUpdate()
    {
        if (this.entity == null)
        {
            return;
        }

        this.entity.OnUpdate(Time.fixedDeltaTime);

        if (!this.isPlayer)
        {
            this.UpdateTransform();
        }
    }

    private void UpdateTransform()
    {
        this.position = GameObjectTool.LogicToWorld(entity.position);
        this.direction = GameObjectTool.LogicToWorld(entity.direction);

        this.rb.MovePosition(this.position);
        this.transform.forward = this.direction;
        this.lastPosition = this.position;
        this.lastRotation = this.rotation;
    }

    public void OnEntityEvent(EntityEvent entityEvent)
    {
        switch (entityEvent)
        {
            case EntityEvent.Idle:
                anim.SetBool("Move", false);
                anim.SetTrigger("Idle");
                break;
            case EntityEvent.MoveFwd:
                anim.SetBool("Move", true);
                break;
            case EntityEvent.MoveBack:
                anim.SetBool("Move", true);
                break;
            case EntityEvent.Jump:
                anim.SetTrigger("Jump");
                break;
        }
    }

    //当收到角色删除时
    public void OnEntityRemoved()
    {
        if (UIWorldElementManager.Instance != null)
        {
            //删除血条
            UIWorldElementManager.Instance.RemoveCharacterNameBar(this.transform);
        }
        //销毁自己
        Destroy(this.gameObject);
    }

    public void OnEntityChanged(Entity entity)
    {
        Debug.LogFormat("MapEntityUpdateRequest:ID:{0} Pos:{1} Dir:{2} Speed:{3}", entity.entityId, entity.position, entity.direction,  entity.speed);
    }
}
