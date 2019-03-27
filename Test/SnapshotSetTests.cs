using System;
using System.Net;
using NUnit.Framework;
using Ropu.ServingNode;
using Ropu.Shared.Concurrent;

namespace Ropu.Tests
{
    public class SnapshotSetTest
    {
        [Test]
        public void Add_NotLocked_IsAdded()
        {
            //arrange
            var set = new SnapshotSet<int>(10);

            //act
            set.Add(42);

            //assert
            Assert.That(set.GetSnapShot()[0], Is.EqualTo(42));
        }

        [Test]
        public void Add_Locked_IsAdded()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.GetSnapShot();

            //act
            set.Add(42);
            set.Release();

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(42));
        }

        [Test]
        public void AddMultiple_Locked_IsAdded()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(41);
            set.GetSnapShot();

            //act
            set.Add(42);
            set.Add(43);
            set.Release();

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(41));
            Assert.That(snapshot[1], Is.EqualTo(42));
            Assert.That(snapshot[2], Is.EqualTo(43));
        }

        [Test]
        public void Add_FifthItem_IsAdded()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(4);

            //act
            set.Add(5); //requires the set to increase in size, (starts with 4)

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[4], Is.EqualTo(5));
        }

        [Test]
        public void Remove_NotLocked_IsRemoved()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Add(2);
            set.Add(3);

            //act
            set.Remove(2);

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(1));
            Assert.That(snapshot[1], Is.EqualTo(3));
            Assert.That(snapshot.Length, Is.EqualTo(2));
        }

        [Test]
        public void Remove_Locked_IsRemoved()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.GetSnapShot();

            //act
            set.Remove(2);
            set.Release();

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(1));
            Assert.That(snapshot[1], Is.EqualTo(3));
            Assert.That(snapshot.Length, Is.EqualTo(2));
        }

        [Test]
        public void Remove_LastItem_IsRemoved()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);

            //act
            set.Remove(1);

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot.Length, Is.EqualTo(0));
        }

        [Test]
        public void AddAndRemove_Locked_CorrectContents()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.GetSnapShot();

            //act
            set.Remove(2);
            set.Add(4);
            set.Remove(1);
            set.Release();

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(4));
            Assert.That(snapshot[1], Is.EqualTo(3));
            Assert.That(snapshot.Length, Is.EqualTo(2));
        }

        [Test]
        public void AddAndRemoveAll_Add_CorrectContents()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Add(2);

            //act
            set.Remove(2);
            set.Remove(1);
            set.Add(3);

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(3));
            Assert.That(snapshot.Length, Is.EqualTo(1));
        }

        [Test]
        public void AddRemoveAddRemove_Add_CorrectContents()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Remove(1);
            set.Add(2);
            set.Remove(2);

            //act
            set.Add(3);

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(3));
            Assert.That(snapshot.Length, Is.EqualTo(1));
        }

        [Test]
        public void AddRemoveAddRemove_AddSame_CorrectContents()
        {
            //arrange
            var set = new SnapshotSet<int>(10);
            set.Add(1);
            set.Remove(1);;

            //act
            set.Add(1);

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot[0], Is.EqualTo(1));
            Assert.That(snapshot.Length, Is.EqualTo(1));
        }

        [Test]
        public void Test_FromLogs()
        {
            //arrange
            var set = new SnapshotSet<IPEndPoint>(10);
            
            var deadpool = new UserIPEndPoint(2000, new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5061));
            set.Add(deadpool);
            
            var ironMan = new UserIPEndPoint(1004, new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5062));
            set.Add(ironMan);

            set.Remove(ironMan);
            ironMan = new UserIPEndPoint(1004, new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5062));
            set.Add(ironMan);

            set.Remove(deadpool);
            deadpool = new UserIPEndPoint(2000, new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5061));
            set.Add(deadpool);

            set.Remove(ironMan);
            ironMan = new UserIPEndPoint(1004, new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5061));
            set.Add(ironMan);

            set.Remove(deadpool);
            deadpool = new UserIPEndPoint(2000, new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5062));
            set.Add(deadpool);

            //act

            //assert
            var snapshot = set.GetSnapShot();
            Assert.That(snapshot.Length, Is.EqualTo(2));
            Assert.That(SpanHas(snapshot, ironMan), Is.True);
            Assert.That(SpanHas(snapshot, deadpool), Is.True);
        }

        bool SpanHas<T>(Span<T> span, T value)
        {
            foreach(var item in span)
            {
                if(item.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}