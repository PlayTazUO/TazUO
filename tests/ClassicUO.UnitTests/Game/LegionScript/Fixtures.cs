using System;
using System.Runtime.CompilerServices;
using System.Threading;
using ClassicUO.Game.Managers;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

// ReSharper disable ClassNeverInstantiated.Global

[CollectionDefinition(Name)]
public class MainThreadCollection : ICollectionFixture<MainThreadFixture>
{
    public const string Name = "MainThread collection";
}

public class MainThreadFixture : IDisposable
{
   private readonly Thread _mt;
   private readonly ManualResetEventSlim _resetEvent = new();
   
   private bool _disposed;
   
   public MainThreadFixture()
   {
      _mt = new Thread(Run) { Name = "Test Main Thread" };
      _mt.Start();
   }

   private void Run()
   {
      MainThreadQueue.Load();
      
      while (!_resetEvent.IsSet)
         MainThreadQueue.ProcessQueue();
   }
   
   [MethodImpl(MethodImplOptions.Synchronized)]
   public void Dispose()
   {
      if (_disposed)
         return;

      _disposed = true;
      _resetEvent.Set();
      _mt.Join();
      GC.SuppressFinalize(this);
   }
}
