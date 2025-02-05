﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;
using Common;
using Common.Data;
using Network;
using GameServer.Managers;
using GameServer.Entities;

namespace GameServer.Services
{
    class MapService : Singleton<MapService>
    {
        public MapService()
        {
            //MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<MapCharacterEnterRequest>(this.OnMapCharacterEnter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<MapEntitySyncRequest>(this.OnMapEntitySync);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<MapTeleportRequest>(this.OnMapTeleport);

        }

        private void OnMapTeleport(NetConnection<NetSession> sender, MapTeleportRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("OnMapTeleport : characterID:{0} : {1} TeleporterId:{2}", character.Id, character.Data, request.teleporterId);

            if (!DataManager.Instance.Teleporters.ContainsKey(request.teleporterId))
            {
                Log.WarningFormat("Source TeleporterID[{0}] not existed", request.teleporterId);
            }

            TeleporterDefine source = DataManager.Instance.Teleporters[request.teleporterId];

            if (source.LinkTo == 0 || !DataManager.Instance.Teleporters.ContainsKey(source.LinkTo))
            {
                Log.WarningFormat("Source TeleporterID[{0}] LinkTo ID [{1}] not existed", request.teleporterId, source.LinkTo);
            }

            TeleporterDefine target = DataManager.Instance.Teleporters[source.LinkTo];

            MapManager.Instance[source.MapID].CharacterLeave(character);
            character.Position = target.Position;
            character.Direction = target.Direction;
            MapManager.Instance[target.MapID].CharacterEnter(sender, character);
        }

        public void Init()
        {
            MapManager.Instance.Init();
        }

        private void OnMapEntitySync(NetConnection<NetSession> sender, MapEntitySyncRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("OnMapEntitySync: characterID:{0} :{1} Entity.Id:{2} Event:{3} Entity:{4}", character.Id, character.Info.Name, request.entitySync.Id, request.entitySync.Event, request.entitySync.Entity.String());
            MapManager.Instance[character.Info.mapId].UpdateEntity(request.entitySync);
        }

        private void OnMapCharacterEnter(NetConnection<NetSession> sender, MapCharacterEnterRequest request)
        {

        }

        //发送给其他玩家 同步状态
        internal void SendEntityUpdate(NetConnection<NetSession> connection, NEntitySync entity)
        {
            connection.Session.Response.mapEntitySync = new MapEntitySyncResponse();
            connection.Session.Response.mapEntitySync.entitySyncs.Add(entity);

            connection.SendResponse();
        }
    }
}
