﻿using NoNameLib.Logic.Position;
using Server.Interfaces;
using Server.Logic.Enums;
using Server.Networking.Messages;

namespace Server.Creatures
{
    public class Player : Creature
    {
        #region Fields 

        private IClientConnection clientConnection;

        private bool isAuthenticated;

        #endregion

        public Player(ICreatureDataProvider dataProvider, IClientConnection connection)
            : base(dataProvider)
        {
            clientConnection = connection;
            clientConnection.OwnerId = this.UniqueId;
            clientConnection.OnMessageReceived += OnMessageReceived;
        }

        public void Initialize()
        {
            DataProvider.LoadPlayerData(this);
            
            // TODO: Send player info to client
        }

        public void SendMessage(MessageBase message)
        {
            if (clientConnection != null)
            {
                clientConnection.SendMessage(message);
            }
        }

        /// <summary>
        /// Disconnect and destroy player object
        /// </summary>
        public void Destroy()
        {
            if (clientConnection != null)
                clientConnection.Disconnect();

            clientConnection = null;
        }

        #region Movement

        public override void MoveFailed()
        {
            if (clientConnection != null)
                clientConnection.MoveFailed(this.Position);
        }

        public override bool OnCreatureMoved(Creature creature, Position oldPosition, Position newPosition, bool isTeleport)
        {
            bool movedInsideViewport = base.OnCreatureMoved(creature, oldPosition, newPosition, isTeleport);
            if (movedInsideViewport)
            {
                if (clientConnection != null)
                    clientConnection.CreatureMoved(creature, oldPosition, newPosition, isTeleport);
            }

            return movedInsideViewport;
        }

        public override bool AddVisibleCreature(Creature creature)
        {
            var isAdded = base.AddVisibleCreature(creature);
            if (isAdded)
            {
                if (clientConnection != null)
                    clientConnection.CreatureVisibleAdd(creature);
            }
            return isAdded;
        }

        public override bool RemoveVisibleCreature(Creature creature)
        {
            var isRemoved = base.RemoveVisibleCreature(creature);
            if (isRemoved)
            {
                if (clientConnection != null)
                    clientConnection.CreatureVisibleRemove(creature);
            }
            return isRemoved;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when new message is received from IClientConnection implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void OnMessageReceived(object sender, MessageBase message)
        {
            if (message.RestrictedAccess != isAuthenticated)
                return;

            var messageType = message.GetMessageType();
            if (messageType == MessageType.Authenticate)
            {
                Authenticate((AuthenticateMessage)message);
            }
            else if (messageType == MessageType.Walk)
            {
                var walkMessage = (WalkMessage)message;
                Move(walkMessage.Direction);
            }
        }

        #endregion

        #region Message Methods

        private void Authenticate(AuthenticateMessage message)
        {
            var result = DataProvider.Authenticate(this, message);
            if (result == AuthenticateResult.Success)
            {
                isAuthenticated = true;
            }

            
            // Send message back to client with the result
            clientConnection.AuthenticateResult(result);

            // Initialize player data if authenticated
            if (isAuthenticated)
                Initialize();
        }

        #endregion
    }
}
