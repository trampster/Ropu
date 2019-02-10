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

        ISetReader<IPEndPoint> _servingNodesReader;

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


            _servingNodesReader?.Release();
            _servingNodesReader = _servingNodes.EndPoints;
            _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, _servingNodesReader.GetSpan());

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
                    if(_talker == null)
                    {
                        _ropuProtocol.SendFloorIdle(_groupId, _servingNodesReader.GetSpan());
                        continue;
                    }
                    _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, _servingNodesReader.GetSpan());
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
                        _ropuProtocol.SendCallEnded(_groupId, _servingNodesReader.GetSpan());
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
            Console.WriteLine("Releasing Floor");
            _talker = null;
            _ropuProtocol.SendFloorIdle(_groupId, _servingNodesReader.GetSpan());
        }

        public void FloorRequest(uint userId)
        {
            if(_talker == userId)
            {
                Console.WriteLine($"Got floor request from {userId} but they already have the floor");
                //send another floor taken so they figure it out
                _ropuProtocol.SendFloorTaken(_talker.Value, _groupId, _servingNodesReader.GetSpan());
                return;
            }
            if(_talker != null)
            {
                Console.WriteLine($"Got floor request from {userId} but {_talker.Value} has the floor");
                _ropuProtocol.SendFloorDenied(_groupId, userId);
            }
            Console.WriteLine($"Floor granted to {userId} for group {_groupId}");
            _talker = userId;
            _ropuProtocol.SendFloorTaken(userId, _groupId, _servingNodesReader.GetSpan());

        }
    }
}