using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace UBC.RISe.MosicIntegration.Service
{
    public class HttpClientSyncCall
    {
        private static readonly TaskFactory MyTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        public static T RunSync<T>(Func<Task<T>> func)
        {
            CultureInfo cultureUi = CultureInfo.CurrentUICulture;
            CultureInfo culture = CultureInfo.CurrentCulture;
            return MyTaskFactory.StartNew<Task<T>>(() =>
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = cultureUi;
                return func();
            }).Unwrap<T>().GetAwaiter().GetResult();
        }
    }
}