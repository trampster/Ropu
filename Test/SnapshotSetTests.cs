using NUnit.Framework;
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
    }
}