namespace AppVendedores2025;

public partial class App
{
    public class TimerService
    {
        private Timer _timer;

        public event Action OnTimerElapsed;

        public TimerService()
        {
            _timer = new Timer(TimerCallback, null, 0, 72000); // 60 segundos
        }

        private void TimerCallback(object state)
        {
            OnTimerElapsed?.Invoke();
        }
    }

}
