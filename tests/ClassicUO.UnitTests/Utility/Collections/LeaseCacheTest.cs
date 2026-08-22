using System;
using ClassicUO.Utility.Collections;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.Collections
{
    public class LeaseCacheTest
    {
        private sealed class Tracked : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Fact]
        public void Lease_SameKeyTwice_BuildsOnce()
        {
            var cache = new LeaseCache<string, Tracked>();
            int builds = 0;

            Tracked first = cache.Lease("a", _ => { builds++; return new Tracked(); });
            Tracked second = cache.Lease("a", _ => { builds++; return new Tracked(); });

            builds.Should().Be(1);
            second.Should().BeSameAs(first);
            cache.Count.Should().Be(1);
        }

        [Fact]
        public void Release_WhileOtherLeasesOutstanding_KeepsValueAlive()
        {
            var cache = new LeaseCache<string, Tracked>();

            Tracked value = cache.Lease("a", _ => new Tracked());
            cache.Lease("a", _ => new Tracked());

            cache.Release("a");

            value.IsDisposed.Should().BeFalse();
            cache.Count.Should().Be(1);
        }

        [Fact]
        public void Release_LastLease_DisposesAndDrops()
        {
            var cache = new LeaseCache<string, Tracked>();

            Tracked value = cache.Lease("a", _ => new Tracked());
            cache.Release("a");

            value.IsDisposed.Should().BeTrue();
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void Lease_AfterFullRelease_RebuildsRatherThanReturningDisposed()
        {
            var cache = new LeaseCache<string, Tracked>();

            Tracked first = cache.Lease("a", _ => new Tracked());
            cache.Release("a");

            Tracked second = cache.Lease("a", _ => new Tracked());

            second.Should().NotBeSameAs(first);
            second.IsDisposed.Should().BeFalse();
        }

        /// <summary>Re-applying an already-held key must not dip through zero and dispose what it hands back.</summary>
        [Fact]
        public void Lease_BeforeReleaseOfSameKey_NeverDisposes()
        {
            var cache = new LeaseCache<string, Tracked>();

            Tracked value = cache.Lease("a", _ => new Tracked());

            cache.Lease("a", _ => new Tracked());
            cache.Release("a");

            value.IsDisposed.Should().BeFalse();
            cache.Count.Should().Be(1);
        }

        [Fact]
        public void Release_UnknownKey_IsIgnored()
        {
            var cache = new LeaseCache<string, Tracked>();

            Action release = () => cache.Release("missing");

            release.Should().NotThrow();
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void Release_DisposalDisabled_LeavesValueIntact()
        {
            var cache = new LeaseCache<string, Tracked>(disposeValues: false);

            Tracked value = cache.Lease("a", _ => new Tracked());
            cache.Release("a");

            value.IsDisposed.Should().BeFalse();
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void Lease_NullValue_IsMemoizedAndReleasedCleanly()
        {
            var cache = new LeaseCache<string, Tracked>();
            int builds = 0;

            cache.Lease("a", _ => { builds++; return null; });
            cache.Lease("a", _ => { builds++; return null; });

            builds.Should().Be(1);

            cache.Release("a");
            cache.Release("a");

            cache.Count.Should().Be(0);
        }

        [Fact]
        public void Lease_DistinctKeys_AreIndependent()
        {
            var cache = new LeaseCache<string, Tracked>();

            Tracked a = cache.Lease("a", _ => new Tracked());
            Tracked b = cache.Lease("b", _ => new Tracked());

            cache.Release("a");

            a.IsDisposed.Should().BeTrue();
            b.IsDisposed.Should().BeFalse();
            cache.Count.Should().Be(1);
        }
    }
}
