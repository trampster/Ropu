using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared.Groups;
using Ropu.Shared.LoadBalancing;

namespace Ropu.LoadBalancer
{

    public interface IGroupCallControllerListener
    {
        void GroupsChanged(IEnumerable<GroupCallController> groupCallcontrollers);
    }

    public class CallControllerRegistry
    {
        readonly IPEndPoint[] _groupLookup;
        readonly RegisteredCallController[] _controllers;
        const int MaxControllers = 256;
        const int MaxGroups = ushort.MaxValue;
        int _nextIndex = 0;
        int _count = 0;
        readonly IGroupsClient _groupsClient;
        IGroupCallControllerListener _listener;
        readonly Queue<ushort> _unassignedGroups = new Queue<ushort>();

        public CallControllerRegistry(IGroupsClient groupsClient)
        {
            _groupLookup = new IPEndPoint[MaxGroups];
            _controllers = new RegisteredCallController[MaxControllers];
            _groupsClient = groupsClient;
            foreach(var group in _groupsClient.Groups)
            {
                _unassignedGroups.Enqueue(group.Id);
            }
        }

        public IEnumerable<GroupCallController> GroupCallControllers
        {
            get
            {
                foreach(var controller in Controllers)
                {
                    foreach(var groupId in controller.Groups)
                    {
                        yield return new GroupCallController()
                        {
                            GroupId = groupId,
                            EndPoint = controller.CallEndPoint
                        };
                    }
                }
            }
        }

        public void SetGroupCallControllerListener(IGroupCallControllerListener listener)
        {
            _listener = listener;
        }

        void IncrementNextIndex()
        {
            _nextIndex = (_nextIndex + 1) % MaxControllers;
        }

        void FindNextIndex()
        {
            while(true)
            {
                IncrementNextIndex();
                if(_controllers[_nextIndex] == null)
                {
                    return;
                }
            }
        }

        public byte? Register(IPEndPoint callManagementEndpoint, RegisteredCallController registeredCallController)
        {
            if(_count == MaxControllers)
            {
                return null; //to many call controllers
            }
            _controllers[_nextIndex] = registeredCallController;
            byte controllerId = (byte)_nextIndex;
            while(registeredCallController.HasCapacity() && _unassignedGroups.Count != 0)
            {
                var groupId = _unassignedGroups.Dequeue();
                registeredCallController.AddGroup(groupId);
                _groupLookup[groupId] = registeredCallController.CallEndPoint;
                _listener?.GroupsChanged(new GroupCallController[]
                {
                    new GroupCallController()
                    {
                        EndPoint = registeredCallController.CallEndPoint, 
                        GroupId = groupId
                    }
                });
            }
            FindNextIndex();
            return controllerId;
        }

        public void RemoveExpired(Action<RegisteredCallController> onRemove)
        {
            for(int index = 0; index < MaxControllers; index++)
            {
                var controller = _controllers[index];
                if(controller != null && controller.IsExpired())
                {
                    Console.WriteLine("Controlling Node Expired");
                    _controllers[index] = null;
                    RedistributeGroups(controller.Groups);
                    _count--;
                    onRemove(controller);
                }
            }
        }

        IEnumerable<RegisteredCallController> Controllers
        {
            get
            {
                for(int index = 0; index < MaxControllers; index++)
                {
                    var controller = _controllers[index];
                    if(controller != null)
                    {
                        yield return controller;
                    }
                }
            }
        }

        void RedistributeGroups(IEnumerable<ushort> groups)
        {
            var list = new List<GroupCallController>();
            foreach(ushort group in groups)
            {
                _unassignedGroups.Enqueue(group);
            }
            foreach(var controller in Controllers)
            {
                if(_unassignedGroups.Count == 0)
                {
                    return;
                }
                while(controller.HasCapacity())
                {
                    var group = _unassignedGroups.Dequeue();
                    controller.AddGroup(_unassignedGroups.Dequeue());
                    list.Add(new GroupCallController(){EndPoint = controller.CallEndPoint, GroupId = group});
                }
            }

            _listener.GroupsChanged(list);
        }

    }
}