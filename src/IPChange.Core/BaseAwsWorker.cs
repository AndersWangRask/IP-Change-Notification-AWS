using Amazon;
using IPChange.Core.Model;
using IPChange.Core.Model.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IPChange.Core
{
    /// <summary>
    /// Abstract class used by the Worker classes 
    /// </summary>
    public abstract class BaseAwsWorker : IDisposable
    {
        public Config Config { get; protected set; }
        public IpState IpState { get; protected set; }
        protected Action<string> Output { get; private set;  }
        protected MultiClientState MultiClientState { get; private set; }

        protected readonly List<(DateTime time, string outputText)> _outputLog =
            new List<(DateTime time, string outputText)>();

        public BaseAwsWorker(Config config, IpState ipState, Action<string> output, MultiClientState multiClientState)
        {
            Config = config;
            IpState = ipState;
            MultiClientState = multiClientState;

            if (output != null)
            {
                Output =
                    (msg) =>
                    {
                        output(msg);
                        _outputLog.Add((DateTime.Now, msg));
                    };
            }
            else
            {
                //Ensure that there is an output function
                Output =
                    (msg) =>
                    {
                        Debug.WriteLine(msg);
                        _outputLog.Add((DateTime.Now, msg));
                    };
            }
        }

        public RegionEndpoint RegionEndpoint
        {
            get
            {
                if (_regionEndpoint == null)
                {
                    _regionEndpoint = RegionEndpoint.GetBySystemName(Config.BaseSettings.AWSRegion);
                }

                return _regionEndpoint;
            }
        }
        protected RegionEndpoint _regionEndpoint = null;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        #endregion

        public override string ToString() => $"AWS Worker: {IpState}";
    }
}