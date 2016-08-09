using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomCancelationToken
{
    public class CustomCancellationToken
    {
        private object cancelSync = new object();
        private bool isCancelled = false;

        public bool IsCancelled
        {
            get 
            {
                lock (cancelSync)
                {
                    return this.isCancelled;
                }
            }
        }

        public void Cancel()
        {
            lock (cancelSync)
            {
                this.isCancelled = true;
            }
        }
    }
}
