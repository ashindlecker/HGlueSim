using System.Collections.Generic;
using SFML.Graphics;

namespace Client
{
    internal class AnimatedSprite
    {
        public uint Delay;
        public bool Loop;

        public List<Sprite> Sprites;

        private int _currentSpriteId;
        private float _passedTime;

        public AnimatedSprite(uint delay)
        {
            Sprites = new List<Sprite>();
            Delay = delay;

            _passedTime = 0;
            _currentSpriteId = 0;

            Loop = true;
        }

        public bool AnimationCompleted
        {
            get { return _currentSpriteId >= Sprites.Count; }
        }

        public Sprite CurrentSprite
        {
            get
            {
                if (_currentSpriteId < Sprites.Count)
                    return Sprites[_currentSpriteId];
                else
                {
                    return Sprites[Sprites.Count - 1];
                }
            }
        }

        public void Reset()
        {
            if (AnimationCompleted)
            {
                _currentSpriteId = 0;
                _passedTime = 0;
            }
        }

        public void Update(float ms)
        {
            _passedTime += ms;

            if (_passedTime >= Delay)
            {
                _currentSpriteId++;
                if (_currentSpriteId >= Sprites.Count)
                {
                    if (Loop)
                    {
                        _currentSpriteId = 0;
                    }
                }
                _passedTime = 0;
            }
        }
    }
}