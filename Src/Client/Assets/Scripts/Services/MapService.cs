﻿using System;
using Common.Data;
using SkillBridge.Message;
using Models;
using Managers;
using Network;
using UnityEngine;

namespace Services
{
    public class MapService : Singleton<MapService>, IDisposable
    {
        public int CurrentMapId = 0;

        public MapService()
        {
            MessageDistributer.Instance.Subscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Subscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            MessageDistributer.Instance.Subscribe<MapEntitySyncResponse>(this.OnMapEntitySync);
        }

        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<MapCharacterEnterResponse>(this.OnMapCharacterEnter);
            MessageDistributer.Instance.Unsubscribe<MapCharacterLeaveResponse>(this.OnMapCharacterLeave);
            MessageDistributer.Instance.Unsubscribe<MapEntitySyncResponse>(this.OnMapEntitySync);
        }

        public void Init()
        { 
        
        }

        /// <summary>
        /// 角色进入地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnMapCharacterEnter(object sender, MapCharacterEnterResponse response)
        {
            Debug.LogFormat("OnMapCharacterEnter:Map:{0} Count:{1}", response.mapId, response.Characters.Count);

            foreach (var cha in response.Characters)
            {
                if (User.Instance.CurrentCharacter == null || (cha.Type == CharacterType.Player) && User.Instance.CurrentCharacter.Id == cha.Id)
                {
                    User.Instance.CurrentCharacter = cha; //刷新一遍角色数据
                }

                CharacterManager.Instance.AddCharacter(cha); //遍历当前地图所有角色 交给角色管理器

                if (CurrentMapId != response.mapId)
                {
                    this.EnterMap(response.mapId); //进入新地图
                    this.CurrentMapId = response.mapId; //更新当前所在地图的地图编号
                }
            }
        }

        /// <summary>
        /// 角色离开地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void OnMapCharacterLeave(object sender, MapCharacterLeaveResponse response)
        {
            Debug.LogFormat("OnMapCharacterLeave:CharID:{0}", response.entityId);

            if (response.entityId != User.Instance.CurrentCharacter.EntityId)
            {
                CharacterManager.Instance.RemoveCharacter(response.entityId);
            }
            else
            {
                CharacterManager.Instance.Clear();                    
            }
        }

        /// <summary>
        /// 进入地图
        /// </summary>
        /// <param name="mapId"></param>
        private void EnterMap(int mapId)
        {
            if (DataManager.Instance.Maps.ContainsKey(mapId)) //判断传来的地图编号是否存在
            {
                MapDefine map = DataManager.Instance.Maps[mapId];
                User.Instance.CurrentMapData = map;
                SceneManager.Instance.LoadScene(map.Resource); //跳转场景
            }
            else
            {
                Debug.LogErrorFormat("EnterMap: Map {0} not existed", mapId);
            }
        }

        /// <summary>
        /// 发送同步请求
        /// </summary>
        /// <param name="entityEvent"></param>
        /// <param name="entity"></param>
        public void SendMapEntitySync(EntityEvent entityEvent, NEntity entity)
        {
            Debug.LogFormat("MapEntityUpdateRequest:ID:{0} Pos:{1} Dir:{2} Speed:{3}", entity.Id, entity.Position.String(), entity.Direction.String(), entity.Speed);
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapEntitySync = new MapEntitySyncRequest();
            message.Request.mapEntitySync.entitySync = new NEntitySync()
            {
                Id = entity.Id,
                Event = entityEvent,
                Entity = entity
            };
            NetClient.Instance.SendMessage(message);
        }

        /// <summary>
        /// 客户端收到同步请求的响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="responce"></param>
        private void OnMapEntitySync(object sender, MapEntitySyncResponse responce)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendFormat("MapEntityUpdateResponse: Entity:{0}", responce.entitySyncs.Count);
            sb.AppendLine();

            foreach (var entity in responce.entitySyncs)
            {
                EntityManager.Instance.OnEntitySync(entity);

                sb.AppendFormat("[{0}evt:{1} entity:{2}]", entity.Id, entity.Event, entity.Entity.String());
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }

        internal void SendMapTeleport(int teleporterID)
        {
            Debug.LogFormat("");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.mapTeleport = new MapTeleportRequest();
            message.Request.mapTeleport.teleporterId = teleporterID;
            NetClient.Instance.SendMessage(message);
        }
    }
}
