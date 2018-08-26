using System;
using System.Timers;

namespace raztalk
{
    public class Timeout
    {
        private Timer m_timer;

        public event ElapsedEventHandler Expired;

        private void FireExpiredEvent(object sender, ElapsedEventArgs e)
        {
            Stop();
            Expired?.Invoke(this, e);
        }

        public void Start(TimeSpan timeout)
        {
            Stop();

            m_timer = new Timer(timeout.TotalMilliseconds + 1); // wait for max N seconds until signalR is connected
            m_timer.Elapsed += FireExpiredEvent;
            m_timer.AutoReset = false;
            m_timer.Enabled = true;
        }
        
        public void Stop()
        {
            if (m_timer != null)
            {
                m_timer.Elapsed -= FireExpiredEvent;
                m_timer.Dispose();
                m_timer = null;
            }
        }
    }
}
