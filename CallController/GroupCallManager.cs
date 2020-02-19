using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.Concurrent;

namespace Ropu.CallController
{
    public class GroupCallManager
    {
        readonly RopuProtocol _ropuProtocol;
        readonly ServingNodes _servingNodes;
        readonly ushort _groupId;
        bool _callInProgress;
        uint _callInitiator;
        uint? _talker;
        readonly KeysClient _keysClient;
        CachedEncryptionKey? _keyInfo;


        public GroupCallManager(ushort groupId, RopuProtocol ropuProtocol, ServingNodes servingNodes, KeysClient keysClient)
        {
            _groupId = groupId;
            _ropuProtocol = ropuProtocol;
            _servingNodes = servingNodes;
            _keysClient = keysClient;
        }

        DateTime _lastActivity;
        CancellationTokenSource? _callCancellationTokenSource;
        

        CachedEncryptionKey KeyInfo
        {
            get
            {
                if(_keyInfo == null)
                {
                    throw new InvalidOperationException("Group Call request KeyInfo but call isn't started yet");
                }
                return _keyInfo;
            }
        }
        public async Task StartCall(uint userId)
        {
            if(_callInProgress)
            {
                Console.WriteLine($"Called start requested for group {_groupId} by {userId} but call is already in progress");
                return;
            }
            _callInProgress = true;
            _talker = userId;
            _callInitiator = userId;

            var keyInfo = await _keysClient.GetGroupKey(_groupId);
            while(keyInfo == null)
            {
                Console.Error.WriteLine("Failed to get key for group {_groupId}");
                await Task.Delay(1000);
                keyInfo = await _keysClient.GetGroupKey(_groupId);
            }
            _keyInfo = keyInfo;

            var endPointsReader = _servingNodes.EndPoints;
            _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, endPointsReader.GetSnapShot(), _keyInfo);
            endPointsReader.Release();

            Console.WriteLine($"Called started with group {_groupId} initiator {userId}");

            _callCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _callCancellationTokenSource.Token;
            var idleTask = RunIdleTimer(cancellationToken);
            var updatesTask = RunPeriodicUpdates(cancellationToken);
            Task.WaitAll(idleTask, updatesTask);
        }

        async Task RunPeriodicUpdates(CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    await Task.Delay(2000, token);
                    var endPointsReader = _servingNodes.EndPoints;

                    if(_talker == null)
                    {
                        _ropuProtocol.SendFloorIdle(_groupId, endPointsReader.GetSnapShot(), KeyInfo);
                        endPointsReader.Release();
                        continue;
                    }
                    _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, endPointsReader.GetSnapShot(), KeyInfo);
                    endPointsReader.Release();
                }
            }
            catch(TaskCanceledException)
            {
            }
        }

        async Task RunIdleTimer(CancellationToken token)
        {
            try
            {
                _lastActivity = DateTime.UtcNow;
                const int callHangTime = 30000;
                while(!token.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    var expiryTime = _lastActivity.AddMilliseconds(callHangTime);
                    if(now > expiryTime)
                    {
                        //end the call
                        var endPointsReader = _servingNodes.EndPoints;
                        var keyInfo = await _keysClient.GetGroupKey(_groupId);
                        if(keyInfo == null)
                        {
                            Console.Error.WriteLine($"Could not get key info for group {_groupId}");
                            return;
                        }
                        _ropuProtocol.SendCallEnded(_groupId, endPointsReader.GetSnapShot(), keyInfo);
                        endPointsReader.Release();
                        _callInProgress = false;
                        Console.WriteLine($"Call ended because idle timer expired after {callHangTime/1000} seconds.");
                        _callCancellationTokenSource?.Cancel();
                        return;
                    }
                    int timeToWait = (int)expiryTime.Subtract(now).TotalMilliseconds;
                    await Task.Delay(timeToWait, token);
                }
            }
            catch(TaskCanceledException)
            {
            }
        }

        public void FloorReleased(uint userId)
        {
            if(_talker == null)
            {
                Console.WriteLine($"Got floor released from {userId} but the floor is idle");
                return;
            }

            if(_talker != userId)
            {
                Console.WriteLine($"Got floor released from {userId} but {_talker} has the floor");
                return;
            }
            _lastActivity = DateTime.UtcNow;

            Console.WriteLine("Releasing Floor");
            _talker = null;

            var endPointsReader = _servingNodes.EndPoints;
            _ropuProtocol.SendFloorIdle(_groupId, endPointsReader.GetSnapShot(), KeyInfo);
            endPointsReader.Release();
        }

        public void FloorRequest(uint userId)
        {
            _lastActivity = DateTime.UtcNow;
            var endPointsReader = _servingNodes.EndPoints;

            if(_talker == userId)
            {
                Console.WriteLine($"Got floor request from {userId} but they already have the floor");
                //send another floor taken so they figure it out
                _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, endPointsReader.GetSnapShot(), KeyInfo);
                endPointsReader.Release();

                return;
            }
            if(_talker != null)
            {
                Console.WriteLine($"Got floor request from {userId} but {_talker.Value} has the floor");
                _ropuProtocol.SendFloorDenied(_groupId, userId);
            }
            Console.WriteLine($"Floor granted to {userId} for group {_groupId}");
            _talker = userId;
            _ropuProtocol.SendFloorTaken(userId, _groupId, endPointsReader.GetSnapShot(), KeyInfo);
            endPointsReader.Release();
        }
    }
}