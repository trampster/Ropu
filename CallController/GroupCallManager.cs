using System;
using System.Net;
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
            _ropuProtocol.SendCallStarted(userId, _groupId, _servingNodesReader.GetSpan());

            Console.WriteLine($"Called started with group {_groupId} initiator {userId}");

            RunIdleTimer();
        }

        async void RunIdleTimer()
        {
            _lastActivity = DateTime.UtcNow;
            const int callHangTime = 30000;
            while(true)
            {
                var now = DateTime.UtcNow;
                var expiryTime = _lastActivity.AddMilliseconds(callHangTime);
                if(now > expiryTime)
                {
                    //end the call
                    _ropuProtocol.SendCallEnded(_groupId, _servingNodesReader.GetSpan());
                    _callInProgress = false;
                    Console.WriteLine($"Call ended because idle timer expired after {callHangTime/1000} seconds.");

                    return;
                }
                int timeToWait = (int)expiryTime.Subtract(now).TotalMilliseconds;
                await Task.Delay(timeToWait);
            }
        }
    }
}