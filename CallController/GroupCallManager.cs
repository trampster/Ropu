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

        public GroupCallManager(ushort groupId, RopuProtocol ropuProtocol, ServingNodes servingNodes)
        {
            _groupId = groupId;
            _ropuProtocol = ropuProtocol;
            _servingNodes = servingNodes;
        }

        DateTime _lastActivity;
        CancellationTokenSource _callCancellationTokenSource;
        
        public void StartCall(uint userId)
        {
            if(_callInProgress)
            {
                Console.WriteLine($"Called start requested for group {_groupId} by {userId} but call is already in progress");
                return;
            }
            _callInProgress = true;
            _talker = userId;
            _callInitiator = userId;


            var endPointsReader = _servingNodes.EndPoints;
            _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, endPointsReader.GetSnapShot());
            endPointsReader.Release();

            Console.WriteLine($"Called started with group {_groupId} initiator {userId}");

            _callCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _callCancellationTokenSource.Token;
            RunIdleTimer(cancellationToken);
            RunPeriodicUpdates(cancellationToken);
        }

        async void RunPeriodicUpdates(CancellationToken token)
        {
            try
            {
                while(!token.IsCancellationRequested)
                {
                    await Task.Delay(2000, token);
                    var endPointsReader = _servingNodes.EndPoints;

                    if(_talker == null)
                    {
                        _ropuProtocol.SendFloorIdle(_groupId, endPointsReader.GetSnapShot());
                        endPointsReader.Release();
                        continue;
                    }
                    _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, endPointsReader.GetSnapShot());
                    endPointsReader.Release();
                }
            }
            catch(TaskCanceledException)
            {
            }
        }

        async void RunIdleTimer(CancellationToken token)
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
                        _ropuProtocol.SendCallEnded(_groupId, endPointsReader.GetSnapShot());
                        endPointsReader.Release();
                        _callInProgress = false;
                        Console.WriteLine($"Call ended because idle timer expired after {callHangTime/1000} seconds.");
                        _callCancellationTokenSource.Cancel();
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
            _ropuProtocol.SendFloorIdle(_groupId, endPointsReader.GetSnapShot());
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
                _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, endPointsReader.GetSnapShot());
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
            _ropuProtocol.SendFloorTaken(userId, _groupId, endPointsReader.GetSnapShot());
            endPointsReader.Release();

        }
    }
}