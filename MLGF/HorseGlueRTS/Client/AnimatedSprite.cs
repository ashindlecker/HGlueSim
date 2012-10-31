using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;


namespace Client
{
    class AnimatedSprite
    {
        public uint Delay;

        public List<Sprite> Sprites;

        private int _currentSpriteId;
        private float _passedTime;


        public AnimatedSprite(uint delay)
        {
            Sprites = new List<Sprite>();
            Delay = delay;

            _passedTime = 0;
            _currentSpriteId = 0;
        }

        public void Update(float ms)
        {
            _passedTime += ms;

            if(_passedTime >= Delay)
            {
                _currentSpriteId++;
                if(_currentSpriteId >= Sprites.Count)
                {
                    _currentSpriteId = 0;
                }
                _passedTime = 0;
            }
        }

        public Sprite CurrentSprite
        {
            get { return Sprites[_currentSpriteId]; }
        }

    }
}
