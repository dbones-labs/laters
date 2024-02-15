namespace Laters.ServerProcessing.Engine;

public static class ReaderWriterLockSlimExtensions
{
    public static IDisposable CreateWriteLock(this ReaderWriterLockSlim locker)
    {
        return new WriteLock(locker);
    }

    public static UpgradeableReadLock CreateUpgradeableReadLock(this ReaderWriterLockSlim locker)
    {
        return new UpgradeableReadLock(locker);
    }
    
    public class UpgradeableReadLock : IDisposable
    {
        readonly ReaderWriterLockSlim _locker;

        public UpgradeableReadLock(ReaderWriterLockSlim locker)
        {
            _locker = locker;
            _locker.EnterUpgradeableReadLock();
        }

        public IDisposable EnterWrite()
        {
            return _locker.CreateWriteLock();
        }
        
        public void Dispose()
        {
            _locker.ExitUpgradeableReadLock();
        }
    }
    

    class WriteLock : IDisposable
    {
        readonly ReaderWriterLockSlim _locker;

        public WriteLock(ReaderWriterLockSlim locker)
        {
            _locker = locker;
            _locker.EnterWriteLock();
        }

        public void Dispose()
        {
            _locker.ExitWriteLock();
        }
    }
}