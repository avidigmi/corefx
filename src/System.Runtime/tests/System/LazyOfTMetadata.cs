// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

using Xunit;

namespace System.Tests
{
    public static class LazyOfTMetadataTests
    {
        [Fact]
        public static void TestCtor_TMetadata()
        {
            var lazy = new Lazy<int, string>("metadata1");
            VerifyLazy(lazy, 0, "metadata1");

            lazy = new Lazy<int, string>(null);
            VerifyLazy(lazy, 0, null);
        }

        [Fact]
        public static void TestCtor_TMetadata_Bool()
        {
            var lazy = new Lazy<int, string>("metadata2", false);
            VerifyLazy(lazy, 0, "metadata2");
        }

        [Fact]
        public static void TestCtor_TMetadata_LazyThreadSaftetyMode()
        {
            var lazy = new Lazy<int, string>("metadata3", LazyThreadSafetyMode.PublicationOnly);
            VerifyLazy(lazy, 0, "metadata3");
        }

        [Fact]
        public static void TestCtor_TMetadata_LazyThreadSaftetyMode_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<int, string>("test", LazyThreadSafetyMode.None - 1)); // Invalid mode
            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<int, string>("test", LazyThreadSafetyMode.ExecutionAndPublication + 1)); // Invalid mode
        }

        [Fact]
        public static void TestCtor_ValueFactory_TMetadata()
        {
            var lazy = new Lazy<string, int>(() => "foo", 4);
            VerifyLazy(lazy, "foo", 4);
        }

        [Fact]
        public static void TestCtor_ValueFactory_TMetadata_Invalid()
        {
            Assert.Throws<ArgumentNullException>("valueFactory", () => new Lazy<int, string>(null, "test")); // Value factory is null
        }

        [Fact]
        public static void TestCtor_ValueFactory_TMetadata_Bool()
        {
            var lazy = new Lazy<string, int>(() => "foo", 5, false);
            VerifyLazy(lazy, "foo", 5);
        }

        [Fact]
        public static void TestCtor_ValueFactory_TMetadata_Bool_Invalid()
        {
            Assert.Throws<ArgumentNullException>("valueFactory", () => new Lazy<int, string>(null, "test", false)); // Value factory is null
        }

        [Fact]
        public static void TestCtor_ValueFactory_TMetadata_LazyThreadSaftetyMode()
        {
            var lazy = new Lazy<string, int>(() => "foo", 6, LazyThreadSafetyMode.None);
            VerifyLazy(lazy, "foo", 6);
        }

        [Fact]
        public static void TestCtor_ValueFactory_TMetadata_LazyThreadSaftetyMode_Invalid()
        {
            Assert.Throws<ArgumentNullException>("valueFactory", () => new Lazy<int, string>(null, "test", LazyThreadSafetyMode.PublicationOnly)); // Value factory is null

            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<int, string>(() => 42, "test", LazyThreadSafetyMode.None - 1)); // Invalid mode
            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<int, string>(() => 42, "test", LazyThreadSafetyMode.ExecutionAndPublication + 1)); // Invalid mode
        }

        public static void VerifyLazy<T, TMetadata>(Lazy<T, TMetadata> lazy, T expectedValue, TMetadata expectedMetadata)
        {
            // Accessing metadata doesn't create the value
            Assert.False(lazy.IsValueCreated);
            Assert.Equal(expectedMetadata, lazy.Metadata);
            Assert.False(lazy.IsValueCreated);

            Assert.Equal(expectedValue, lazy.Value);
            Assert.True(lazy.IsValueCreated);
        }
    }
}
