using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IServiceOriented.ServiceBus.IO
{
    public abstract class MessageDeliveryReader : IDisposable
    {
        public MessageDeliveryReader(Stream stream, bool isOwner)
        {
            BaseStream = stream;
            OwnsStream = isOwner;
        }
        
        protected bool OwnsStream
        {
            get;
            set;
        }

        protected Stream BaseStream
        {
            get;
            private set;
        }

        public bool EndOfStream
        {
            get
            {
                return BaseStream.Position == BaseStream.Length;
            }
        }

        public void Close()
        {
            if (OwnsStream)
            {
                BaseStream.Close();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!Disposed)
            {
                Close();
                Disposed = true;
            }
        }

        protected bool Disposed
        {
            get;
            private set;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        ~MessageDeliveryReader()
        {
            Dispose(false);
        }

        public abstract MessageDelivery Read();
    }
}
