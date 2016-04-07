using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Scarlet.IO
{
    public class DisposableCollection<T> : Collection<T>, IDisposable where T : IDisposable
    {
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        foreach (T t in this) t.Dispose();
                        this.Clear();
                    }
                }
            }
            finally
            {
                disposed = true;
            }
        }

        ~DisposableCollection()
        {
            Dispose(false);
        }
    }
}
