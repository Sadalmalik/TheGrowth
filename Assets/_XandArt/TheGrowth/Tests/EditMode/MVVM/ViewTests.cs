using System;
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.MVVM;
using TheGrowth.Tests.Support;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.MVVM
{
    /// <summary>База MVVM: привязка вьюхи, подписки вьюхи и VM, уничтожение VM, свойство окна, мост EventBus.</summary>
    public sealed class ViewTests
    {
        private readonly List<GameObject> m_Objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in m_Objects)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }

            m_Objects.Clear();
        }

        private T Create<T>() where T : Component
        {
            var gameObject = new GameObject(typeof(T).Name);
            m_Objects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        [Test]
        public void Bind_SubscribesInOnBind_UnbindDisposesSubscriptions()
        {
            var view = Create<TestView>();
            var viewModel = new TestViewModel();
            viewModel.Value.Value = 1;

            view.Bind(viewModel);

            Assert.IsTrue(view.IsBound);
            Assert.AreSame(viewModel, view.Source);
            Assert.AreEqual(1, view.LastValue, "the current value is applied on bind");
            viewModel.Value.Value = 2;
            Assert.AreEqual(2, view.LastValue);

            view.Unbind();

            Assert.IsFalse(view.IsBound);
            Assert.IsNull(view.Source);
            Assert.AreEqual(1, view.UnbindCount);
            Assert.IsFalse(viewModel.Value.HasObservers, "the view subscription is disposed");
            viewModel.Value.Value = 3;
            Assert.AreEqual(2, view.LastValue);

            view.Unbind();
            Assert.AreEqual(1, view.UnbindCount, "unbinding twice has no effect");
            viewModel.Dispose();
        }

        [Test]
        public void ViewModelDispose_UnbindsView_DisposesItsSubscriptions_Once()
        {
            var view = Create<TestView>();
            var viewModel = new TestViewModel();
            var source = new ReactiveProperty<int>();
            var received = 0;
            source.Subscribe(_ => received++).AddTo(viewModel);
            received = 0;
            view.Bind(viewModel);
            var token = viewModel.Token;

            viewModel.Dispose();

            Assert.IsTrue(viewModel.IsDisposed);
            Assert.IsFalse(view.IsBound, "disposing the view model unbinds its view");
            Assert.AreEqual(1, view.UnbindCount);
            Assert.AreEqual(1, viewModel.DisposeCount);
            Assert.IsFalse(source.HasObservers, "AddTo(viewModel) subscriptions are disposed");
            Assert.IsTrue(token.IsCancellationRequested, "the view model lifetime is cancelled");
            source.Value = 1;
            Assert.AreEqual(0, received);

            viewModel.Dispose();
            Assert.AreEqual(1, viewModel.DisposeCount, "disposing twice has no effect");
        }

        [Test]
        public void Bind_DisposedViewModel_Throws()
        {
            var view = Create<TestView>();
            var viewModel = new TestViewModel();
            viewModel.Dispose();

            Assert.Throws<ObjectDisposedException>(() => view.Bind(viewModel));
            Assert.IsFalse(view.IsBound);
            Assert.AreEqual(0, view.BindCount);
        }

        [Test]
        public void Bind_Again_UnbindsPreviousSource()
        {
            var view = Create<TestView>();
            var first = new TestViewModel();
            var second = new TestViewModel();

            view.Bind(first);
            view.Bind(second);

            Assert.AreEqual(1, view.UnbindCount);
            Assert.IsFalse(first.Value.HasObservers, "the previous source is released");
            Assert.AreSame(second, view.Source);
            first.Dispose();
            Assert.IsTrue(view.IsBound, "disposing the previous view model does not unbind the view");

            view.Bind(second);
            Assert.AreEqual(3, view.BindCount);
            Assert.AreEqual(2, view.UnbindCount);
            var notifications = view.Notifications;
            second.Value.Value = 5;
            Assert.AreEqual(notifications + 1, view.Notifications, "rebinding the same source keeps one subscription");
            second.Dispose();
        }

        [Test]
        public void AddTo_UnboundViewOrDisposedViewModel_DisposesAndThrows()
        {
            var view = Create<TestView>();
            var subject = new ReactiveProperty<int>();

            Assert.Throws<InvalidOperationException>(() => subject.Subscribe(_ => { }).AddTo(view));
            Assert.IsFalse(subject.HasObservers);

            var viewModel = new TestViewModel();
            viewModel.Dispose();
            Assert.Throws<ObjectDisposedException>(() => subject.Subscribe(_ => { }).AddTo(viewModel));
            Assert.IsFalse(subject.HasObservers);
        }

        [Test]
        public void View_BindsToPlainData()
        {
            var view = Create<TestItemView>();
            var item = new TestItem("a");

            view.Bind(item);

            Assert.AreSame(item, view.Source);
            Assert.AreEqual(typeof(TestItem), view.SourceType);
            Assert.Throws<ArgumentException>(() => view.BindObject(new TestViewModel()));
            view.Unbind();
            Assert.IsNull(view.Source);
        }

        [Test]
        public void WindowProperty_Open_ReplacesPrevious_DisposeClearsProperty()
        {
            var property = new ReactiveProperty<TestViewModel>();
            var values = new List<TestViewModel>();
            property.Subscribe(values.Add);
            var first = new TestViewModel();
            var second = new TestViewModel();

            property.Open(first);
            property.Open(second);

            Assert.IsTrue(first.IsDisposed, "the previous window is disposed");
            Assert.AreSame(second, property.Value);
            Assert.That(values, Is.EqualTo(new[] { null, first, null, second }));

            second.Dispose();
            Assert.IsNull(property.Value, "disposing the view model closes the window");

            var third = property.Open(new TestViewModel());
            property.Close();
            Assert.IsTrue(third.IsDisposed);
            Assert.IsNull(property.Value);
            Assert.Throws<ObjectDisposedException>(() => property.Open(third));
        }

        [Test]
        public void EventBus_Observe_SubscribesUntilDisposed()
        {
            var bus = new EventBus();
            var received = new List<int>();

            var subscription = bus.Observe<int>().Subscribe(received.Add);
            bus.Invoke(1);
            subscription.Dispose();

            Assert.That(received, Is.EqualTo(new[] { 1 }));
            Assert.IsFalse(bus.AnySubscribers<int>(), "disposing the observable unsubscribes from the bus");
        }
    }
}
