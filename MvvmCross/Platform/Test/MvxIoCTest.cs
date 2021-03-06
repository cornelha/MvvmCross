﻿// MvxIocTest.cs

// MvvmCross is licensed using Microsoft Public License (Ms-PL)
// Contributions and inspirations noted in readme.md and license.txt
//
// Project Lead - Stuart Lodge, @slodge, me@slodge.com

using System.Collections.Generic;
using MvvmCross.Platform.Core;
using MvvmCross.Platform.Exceptions;
using MvvmCross.Platform.IoC;
using NUnit.Framework;
using System.Reflection;
using System.Linq;

namespace MvvmCross.Platform.Test
{
    [TestFixture]
    public class MvxIocTest
    {
        public interface IA 
        { 
            IB B { get; } 
        }

        public interface IB
        {
            IC C { get; }
        }

        public interface IC
        {
        }

        public interface IOG<T>
        {
        }

        public interface IOG2<T, T2>
        {
        }

        public interface IHasOGParameter
        {
            IOG<C> OpenGeneric { get; }
        }

        public class A : IA
        {
            public A(IB b)
            {
                B = b;
            }

            public IB B { get; set; }
        }

        public class B : IB
        {
            public B(IC c)
            {
                C = c;
            }

            public IC C { get; set; }
        }

        public class C : IC
        {
            public C(IA a)
            {
            }
        }

        public class C2 : IC
        {
            public C2()
            {
            }
        }

        public class COdd : IC
        {
            public static bool FirstTime = true;

            public COdd()
            {
                if (FirstTime)
                {
                    FirstTime = false;
                    var a = Mvx.Resolve<IA>();
                }
            }
        }

        public class OG<T> : IOG<T>
        {
        }

        public class OG2<T, T2> : IOG2<T, T2>
        {
        }

        public class HasOGParameter : IHasOGParameter
        {
            public HasOGParameter(IOG<C> openGeneric)
            {
                this.OpenGeneric = openGeneric;
            }

            public IOG<C> OpenGeneric { get; }
        }

        [Test]
        public void TryResolve_CircularButSafeDynamicWithOptionOff_ReturnsTrue()
        {
            COdd.FirstTime = true;
            MvxSingleton.ClearAllSingletons();
            var options = new MvxIocOptions()
            {
                TryToDetectDynamicCircularReferences = false
            };
            var instance = MvxIoCProvider.Initialize(options);

            Mvx.RegisterType<IA, A>();
            Mvx.RegisterType<IB, B>();
            Mvx.RegisterType<IC, COdd>();

            IA a;
            var result = Mvx.TryResolve(out a);
            Assert.IsTrue(result);
            Assert.IsNotNull(a);
        }

        [Test]
        public void TryResolve_CircularButSafeDynamicWithOptionOn_ReturnsFalse()
        {
            COdd.FirstTime = true;
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.RegisterType<IA, A>();
            Mvx.RegisterType<IB, B>();
            Mvx.RegisterType<IC, COdd>();

            IA a;
            var result = Mvx.TryResolve(out a);
            Assert.IsFalse(result);
            Assert.IsNull(a);
        }

        [Test]
        public void TryResolve_CircularLazyRegistration_ReturnsFalse()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.LazyConstructAndRegisterSingleton<IA, A>();
            Mvx.LazyConstructAndRegisterSingleton<IB, B>();
            Mvx.LazyConstructAndRegisterSingleton<IC, C>();

            IA a;
            var result = Mvx.TryResolve(out a);
            Assert.IsFalse(result);
            Assert.IsNull(a);
        }

        [Test]
        public void TryResolve_NonCircularRegistration_ReturnsTrue()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.LazyConstructAndRegisterSingleton<IA, A>();
            Mvx.LazyConstructAndRegisterSingleton<IB, B>();
            Mvx.LazyConstructAndRegisterSingleton<IC, C2>();

            IA a;
            var result = Mvx.TryResolve(out a);
            Assert.IsTrue(result);
            Assert.IsNotNull(a);
        }

        [Test]
        public void TryResolve_LazySingleton_ReturnsSameSingletonEachTime()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.LazyConstructAndRegisterSingleton<IA, A>();
            Mvx.LazyConstructAndRegisterSingleton<IB, B>();
            Mvx.LazyConstructAndRegisterSingleton<IC, C2>();

            IA a0;
            var result = Mvx.TryResolve(out a0);
            Assert.IsTrue(result);
            Assert.IsNotNull(a0);

            for (int i = 0; i < 100; i++)
            {
                IA a1;
                result = Mvx.TryResolve(out a1);
                Assert.IsTrue(result);
                Assert.AreSame(a0, a1);
            }
        }

        [Test]
        public void TryResolve_NonLazySingleton_ReturnsSameSingletonEachTime()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.LazyConstructAndRegisterSingleton<IB, B>();
            Mvx.LazyConstructAndRegisterSingleton<IC, C2>();
            Mvx.ConstructAndRegisterSingleton<IA, A>();

            IA a0;
            var result = Mvx.TryResolve(out a0);
            Assert.IsTrue(result);
            Assert.IsNotNull(a0);

            for (int i = 0; i < 100; i++)
            {
                IA a1;
                result = Mvx.TryResolve(out a1);
                Assert.IsTrue(result);
                Assert.AreSame(a0, a1);
            }
        }

        [Test]
        public void TryResolve_Dynamic_ReturnsDifferentInstanceEachTime()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.LazyConstructAndRegisterSingleton<IB, B>();
            Mvx.LazyConstructAndRegisterSingleton<IC, C2>();
            Mvx.RegisterType<IA, A>();

            var previous = new Dictionary<IA, bool>();

            for (int i = 0; i < 100; i++)
            {
                IA a1;
                var result = Mvx.TryResolve(out a1);
                Assert.IsTrue(result);
                Assert.IsFalse(previous.ContainsKey(a1));
                Assert.AreEqual(i, previous.Count);
                previous.Add(a1, true);
            }
        }

        [Test]
        public void TryResolve_ParameterConstructors_CreatesParametersUsingIocResolution()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            Mvx.RegisterType<IB, B>();
            Mvx.LazyConstructAndRegisterSingleton<IC, C2>();
            Mvx.RegisterType<IA, A>();

            IA a1;
            var result = Mvx.TryResolve(out a1);
            Assert.IsTrue(result);
            Assert.IsNotNull(a1);
            Assert.IsNotNull(a1.B);
            Assert.IsInstanceOf<B>(a1.B);
        }

        [Test]
        public void RegisterType_with_constructor_creates_different_objects()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            instance.RegisterType<IC>(() => new C2());

            var c1 = Mvx.Resolve<IC>();
            var c2 = Mvx.Resolve<IC>();

            Assert.IsNotNull(c1);
            Assert.IsNotNull(c2);

            Assert.AreNotEqual(c1, c2);
        }

        [Test]
        public void Non_generic_RegisterType_with_constructor_creates_different_objects()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            instance.RegisterType(typeof(IC), () => new C2());

            var c1 = Mvx.Resolve<IC>();
            var c2 = Mvx.Resolve<IC>();

            Assert.IsNotNull(c1);
            Assert.IsNotNull(c2);

            Assert.AreNotEqual(c1, c2);
        }

        [Test]
        public void Non_generic_RegisterType_with_constructor_throws_if_constructor_returns_incompatible_reference()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            instance.RegisterType(typeof(IC), () => "Fail");

            Assert.Throws<MvxIoCResolveException>(() => {
                var c1 = Mvx.Resolve<IC>();
            });
        }

        [Test]
        public void Non_generic_RegisterType_with_constructor_throws_if_constructor_returns_incompatible_value()
        {
            MvxSingleton.ClearAllSingletons();
            var instance = MvxIoCProvider.Initialize();

            instance.RegisterType(typeof(IC), () => 36);

            Assert.Throws<MvxIoCResolveException>(() => {
                var c1 = Mvx.Resolve<IC>();
            });
        }

        #region Open-Generics

        [Test]
        public static void Resolves_successfully_when_registered_open_generic_with_one_generic_type_parameter()
        {
            var instance = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)instance).CleanAllResolvers();

            instance.RegisterType(typeof(IOG<>), typeof(OG<>));

            IOG<C2> toResolve = null;
            Mvx.TryResolve<IOG<C2>>(out toResolve);

            Assert.IsNotNull(toResolve);
            Assert.IsTrue(toResolve.GetType().GetTypeInfo().ImplementedInterfaces.Any(i => i == typeof(IOG<C2>)));
            Assert.IsTrue(toResolve.GetType() == typeof(OG<C2>));
        }

        [Test]
        public static void Resolves_successfully_when_registered_closed_generic_with_one_generic_type_parameter()
        {
            var instance = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)instance).CleanAllResolvers();

            instance.RegisterType(typeof(IOG<C2>), typeof(OG<C2>));
            
            IOG<C2> toResolve = null;
            Mvx.TryResolve<IOG<C2>>(out toResolve);

            Assert.IsNotNull(toResolve);
            Assert.IsTrue(toResolve.GetType().GetTypeInfo().ImplementedInterfaces.Any(i => i == typeof(IOG<C2>)));
            Assert.IsTrue(toResolve.GetType() == typeof(OG<C2>));
        }

        [Test]
        public static void Resolves_successfully_when_registered_open_generic_with_two_generic_type_parameter()
        {
            var instance = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)instance).CleanAllResolvers();

            instance.RegisterType(typeof(IOG2<,>), typeof(OG2<,>));

            IOG2<C2, C> toResolve = null;
            Mvx.TryResolve<IOG2<C2,C>>(out toResolve);

            Assert.IsNotNull(toResolve);
            Assert.IsTrue(toResolve.GetType().GetTypeInfo().ImplementedInterfaces.Any(i => i == typeof(IOG2<C2, C>)));
            Assert.IsTrue(toResolve.GetType() == typeof(OG2<C2, C>));
        }

        [Test]
        public static void Resolves_successfully_when_registered_closed_generic_with_two_generic_type_parameter()
        {
            var instance = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)instance).CleanAllResolvers();

            instance.RegisterType(typeof(IOG2<C2,C>), typeof(OG2<C2,C>));

            IOG2<C2, C> toResolve = null;
            Mvx.TryResolve<IOG2<C2, C>>(out toResolve);

            Assert.IsNotNull(toResolve);
            Assert.IsTrue(toResolve.GetType().GetTypeInfo().ImplementedInterfaces.Any(i => i == typeof(IOG2<C2, C>)));
            Assert.IsTrue(toResolve.GetType() == typeof(OG2<C2, C>));
        }

        [Test]
        public static void Resolves_unsuccessfully_when_registered_open_generic_with_one_generic_parameter_that_was_not_registered()
        {
            var instance = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)instance).CleanAllResolvers();

            IOG<C2> toResolve = null;

            var isResolved = Mvx.TryResolve<IOG<C2>>(out toResolve);

            Assert.IsFalse(isResolved);
            Assert.IsNull(toResolve);
        }

        [Test]
        public static void Resolves_successfully_when_resolving_entity_that_has_injected_an_open_generic_parameter()
        {
            var instance = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)instance).CleanAllResolvers();

            instance.RegisterType<IHasOGParameter, HasOGParameter>();
            instance.RegisterType(typeof(IOG<>), typeof(OG<>));

            IHasOGParameter toResolve = null;
            Mvx.TryResolve<IHasOGParameter>(out toResolve);

            Assert.IsNotNull(toResolve);
            Assert.IsTrue(toResolve.GetType().GetTypeInfo().ImplementedInterfaces.Any(i => i == typeof(IHasOGParameter)));
            Assert.IsTrue(toResolve.GetType() == typeof(HasOGParameter));
            Assert.IsTrue(toResolve.OpenGeneric.GetType().GetTypeInfo().ImplementedInterfaces.Any(i => i == typeof(IOG<C>)));
            Assert.IsTrue(toResolve.OpenGeneric.GetType() == typeof(OG<C>));
        }

        #endregion

        #region Child Container

        [Test]
        public static void Resolves_successfully_when_using_childcontainer()
        {
            var container = MvxIoCProvider.Initialize();
            ((MvxIoCProvider)container).CleanAllResolvers();

            container.RegisterType<IC, C2>();
            var childContainer = container.CreateChildContainer();
            childContainer.RegisterType<IB, B>();

            var b = childContainer.Create<IB>();

            Assert.IsTrue(container.CanResolve<IC>());
            Assert.IsFalse(container.CanResolve<IB>());
            Assert.IsTrue(childContainer.CanResolve<IC>());
            Assert.IsTrue(childContainer.CanResolve<IB>());

            Assert.IsNotNull(b);
            Assert.IsNotNull(b.C);
        }

        #endregion

        // TODO - there are so many tests we could and should do here!
    }
}