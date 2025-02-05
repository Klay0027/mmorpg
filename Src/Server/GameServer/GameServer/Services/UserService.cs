﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Network;
using SkillBridge.Message;
using GameServer.Entities;
using GameServer.Managers;

namespace GameServer.Services
{
    class UserService:Singleton<UserService>
    {
        public UserService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserRegisterRequest>(this.OnRegister);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserLoginRequest>(this.OnLogin);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserCreateCharacterRequest>(this.OnCreateCharacter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameEnterRequest>(this.OnGameEnter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameLeaveRequest>(this.OnGameLeave);
        }
        
        public void Init()
        {

        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
        {
            Log.InfoFormat("UserRegisterRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            sender.Session.Response.userRegister = new UserRegisterResponse();

            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user != null)
            {
                sender.Session.Response.userRegister.Result = Result.Failed;
                sender.Session.Response.userRegister.Errormsg = "用户已存在！";
            }
            else
            {
                TPlayer player = DBService.Instance.Entities.Players.Add(new TPlayer());
                DBService.Instance.Entities.Users.Add(new TUser() { Username = request.User, Password = request.Passward, Player = player, RegisterDate = DateTime.Now });
                DBService.Instance.Entities.SaveChanges();
                sender.Session.Response.userRegister.Result = Result.Success;
                sender.Session.Response.userRegister.Errormsg = "注册成功！";
            }

            sender.SendResponse();
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnLogin(NetConnection<NetSession> sender, UserLoginRequest request)
        {
            Log.InfoFormat("UserLoginRequest: User:{0}  Pass:{1}", request.User, request.Passward);
            sender.Session.Response.userLogin = new UserLoginResponse();

            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user != null)
            {
                if (user.Password == request.Passward)
                {
                    sender.Session.User = user;
                    sender.Session.Response.userLogin.Result = Result.Success;
                    sender.Session.Response.userLogin.Errormsg = "登录成功，欢迎来到声闻世界！";
                    sender.Session.Response.userLogin.Userinfo = new NUserInfo();
                    sender.Session.Response.userLogin.Userinfo.Id = (int)user.ID;
                    sender.Session.Response.userLogin.Userinfo.Player = new NPlayerInfo();
                    sender.Session.Response.userLogin.Userinfo.Player.Id = user.Player.ID;

                    foreach (var character in user.Player.Characters)
                    {
                        NCharacterInfo info = new NCharacterInfo();
                        info.Id = character.ID;
                        info.Name = character.Name;
                        info.Class = (CharacterClass)character.Class;
                        info.ConfigId = character.ID;
                        info.Type = CharacterType.Player;
                        sender.Session.Response.userLogin.Userinfo.Player.Characters.Add(info);
                    }
                    
                }
                else 
                {
                    sender.Session.Response.userLogin.Result = Result.Failed;
                    sender.Session.Response.userLogin.Errormsg = "用户名或密码不对！";
                }               
            }
            else
            {
                sender.Session.Response.userLogin.Result = Result.Failed;
                sender.Session.Response.userLogin.Errormsg = "用户不存在!请先注册~";
            }

            sender.SendResponse();
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnCreateCharacter(NetConnection<NetSession> sender, UserCreateCharacterRequest request)
        {
            Log.InfoFormat("CreateCharacter: Name:{0}  Class:{1}", request.Name, request.Class);

            //创建角色对象
            TCharacter character = new TCharacter();
            character.Class = (int)request.Class;
            character.Name = request.Name;
            character.TID = (int)request.Class;
            character.Level = 1;
            character.MapID = 1;
            character.MapPosX = 2200;
            character.MapPosY = 1900;
            character.MapPosZ = 800;
            character.Gold = 5000;
            character.Equips = new byte[28];
            //背包系统测试
            //start
            var bag = new TCharacterBag();
            bag.Character = character;
            bag.Items = new byte[0];
            bag.Unlocked = 10;
            TCharacterItems it = new TCharacterItems();
            character.CharacterBag = DBService.Instance.Entities.CharacterBag.Add(bag);
            //end
            character.Items.Add(new TCharacterItems()
            {
                Owner = character,
                ItemID = 1,
                ItemCount = 20,
            });

            character.Items.Add(new TCharacterItems()
            {
                Owner = character,
                ItemID = 2,
                ItemCount = 20,
            });

            //调用数据库服务新增角色存储到数据库
            character = DBService.Instance.Entities.Characters.Add(character);
            sender.Session.User.Player.Characters.Add(character); //添加角色session
            DBService.Instance.Entities.SaveChanges(); //保存数据

            sender.Session.Response.createChar = new UserCreateCharacterResponse();
            sender.Session.Response.createChar.Result = Result.Success;
            sender.Session.Response.createChar.Errormsg = "添加角色成功！";
            //遍历当前玩家有哪些角色
            foreach (var item in sender.Session.User.Player.Characters)
            {
                NCharacterInfo info = new NCharacterInfo();
                info.Id = item.ID;
                info.Name = item.Name;
                info.Class = (CharacterClass)item.Class;
                info.ConfigId = item.TID;
                info.Type = CharacterType.Player;
                sender.Session.Response.createChar.Characters.Add(info);
            }

            sender.SendResponse();
        }

        /// <summary>
        /// 进入游戏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnGameEnter(NetConnection<NetSession> sender, UserGameEnterRequest request)
        {
            TCharacter dbchar = sender.Session.User.Player.Characters.ElementAt(request.characterIdx);
            Log.InfoFormat("UserGameEnterRequest: characterID:{0}:{1} Map:{2}", dbchar.ID, dbchar.Name, dbchar.MapID);

            Character character = CharacterManager.Instance.AddCharacter(dbchar);
            SessionManager.Instance.AddSession(character.Id, sender);
            sender.Session.Response.gameEnter = new UserGameEnterResponse();
            sender.Session.Response.gameEnter.Result = Result.Success;
            sender.Session.Response.gameEnter.Errormsg = "Login Success!";

            sender.Session.Character = character;
            sender.Session.PostResponser = character;

            sender.Session.Response.gameEnter.Character = character.Info;
            sender.SendResponse();

            MapManager.Instance[dbchar.MapID].CharacterEnter(sender, character);
        }

        /// <summary>
        /// 离开游戏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnGameLeave(NetConnection<NetSession> sender, UserGameLeaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("UserGameLeaveRequest: characterID:{0}:{1} Map:{2}", character.Id, character.Info.Name, character.Info.mapId);
           
            CharacterLeave(character);
            sender.Session.Response.gameLeave = new UserGameLeaveResponse();
            sender.Session.Response.gameLeave.Result = Result.Success;
            sender.Session.Response.gameLeave.Errormsg = "None";

            sender.SendResponse();
        }

        public void CharacterLeave(Character character)
        {
            Log.InfoFormat("CharacterLeave:character:{0}离开游戏", character.Name);
            //玩家离开 从Session管理器中移除
            SessionManager.Instance.RemoveSession(character.Id);
            //根据角色ID在角色管理器中从字典Characters移除
            CharacterManager.Instance.RemoveCharacter(character.Id);

            character.Clear();
            //根据角色信息在地图管理器中从当前地图字典中移除
            MapManager.Instance[character.Info.mapId].CharacterLeave(character);
        }
    }
}
