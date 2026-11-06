namespace Isekai.Gameplay.Combat
{
    // Which step of a light-attack chain an attack is: the chain continues within the window and starts over after it.
    public class ComboChain
    {
        private readonly int _length;
        private readonly float _window;
        private int _step;
        private float _lastTime = float.NegativeInfinity;

        public ComboChain(int length, float window)
        {
            _length = length;
            _window = window;
        }

        public int Next(float now)
        {
            _step = now - _lastTime <= _window ? _step % _length + 1 : 1;
            _lastTime = now;
            return _step;
        }

        public void Reset() => _lastTime = float.NegativeInfinity;
    }
}
