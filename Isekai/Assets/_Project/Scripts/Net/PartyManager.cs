using System.Collections.Generic;
using Isekai.Core.Events;
using Isekai.Gameplay.Party;
using Isekai.Gameplay.Targeting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai.Net
{
    // shortcut: one party per session, living in the TestLevel scene; move to a persistent server-spawned object once levels change mid-session.
    // shortcut: no rate limit on invites; add one before public matchmaking.
    public class PartyManager : NetworkBehaviour
    {
        private const ulong NoInviter = ulong.MaxValue;

        [SerializeField] private PartyEventChannel _channel;
        [SerializeField] private TextEventChannel _pickupPrompt; // showing = a pickup is in reach and E is for it
        [SerializeField] private InputAction _interact = new("PartyInteract", InputActionType.Button, "<Keyboard>/e");
        [SerializeField] private InputAction _leave = new("PartyLeave", InputActionType.Button, "<Keyboard>/l");
        [SerializeField] private float _inviteRange = 5f;
        [SerializeField] private float _inviteSeconds = 15f;

        private readonly NetworkList<ulong> _members = new();
        private readonly PartyRoster _roster = new();
        private readonly PendingInvites _invites = new();
        private readonly List<ulong> _scratch = new();
        private readonly List<PlayerCandidate> _candidates = new();
        private ulong _inviter = NoInviter;

        public override void OnNetworkSpawn()
        {
            _members.OnListChanged += OnMembersChanged;
            _interact.performed += OnInteract;
            _leave.performed += OnLeave;
            _interact.Enable();
            _leave.Enable();
            if (IsServer) NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            Publish();
        }

        public override void OnNetworkDespawn()
        {
            _members.OnListChanged -= OnMembersChanged;
            _interact.performed -= OnInteract;
            _leave.performed -= OnLeave;
            _interact.Disable();
            _leave.Disable();
            if (IsServer && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _channel.Raise(null);
        }

        private void Update()
        {
            if (!IsServer) return;
            _invites.CollectExpired(Time.time, _scratch);
            foreach (var target in _scratch) ClearInvite(target);
        }

        // E accepts a pending invite, otherwise invites the nearest player. A pickup in reach takes E first.
        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!string.IsNullOrEmpty(_pickupPrompt.Current)) return;
            if (_inviter != NoInviter) AcceptRpc();
            else InviteRpc();
        }

        private void OnLeave(InputAction.CallbackContext context) => LeaveRpc();

        [Rpc(SendTo.Server)]
        private void InviteRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;
            var senderObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(sender);
            if (senderObject == null || !_roster.CanInvite(sender)) return;

            _candidates.Clear();
            foreach (var id in NetworkManager.ConnectedClientsIds)
            {
                if (id == sender || _roster.Contains(id)) continue;
                var other = NetworkManager.SpawnManager.GetPlayerNetworkObject(id);
                if (other != null) _candidates.Add(new PlayerCandidate(id, other.transform.position));
            }

            var target = NearestPlayer.Pick(senderObject.transform.position, _candidates, _inviteRange);
            if (!target.HasValue) return;
            _invites.Add(sender, target.Value, Time.time, _inviteSeconds);
            ShowInviteRpc(sender, RpcTarget.Single(target.Value, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        private void AcceptRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;
            if (!_invites.TryConsume(sender, Time.time, out var inviter)) return;
            ClearInvite(sender);
            if (_roster.TryJoin(inviter, sender)) Sync();
        }

        [Rpc(SendTo.Server)]
        private void LeaveRpc(RpcParams rpcParams = default)
        {
            if (_roster.Remove(rpcParams.Receive.SenderClientId)) Sync();
        }

        // Server-only so a client cannot show a fake invite to someone else.
        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void ShowInviteRpc(ulong inviterId, RpcParams rpcParams = default)
        {
            _inviter = inviterId;
            Publish();
        }

        private void ClearInvite(ulong target) => ShowInviteRpc(NoInviter, RpcTarget.Single(target, RpcTargetUse.Temp));

        private void OnClientDisconnected(ulong clientId)
        {
            _invites.Remove(clientId, _scratch);
            foreach (var target in _scratch) ClearInvite(target);
            if (_roster.Remove(clientId)) Sync();
        }

        // The roster is plain C# on the server; copy it into the replicated list.
        private void Sync()
        {
            _members.Clear();
            foreach (var member in _roster.Members) _members.Add(member);
        }

        private void OnMembersChanged(NetworkListEvent<ulong> change) => Publish();

        private void Publish()
        {
            var names = new string[_members.Count];
            var self = -1;
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = "Player " + _members[i];
                if (_members[i] == NetworkManager.LocalClientId) self = i;
            }
            _channel.Raise(new PartyStatus(names, self, _inviter == NoInviter ? null : "Player " + _inviter));
        }
    }
}
