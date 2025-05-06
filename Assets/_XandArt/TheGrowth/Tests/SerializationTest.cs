using System;
using NUnit.Framework;
using XandArt.Architecture;

namespace XandArt.Tests
{
    public class SerializationTest
    {
        // I was testing generic struct casts for implementing Serialization converter.
        // Turns out that best way is full reflection management (see RefConverter)
        // [Test]
        // public void TestGenericCast()
        // {
        //     Ref<Entity> refA = new Ref<Entity> { Guid = Guid.NewGuid() };
        //     Ref<SampleEntity> refB = null;
        //
        //     var refC = (object)refA;
        //
        //     refB = (SampleEntity) refC;
        //
        //     Assert.AreEqual(refA.Guid, refB.Guid);
        // }

        public class SampleEntity : Entity
        {
            public Ref<SampleEntity> Other;
        }

        [Test]
        public void TestReference()
        {
            var world = new PersistentState();
            world.SetActive();

            var entity = new Entity();
            world.Add(entity);
            Ref<Entity> entityReference = entity;

            Assert.AreEqual(entityReference.Guid, entity.Guid);

            entityReference = null;

            Assert.AreEqual(entityReference.Guid, Guid.Empty);

            entityReference.Guid = entity.Guid;

            Assert.AreEqual(entityReference.Value, entity);

            PersistentState.Active = null;
        }

        [Test]
        public void TestSerialization()
        {
            var persistenceManager = new PersistenceManager();
            var world1 = new PersistentState();
            world1.SetActive();

            // just fill
            var entity = new Entity();
            world1.Add(entity);

            // Loop reference
            var entityA = new SampleEntity();
            world1.Add(entityA);
            var entityB = new SampleEntity();
            world1.Add(entityB);

            entityA.Other = entityB;
            entityB.Other = entityA;

            // Test save and load
            persistenceManager.Save("TestSave", world1);

            var world2 = persistenceManager.Load("TestSave");
            //world2.SetActive();

            Assert.AreEqual(world1.Entities.Count, world2.Entities.Count);
        }
    }
}